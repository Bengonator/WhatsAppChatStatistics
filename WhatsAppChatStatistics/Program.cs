using dotenv.net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static WhatsApp_Statistics.Message;

namespace WhatsApp_Statistics
{
    internal class Program
    {
        private static void WH(string str = "")
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(str);
            Console.ResetColor();
        }

        private static void W(string str = "")
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(str);
            Console.ResetColor();
        }

        private static void WL(string str = "")
        {
            Console.WriteLine(str);
        }

        static async Task Main()
        {
            DotEnv.Load(new DotEnvOptions(envFilePaths: new[]{ Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", ".env") }));
            string FOLDER_PATH = Environment.GetEnvironmentVariable("FOLDER_PATH");
            string TXT_FILENAME = Environment.GetEnvironmentVariable("TXT_FILENAME");
            string FILE_PATH = Path.Combine(FOLDER_PATH, TXT_FILENAME);

            bool includeMediaDuration = false;
            Chat chat;
            try
            {
                Console.WriteLine("Reading from chatlog...");
                chat = await Chat.TxtToChat("title of chat", FILE_PATH, includeMediaDuration);
                Console.WriteLine("Finished reading from chatlog.");
            }
            catch (Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error while reading from chat log:");
                Console.WriteLine(FILE_PATH);
                Console.WriteLine();
                Console.WriteLine("Full error message:");
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
                Console.ReadLine();
                return;
            }

            PrintAllStatistics(chat);
            Console.ReadLine();
        }

        private static void PrintAllStatistics(Chat chat)
        {
            Statistics stats = new Statistics(chat);
            WL();

            W("Total amount of messages: ");
            WL(stats.GetMessages().Count().ToString());
            WL();

            WH("Amount of messages of all senders: ");
            Dictionary<string, int> numPerAllSenders = stats.GetMessages()
                .GroupBy(msg => msg.sender)
                .ToDictionary(group => group.Key, g => g.Count());

            foreach (KeyValuePair<string, int> pair in numPerAllSenders.OrderByDescending(pair => pair.Value))
            {
                W($"{pair.Key}: ");
                WL(pair.Value.ToString());
            }
            WL();

            WH("Amount of messages of all types: ");
            Dictionary<MessageType, int> numPerAllTypes = stats.GetMessages()
                .GroupBy(msg => msg.messageType)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (KeyValuePair<MessageType, int> pair in numPerAllTypes.OrderByDescending(pair => pair.Value))
            {
                W($"{pair.Key}: ");
                WL(pair.Value.ToString());
            }
            WL();

            WH("VoiceNote duration per sender:");
            Dictionary<string, int> durPerSender = stats.GetMessages(messageTypes: new[] {MessageType.Voicenote})
                .GroupBy(msg => msg.sender)
                .ToDictionary(group => group.Key, group => group.Sum(msg => msg.length));

            foreach (KeyValuePair<string, int> pair in durPerSender.OrderByDescending(pair => pair.Value))
            {
                W($"{pair.Key}: ");
                WL(pair.Value.ToString());
            }
            WL();

            chat.Print();
        }
    }
}
