using System;

namespace WhatsApp_Statistics
{
    internal class Message
    {
        public enum MessageType
        {
            Text,
            Image,
            Voicenote,
            Video,
            Sticker,
            Files,
            System, // encryption information, created group, added to group, changed group settings
            Ghost, // deleted for all, locally deleted media, onetime-view, calls
        }

        public readonly DateTime dateTime;
        public readonly string sender;
        public string content;
        public readonly MessageType messageType;
        public int length;

        public Message(DateTime dateTime, string sender, string content, MessageType messageType, int length)
        {
            this.dateTime = dateTime;
            this.sender = sender;
            this.content = content;
            this.messageType = messageType;
            this.length = length;
        }

        public override string ToString()
        {
            string lengthUnit;
            switch (messageType)
            {
                case MessageType.Image: case MessageType.Sticker: case MessageType.Files: lengthUnit = "bytes"; break;
                case MessageType.Voicenote: case MessageType.Video: lengthUnit = "seconds"; break;
                default: lengthUnit = "chars"; break;
            }

            return String.Format($"{dateTime} - {sender} - {messageType} - {length} {lengthUnit}: {content}");
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
