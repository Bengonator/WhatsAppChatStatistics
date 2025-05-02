using System;

namespace WhatsApp_Statistics
{
    internal class Message
    {
        public static string HumanReadableSize(int size)
        {
            if (size == 0) return "0 B";

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unitIndex = (int)Math.Floor(Math.Log(size, 1024));
            double adjustedSize = size / Math.Pow(1024, unitIndex);

            return $"{adjustedSize:0.##} {units[unitIndex]}";
        }

        public static string HumanReadableDuration(int duration)
        {
            if (duration < 60)
                return $"{duration} seconds";

            double minutes = duration / 60.0;
            if (minutes < 60)
                return $"{minutes:0.##} minutes";

            double hours = duration / 3600.0;
            if (hours < 24)
                return $"{hours:0.##} hours";

            double days = duration / 86400.0;
            return $"{days:0.##} days";
        }

        public enum MessageType
        {
            Text,
            Image,
            Voicenote,
            Video,
            Sticker,
            Files,
            System, // encryption information, created group, added to group, changed group settings, deleted for all, locally deleted media, onetime-view
        }

        public readonly DateTime dateTime;
        public readonly string sender;
        public string content;
        public readonly MessageType messageType;
        public int length;
        private readonly bool isDuration;

        public Message(DateTime dateTime, string sender, string content, MessageType messageType, int length, bool isDuration)
        {
            this.dateTime = dateTime;
            this.sender = sender;
            this.content = content;
            this.messageType = messageType;
            this.length = length;
            this.isDuration = isDuration;
        }

        public override string ToString()
        {
            string lengthString;
            switch (messageType)
            {
                case MessageType.Image: case MessageType.Sticker: case MessageType.Files:
                        lengthString = HumanReadableSize(length); break;

                case MessageType.Voicenote: case MessageType.Video:
                    {
                        if (isDuration) lengthString = HumanReadableDuration(length);
                        else lengthString = HumanReadableSize(length);

                        break;
                    }

                default:
                    lengthString = $"{length} chars";  break;
            }

            return String.Format($"{dateTime} - {sender} - {messageType} - {lengthString}: {content}");
        }

        public void Print()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(dateTime);
            
            Console.ResetColor();
            Console.Write(" - ");
            
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(sender);
            
            Console.ResetColor();
            Console.Write(": ");
            
            Console.WriteLine(content);
        }

        public void AppendToContent(string appendix)
        {
            content = String.Concat(content, Environment.NewLine, appendix);
            if (messageType == MessageType.Text) length = content.Length;
        }
    }
}
