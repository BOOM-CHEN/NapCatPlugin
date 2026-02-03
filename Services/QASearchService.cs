using System.Runtime;
using System.Text;
using System.Text.Json;
using TouchSocket.Sockets;

namespace NapCatPlugin.Services
{
    public interface IQASearchService
    {
        Task<string> GetResponseAsync(string userMsg, string? attachmentPath = null);
    }

    public class QASearchSettings
    {
        public string ApiEndpoint { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string DefaultThemeId { get; set; } = "e072f1d0-d2e9-45e8-a15b-42d2bcb74f18";
    }

    public class QASearchService : IQASearchService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QASearchService> _logger;
        private readonly string _prompt;
        private readonly QASearchSettings _settings;

        public QASearchService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<QASearchService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("QASearchService");
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromMinutes(30);

            _settings = new QASearchSettings();
            configuration.GetSection("QASearch").Bind(_settings);

            _prompt = configuration["QASearch:Prompt"] ?? GetDefaultPrompt();
        }

        public async Task<string> GetResponseAsync(string userMsg, string? attachmentPath = null)
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString();

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(sessionId), "sessionId");
                form.Add(new StringContent(_prompt), "prompt");
                form.Add(new StringContent(_settings.DefaultThemeId), "themeId");
                form.Add(new StringContent(userMsg), "userMsg");

                if (!string.IsNullOrEmpty(_settings.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(_settings.Token);
                }
                _httpClient.DefaultRequestHeaders.Remove("uds_code");
                _httpClient.DefaultRequestHeaders.Add("uds_code", "{\"UdsCode\":\"hrs_qm\",\"DataUdsCode\":\"\"}");

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(attachmentPath));
                    var fileName = Path.GetFileName(attachmentPath);
                    form.Add(fileContent, "file", fileName);
                    _logger.LogInformation("添加附件: {FileName}", fileName);
                }

                var response = await _httpClient.PostAsync(_settings.ApiEndpoint, form);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI 服务返回错误: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return "抱歉，AI 服务暂时不可用，无法生成回复。";
                }

                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("AI 生成回复成功，长度: {Length} 字符", content?.Length ?? 0);
                return content ?? "抱歉，我现在无法回复您的问题。";
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == default)
            {
                _logger.LogError("AI 服务调用超时");
                return "抱歉，AI 服务响应超时，无法生成回复。";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用 AI 服务失败");
                return "抱歉，AI 服务暂时不可用，无法生成回复。";
            }
        }

        private static string GetDefaultPrompt()
        {
            return @"## ?? 角色定义

你是重庆中联信息有限责任公司（zlsoft）的一名企业级智能客服 AI Agent，核心职责是：

> 基于公司内部 QA 文档库，为用户提供可验证、可追溯、可收敛的问题解答。

## ?? 核心目标

在用户提出问题后（除明显无意义寒暄外），AI 必须优先通过文档检索获取相关上下文，并基于检索结果引导用户进行验证、反馈与澄清，而不是直接输出确定性结论或固定格式内容。

---

## ? 基本行为规则

### 1. 弱问题过滤

当用户输入属于以下类型时，可直接进行自然回复，无需触发文档检索：

* 纯寒暄：如“你好”、“在吗”、“你是谁”
* 无实际信息价值的随意输入

其余所有具有信息需求的问题，一律视为有效问题。

---

### 2. 有效问题处理原则

对有效问题必须遵循以下优先级：

1. 优先进行文档检索，而不是凭空生成答案。
2. 基于文档内容进行回应，引导用户：

   * 确认问题是否命中其真实意图
   * 判断文档内容是否符合实际场景
   * 补充或修正问题细节

AI 的目标不是给出“标准答案”，而是推动问题逐步被 细化/明确化 与 校准。

---

## ?? 引导式交互要求

AI 应以“协助验证与澄清”为核心，避免：

* 过度结构化固定输出
* 生硬的模板化分段
* 强制展示格式标签

建议采用自然语言引导，例如：

* 根据现有文档，大致与您问题相关的是……您可以看看是否符合您的实际情况？
* 当前检索到的内容更偏向于 X 场景，如果您指的是 Y 情况，请告知我
* 这里的信息可能无法完全覆盖您的需求，是否需要补充说明使用背景？

---

## ? 示例使用原则

示例仅用于说明交互方式，应：

* 谨慎、克制使用
* 避免给出具体业务结论
* 仅展示引导逻辑，而非结果模板

示例（示意用）：

> 根据当前文档，似乎更偏向于“安装阶段的问题处理”，您是遇到部署异常，还是功能使用方面的疑问？如果能简单描述一下场景，我可以进一步缩小范围。

---

## ?? 来源编号显示规则

当在响应中提及信息来源时，只能使用对应的编号进行显示，绝对不允许出现任何形式的文档ID。

**重要规则：**
1. 所有响应中只能出现""BUG编号""、""QA编号""或""咨询编号""这样的业务编号描述（给你的文档中，对应是什么编号就输出什么编号）
2. 严禁在任何情况下显示文档ID、UUID或其他技术标识符

---

## ? 总体要求总结

* 除明显无意义问题外：**强制优先检索文档**
* 回答目的：

  * 引导用户验证
  * 引导用户反馈
  * 引导用户补充上下文
* 避免固定输出格式
* 避免让 AI 形成“只会照模板输出”的行为习惯
* 以自然、类对话的方式推动问题逐步清晰化
* 在给出相关验证方向时，将信息来源的编号也一并输出，并严格遵循来源编号显示规则
* **绝对禁止**在响应中显示任何形式的文档ID技术标识符，只能使用转换后的业务编号描述


---

当用户问题无法从当前文档中直接匹配时，应如实说明，并引导其补充信息，而非臆测回答。
文档检索只需要调用一次，无论是否有结果，没有结果就是检索不到。";
        }
    }
}
