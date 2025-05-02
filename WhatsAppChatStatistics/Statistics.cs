using System;
using System.Collections.Generic;
using System.Linq;
using static WhatsApp_Statistics.Message;

namespace WhatsApp_Statistics
{
    internal class Statistics
    {
        private readonly List<Message> messages;
        private readonly List<string> senders;

        public Statistics(Chat chat)
        {
            this.messages = chat.messages;
            this.senders = GetSenders();
        }

        public List<string> GetSenders()
        {
            List<string> senders = new List<string>();
            messages.ForEach(msg =>
            {
                senders.Add(msg.sender);
            });

            return senders.Distinct().ToList();
        }

        public List<Message> GetMessages(DateTime? from = null, DateTime? to = null, string[] senders = null,
            string[] contentContainsAny = null, MessageType[] messageTypes = null,
            int minLength = int.MinValue, int maxLength = int.MaxValue)
        {
            if (from == null) from = DateTime.MinValue;
            if (to == null) to = DateTime.MaxValue;
            if (senders == null || senders.Length == 0) senders = this.senders.ToArray();
            if (contentContainsAny == null || contentContainsAny.Length == 0) contentContainsAny = new string[] {""};
            if (messageTypes == null || messageTypes.Length == 0) messageTypes = (MessageType[])Enum.GetValues(typeof(MessageType));

            return messages.Where(msg =>
                from <= msg.dateTime
                && msg.dateTime <= to
                && senders.Contains(msg.sender)
                && contentContainsAny.Any(str => msg.content.Contains(str))
                && messageTypes.Contains(msg.messageType)
                && minLength <= msg.length
                && msg.length <= maxLength
            ).ToList();
        }

        // TODO: add filter for daytime, weekday and add word search with 'And' instead of 'Or'

    }
}
