using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using static WhatsApp_Statistics.Message;

namespace WhatsApp_Statistics
{
    internal class Chat
    {
        private static readonly string FMPEG_FILE_PATH = Environment.GetEnvironmentVariable("FMPEG_FILE_PATH");
        private const string WHATSAPP_SENDER = "WhatsApp";
        private const string NULL = "null";
        private const string ANDROID_NO_MEDIA_ATTACHED = "<Medien ausgeschlossen>";
        private const string IPHONE_NO_MEDIA_ATTACHED = "weggelassen";

        public static void SaveAsJson(Chat chat, string filePath)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(chat, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static Chat ReadFromJson(string filePath)
        {
            string str = File.ReadAllText(filePath);
            Chat chat = JsonSerializer.Deserialize<Chat>(str);
            return chat;
        }

        public static async Task<Chat> TxtToChat(string title, string filePath, bool isAndroid, bool includeMediaDuration)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            FFmpeg.SetExecutablesPath(FMPEG_FILE_PATH);
            const char invSpecialChar = '\u200e'; // Invisible special character in front of all non-text messages

            string folderPath = filePath.Substring(0, filePath.LastIndexOf('\\'));
            Chat chat = new Chat(title, isAndroid, includeMediaDuration, new List<Message>());

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

                        // Only happens for IPhone Media messages
                        bool firstLineCharIsu200e = false;
                        if (line[0] == invSpecialChar)
                        {
                            firstLineCharIsu200e = true;
                            line = line.Substring(1); // Remove leading invisible special character
                        }

                        // Android: DateTime - Sender
                        // IPhone: [DatenTime] Sender
                        string dateTimeAsString = isAndroid
                            ? dateTimeAsString = line.Split('-')[0]
                            : line.Split(']')[0].Substring(1); // Substring(1) to remove the leading '['

                        // If the date and time can't be parsed, it is the second line of the content of the previous message and not a new one
                        if (!DateTime.TryParse(dateTimeAsString, out DateTime dateTime)) // The trailing whitespace is ignored by DateTime.TryParse()
                        {
                            if (prevMsg == null) throw new Exception("First line of chat log could not be read as a message. This might be due to the beginning of the message missing.");
                            prevMsg.AppendToContent(line);
                            continue;
                        }

                        // Android: DateTime - Sender
                        // IPhone: [DatenTime] Sender
                        string lineWithoutDateTime = isAndroid
                            ? line.Substring(dateTimeAsString.Length + 2) // +2 to remove "- "
                            : line.Substring(dateTimeAsString.Length + 3); // +3 to skip '[' and then to remove "] "

                        // Both: Sender:
                        string[] splittedLineWithoutDateTime = lineWithoutDateTime.Split(':');

                        string sender;
                        string content;
                        int length;
                        Message.MessageType messageType;

                        // Android System message without sender
                        if (isAndroid && splittedLineWithoutDateTime.Length == 1)
                        {
                            sender = WHATSAPP_SENDER;
                            content = lineWithoutDateTime;
                            messageType = Message.MessageType.System;
                            length = int.MinValue;
                        }
                        else
                        {
                            // KNOWN PROBLEM: If there is a ':' in the sender name
                            sender = lineWithoutDateTime.Split(':')[0];

                            bool firstContentCharIsu200e;
                            if (lineWithoutDateTime.Split(':')[1].Length == 0)
                            {
                                content = "";
                                firstContentCharIsu200e = false;
                            }
                            else
                            {
                                content = lineWithoutDateTime.Substring(lineWithoutDateTime.IndexOf(':') + 2); // +2 to skip ": "
                                firstContentCharIsu200e = content[0] == invSpecialChar;
                            }

                            if (!firstContentCharIsu200e // Text message
                                && !(content.Equals(NULL) // Includes deleted for all, onetime-view and calls on Android
                                && !content.Equals(ANDROID_NO_MEDIA_ATTACHED))) // Includes locally deleted media on Android
                            {
                                messageType = Message.MessageType.Text;
                                length = content.Length;
                            }
                            else if (content.Equals(NULL) || content.Equals(ANDROID_NO_MEDIA_ATTACHED) // Some Android System message
                                || content.Contains(IPHONE_NO_MEDIA_ATTACHED) // IPhone System message: no media attached
                                || (!isAndroid && !firstLineCharIsu200e)) // Most IPhone System message
                            {
                                messageType = Message.MessageType.System;
                                length = int.MinValue;
                            }
                            else // Media message
                            {
                                // Android: fileName.fileType (...
                                // IPhone: <Anhang: fileName.fileType>
                                content = content.Substring(1); // Remove the leading special character
                                string fileName;
                                if (isAndroid) fileName = content.Substring(0, content.LastIndexOf('(') - 1); // -1 to remove the trailing whitespace
                                else fileName = content.Split(':')[1].TrimEnd('>').Substring(1); // To remove the leading leading invisible special character and the trailing '>'

                                string mediaPath = Path.Combine(
                                    folderPath,
                                    fileName);

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
                                    messageType = MessageType.Image;
                                    length = fileSize;
                                }
                                else if (content.Contains(".opus"))
                                {
                                    messageType = MessageType.Voicenote;
                                    length = duration;
                                }
                                else if (content.Contains(".mp4"))
                                {
                                    messageType = MessageType.Video;
                                    length = duration;
                                }
                                else if (content.Contains(".webp"))
                                {
                                    messageType = MessageType.Sticker;
                                    length = fileSize;
                                }
                                else
                                {
                                    messageType = MessageType.Files;
                                    length = fileSize;
                                }
                            }
                        }

                        Message message = new Message(dateTime, sender, content, messageType, length, includeMediaDuration);
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

        public string Title { get; }
        public bool IsAndroid { get; }
        public bool IncludeMediaDuration { get; }
        public List<Message> Messages { get; }

        public Chat(string title, bool isAndroid, bool includeMediaDuration, List<Message> messages) {
            this.Title = title;
            this.IsAndroid = isAndroid;
            this.IncludeMediaDuration = includeMediaDuration;
            this.Messages = messages;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Chat: {Title}");
            Messages.ForEach(msg => sb.AppendLine(msg.ToString()));
            return sb.ToString();
        }

        public void Print()
        {
            Console.WriteLine($"Chat: {Title}");
            Messages.ForEach(msg => msg.Print());
        }

        public void AddMessage(Message message)
        {
            Messages.Add(message);
        }
    }
}
