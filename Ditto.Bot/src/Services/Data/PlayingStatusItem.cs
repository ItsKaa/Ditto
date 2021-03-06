﻿using Ditto.Data.Discord;
using System;

namespace Ditto.Bot.Services.Data
{
    public class PlayingStatusItem<T>
    {
        public int Id { get; private set; }
        public Func<DiscordClientEx, T> Function { get; set; }
        public PriorityLevel Priority { get; set; }
        //public TimeSpan? Delay { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastTime { get; set; }

        public PlayingStatusItem(int id)
        {
            Id = id;
            Function = default(Func<DiscordClientEx, T>);
            Priority = PriorityLevel.Normal;
            //Delay = null;
            DateAdded = DateTime.Now;
            LastTime = DateTime.MinValue;
        }

        public T Execute(DiscordClientEx client)
        {
            LastTime = DateTime.Now;
            return Function(client);
        }
    }
}
