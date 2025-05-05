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

        public DateTime DateTime { get; }
        public string Sender { get; }
        public string Content { get; set; }
        public MessageType Type { get; }
        public int Length { get; set; }
        public bool IsDuration { get; }

        public Message(DateTime dateTime, string sender, string content, MessageType type, int length, bool isDuration)
        {
            this.DateTime = dateTime;
            this.Sender = sender;
            this.Content = content;
            this.Type = type;
            this.Length = length;
            this.IsDuration = isDuration;
        }

        public override string ToString()
        {
            string lengthString;
            switch (Type)
            {
                case MessageType.Image: case MessageType.Sticker: case MessageType.Files:
                        lengthString = HumanReadableSize(Length); break;

                case MessageType.Voicenote: case MessageType.Video:
                    {
                        if (IsDuration) lengthString = HumanReadableDuration(Length);
                        else lengthString = HumanReadableSize(Length);

                        break;
                    }

                default:
                    lengthString = $"{Length} chars";  break;
            }

            return String.Format($"{DateTime} - {Sender} - {Type} - {lengthString}: {Content}");
        }

        public void Print()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(DateTime);
            
            Console.ResetColor();
            Console.Write(" - ");
            
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(Sender);
            
            Console.ResetColor();
            Console.Write(": ");
            
            Console.WriteLine(Content);
        }

        public void AppendToContent(string appendix)
        {
            Content = String.Concat(Content, Environment.NewLine, appendix);
            if (Type == MessageType.Text) Length = Content.Length;
        }
    }
}
