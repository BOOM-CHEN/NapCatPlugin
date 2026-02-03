using NapPlana.Core.Event.Handler;
using NapPlana.Core.Bot;
using NapPlana.Core.Data;
using NapPlana.Core.Data.Message;

namespace NapCatPlugin.Services
{
    public class BotEventService : BackgroundService
    {
        private readonly IQASearchService _qaSearchService;
        private readonly NapBot _bot;
        private readonly ILogger<BotEventService> _logger;

        public BotEventService(IQASearchService qaSearchService, NapBot bot, ILogger<BotEventService> logger)
        {
            _qaSearchService = qaSearchService;
            _bot = bot;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BotEventHandler.OnMessageSentGroup += (eventData) =>
            {
                Console.WriteLine($"群消息类型 {eventData.MessageType}, 消息ID: {eventData.MessageId}");
            };

            BotEventHandler.OnMessageSentPrivate += (eventData) =>
            {
                Console.WriteLine($"私聊消息类型 {eventData.MessageType}, 消息ID: {eventData.MessageId}");
            };

            BotEventHandler.OnGroupMessageReceived += async (eventData) =>
            {
                Console.WriteLine($"群消息类型 {eventData.MessageType}, 消息ID: {eventData.MessageId}, 群ID: {eventData.GroupId}");
                try
                {
                    string messageContent = string.Empty;
                    if (eventData.Messages.Count > 0)
                    {
                        messageContent = HelperService.ExtractMessageContent(eventData.Messages);
                    }
                    else
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(messageContent))
                    {
                        return;
                    }

                    var botQQ = eventData.SelfId;
                    if (botQQ == 0)
                    {
                        return;
                    }

                    var isAtBot = messageContent.Contains($"[CQ:at,qq={botQQ}]") ||
                                  messageContent.Contains($"@") ||
                                  messageContent.Contains($"[AT:{botQQ}]");

                    if (isAtBot)
                    {
                        var question = messageContent
                            .Replace($"[CQ:at,qq={botQQ}]", "")
                            .Replace($"[AT:{botQQ}]", "")
                            .Replace("@", "")
                            .Trim();

                        if (!string.IsNullOrWhiteSpace(question))
                        {
                            _logger.LogInformation("群消息触发QA: {Question}", question);

                            var response = await _qaSearchService.GetResponseAsync(question);
                            //await SendGroupMessage(eventData.GroupId, response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理群消息QA请求失败");
                }
            };

            BotEventHandler.OnPrivateMessageReceived += async (eventData) =>
            {
                Console.WriteLine($"私聊消息类型 {eventData.MessageType}, 消息ID: {eventData.MessageId}");
                try
                {
                    string messageContent = string.Empty;
                    if (eventData.Messages.Count > 0)
                    {
                        messageContent = HelperService.ExtractMessageContent(eventData.Messages);
                    }
                    else
                    {
                        return;
                    }

                    if (messageContent.TrimStart().StartsWith("#QA", StringComparison.OrdinalIgnoreCase))
                    {
                        var question = messageContent.Substring(3).Trim();
                        if (!string.IsNullOrWhiteSpace(question))
                        {
                            _logger.LogInformation("私聊消息触发QA: {Question}", question);

                            var response = await _qaSearchService.GetResponseAsync(question);
                            //await SendPrivateMessage(eventData.UserId, response);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理私聊QA请求失败");
                }
            };

            return Task.CompletedTask;
        }



        //private async Task SendGroupMessage(long groupId, string message)
        //{
        //    try
        //    {
        //        var sendElement = new List<SendElement>
        //        {
        //            new SendElement
        //            {
        //                Type = "text",
        //                Data = new Dictionary<string, string>
        //                {
        //                    { "content", message }
        //                }
        //            }
        //        };

        //        await _bot.SendGroupMessageAsync(groupId, sendElement);
        //        _logger.LogInformation("群消息回复已发送至群 {GroupId}", groupId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "发送群消息失败，群ID: {GroupId}", groupId);
        //    }
        //}

        //private async Task SendPrivateMessage(long userId, string message)
        //{
        //    try
        //    {
        //        var sendElement = new List<SendElement>
        //        {
        //            new SendElement
        //            {
        //                Type = "text",
        //                Data = new Dictionary<string, string>
        //                {
        //                    { "content", message }
        //                }
        //            }
        //        };

        //        await _bot.(userId, sendElement);
        //        _logger.LogInformation("私聊消息已发送至用户 {UserId}", userId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "发送私聊消息失败，用户ID: {UserId}", userId);
        //    }
        //}
    }
}
