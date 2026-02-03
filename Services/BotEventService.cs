using NapPlana.Core.Event.Handler;

namespace NapCatPlugin.Services
{
    public class BotEventService : BackgroundService
    {
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
            BotEventHandler.OnGroupMessageReceived += (eventData) =>
            {
                Console.WriteLine($"私聊消息类型 {eventData.MessageType}, 消息ID: {eventData.MessageId}");
            };
            BotEventHandler.OnPrivateMessageReceived += (eventData) =>
            {
                Console.WriteLine($"私聊消息类型 {eventData.MessageType}, 消息ID: {eventData.MessageId}, 消息内容：{eventData.Messages}");
            };

            return Task.CompletedTask;
        }
    }
}
