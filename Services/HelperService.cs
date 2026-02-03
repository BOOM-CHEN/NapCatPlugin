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
                string? text = ExtractTextFromMessageData(msg.MessageData, msg.MessageType);
                if (!string.IsNullOrEmpty(text))
                {
                    contents.Add(text);
                }
            }

            return string.Join("", contents);
        }

        public static string? ExtractTextFromMessageData(object? messageData, MessageDataType messageType)
        {
            if (messageData == null)
            {
                return null;
            }

            try
            {
                switch (messageType)
                {
                    case MessageDataType.Text:
                        var textProperty = messageData.GetType().GetProperty("Text");
                        if (textProperty != null)
                        {
                            return textProperty.GetValue(messageData)?.ToString();
                        }
                        break;

                    case MessageDataType.At:
                        var qqProperty = messageData.GetType().GetProperty("Qq");
                        if (qqProperty != null)
                        {
                            var qqValue = qqProperty.GetValue(messageData)?.ToString();
                            if (qqValue != null && qqValue != "all")
                            {
                                return $"[CQ:at,qq={qqValue}]";
                            }
                            else if (qqValue == "all")
                            {
                                return "[CQ:at,qq=all]";
                            }
                        }
                        break;

                    case MessageDataType.Face:
                        var faceProperty = messageData.GetType().GetProperty("FaceId");
                        if (faceProperty != null)
                        {
                            var faceId = faceProperty.GetValue(messageData)?.ToString();
                            if (!string.IsNullOrEmpty(faceId))
                            {
                                return $"[CQ:face,id={faceId}]";
                            }
                        }
                        break;

                    case MessageDataType.Image:
                        var imageProperty = messageData.GetType().GetProperty("Url");
                        if (imageProperty != null)
                        {
                            var url = imageProperty.GetValue(messageData)?.ToString();
                            if (!string.IsNullOrEmpty(url))
                            {
                                return $"[CQ:image,file={url}]";
                            }
                        }
                        break;
                }

                var contentProperty = messageData.GetType().GetProperty("Content");
                if (contentProperty != null)
                {
                    return contentProperty.GetValue(messageData)?.ToString();
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
