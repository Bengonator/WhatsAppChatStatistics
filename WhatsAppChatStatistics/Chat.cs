using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace WhatsApp_Statistics
{
    internal class Chat
    {
        private static readonly string FMPEG_FILE_PATH = Environment.GetEnvironmentVariable("FMPEG_FILE_PATH");
        private const string WHATSAPP_SENDER = "WhatsApp";
        private const string NULL = "null";
        private const string NO_MEDIA_ATTACHED = "<Medien ausgeschlossen>";

        public static async Task<Chat> TxtToChat(string title, string filePath , bool includeMediaDuration)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            FFmpeg.SetExecutablesPath(FMPEG_FILE_PATH);

            int lastIdxSlash = filePath.LastIndexOf('\\');
            string folderPath = filePath.Substring(0, lastIdxSlash);
            Chat chat = new Chat(title);

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    Message prevMsg = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Empty line
                        if (line.Length == 0)
                        {
                            prevMsg?.AppendToContent("");
                            continue;
                        }

                        string dateTimeAsString;
                        dateTimeAsString = line.Split('-')[0];
                        
                        // If the date and time can't be parsed, it is the second line of the content of the previous message and not a new one
                        if (!DateTime.TryParse(dateTimeAsString, out DateTime dateTime)) // The trailing whitespace is ignored by DateTime.TryParse()
                        {
                            if (prevMsg == null) throw new Exception("First line of chat log could not be read as a message. This might be due to the beginning of the message missing.");
                            prevMsg.AppendToContent(line);
                            continue;
                        }

                        string lineWithoutDateTime = line.Substring(dateTimeAsString.Length + 2); // +2 to remove "- "
                        string[] splittedLineWithoutDateTime = lineWithoutDateTime.Split(':');

                        string sender;
                        string content;
                        int length;
                        Message.MessageType messageType;
                        if (splittedLineWithoutDateTime.Length == 1) // Happens for example for the encryption notifications at the beginning of each chat
                        {
                            sender = WHATSAPP_SENDER;
                            content = lineWithoutDateTime.Split(':')[0];
                            messageType = Message.MessageType.System;
                            length = int.MinValue;

                            if (content[0] == '\u200e') content = content.Substring(1); // Remove the leading invisible special character
                        }
                        else
                        {
                            sender = lineWithoutDateTime.Split(':')[0]; // Can cause problems if there is a ':' in the sender name
                            content = lineWithoutDateTime.Split(':')[1].Substring(1); // Remove the leading whitespace

                            if (content[0] != '\u200e') // Invisible special character in front of all non-text messages
                            {
                                if (content.Equals(NULL) || content.Equals(NO_MEDIA_ATTACHED))
                                {
                                    messageType = Message.MessageType.Ghost;
                                    length = int.MinValue;
                                }
                                else
                                {
                                    messageType = Message.MessageType.Text;
                                    length = content.Length;
                                }
                            }
                            else // Media message
                            {
                                content = content.Substring(1); // Remove the leading special character
                                string mediaPath = String.Concat(
                                    folderPath,
                                    "\\",
                                    content.Substring(0, content.LastIndexOf('(') - 1) // -1 to remove the trailing whitespace
                                );

                                int fileSize = 0;
                                int duration = 0;

                                if (File.Exists(mediaPath))
                                {
                                    fileSize = (int)new FileInfo(mediaPath).Length;
                                    duration = includeMediaDuration ? (int)(await FFmpeg.GetMediaInfo(mediaPath)).Duration.TotalSeconds : fileSize;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    throw new FileNotFoundException($"Could not find file '{mediaPath}'");
                                }

                                if (content.Contains(".jpg"))
                                {
                                    messageType = Message.MessageType.Image;
                                    length = fileSize;
                                }
                                else if (content.Contains(".opus"))
                                {
                                    messageType = Message.MessageType.Voicenote;
                                    length = duration;
                                }
                                else if (content.Contains(".mp4"))
                                {
                                    messageType = Message.MessageType.Video;
                                    length = duration;
                                }
                                else if (content.Contains(".webp"))
                                {
                                    messageType = Message.MessageType.Sticker;
                                    length = fileSize;
                                }
                                else
                                {
                                    messageType = Message.MessageType.Files;
                                    length = fileSize;
                                }
                            }
                        }

                        Message message = new Message(dateTime, sender, content, messageType, length);
                        chat.AddMessage(message);
                        prevMsg = message;
                    }
                } 
            }
            catch (Exception exc)
            {
                throw exc;
            }

            return chat;
        }

        public readonly string title;
        public readonly List<Message> messages = new List<Message>();

        public Chat(string title) {
            this.title = title;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Chat: {title}");
            messages.ForEach(msg => sb.AppendLine(msg.ToString()));
            return sb.ToString();
        }

        public void Print()
        {
            Console.WriteLine($"Chat: {title}");
            messages.ForEach(msg => msg.Print());
        }

        public void AddMessage(Message message)
        {
            messages.Add(message);
        }
    }
}
