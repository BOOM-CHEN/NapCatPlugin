using NapCatPlugin.Services;
using NapPlana.Core.Bot;
using NapPlana.Core.Data;
using NapPlana.Core.Event.Handler;
using TouchSocket.Sockets;

namespace NapCatPlugin
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddHttpClient("QASearchService");

            BotEventHandler.OnLogReceived += (level, message) =>
            {
                if (level == NapPlana.Core.Data.LogLevel.Debug)
                {
                    return;
                }
                Console.WriteLine($"[{level}] {message}");
            };
            var bot = PlanaBotFactory
                .Create()
                .SetConnectionType(BotConnectionType.WebSocketClient)
                .SetIp("192.168.34.200")
                .SetPort(6098)
                .SetToken("2L5LCgd4v8bMGHkA")
                .Build();
            await bot.StartAsync();

            builder.Services.AddSingleton(bot);
            builder.Services.AddHostedService<BotEventService>();
            builder.Services.AddSingleton<IQASearchService, QASearchService>();


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
