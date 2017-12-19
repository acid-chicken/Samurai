using System;
using LiteDB;

namespace AcidChicken.Samurai.Discord.Models
{
    public class TipRequest
    {
        public TipRequest() { }

        public TipRequest(ulong from, ulong to, decimal amount, DateTimeOffset limit)
        {
            From = from;
            To = to;
            Amount = amount;
            Limit = limit;
        }

        public ObjectId Id { get; set; }

        public ulong From { get; set; }

        public ulong To { get; set; }

        public decimal Amount { get; set; }

        public DateTimeOffset Limit { get; set; }
    }
}
