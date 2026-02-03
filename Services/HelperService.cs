using NapPlana.Core.Data;
using NapPlana.Core.Data.Message;

namespace NapCatPlugin.Services
{
    public static class HelperService
    {
        public static string ExtractMessageContent(List<MessageBase>? messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return string.Empty;
            }

            var contents = new List<string>();
            foreach (var msg in messages)
            {
                if (msg.MessageData != null)
                {
                    string? text = ExtractTextFromMessageData(msg.MessageData, msg.MessageType);
                    if (!string.IsNullOrEmpty(text))
                    {
                        contents.Add(text);
                    }
                }
            }

            return string.Join("", contents);
        }

        public static string? ExtractTextFromMessageData(object messageData, MessageDataType messageType)
        {
            try
            {
                if (messageType == MessageDataType.Text)
                {
                    var textProperty = messageData.GetType().GetProperty("Text");
                    if (textProperty != null)
                    {
                        return textProperty.GetValue(messageData)?.ToString();
                    }
                }

                var contentProperty = messageData.GetType().GetProperty("Content");
                if (contentProperty != null)
                {
                    return contentProperty.GetValue(messageData)?.ToString();
                }

                var dataProperty = messageData.GetType().GetProperty("Data");
                if (dataProperty != null)
                {
                    var dataValue = dataProperty.GetValue(messageData);
                    if (dataValue is IDictionary<string, object> dict && dict.ContainsKey("content"))
                    {
                        return dict["content"]?.ToString();
                    }
                }

                return messageData.ToString();
            }
            catch
            {
                return messageData.ToString();
            }
        }
    }
}
