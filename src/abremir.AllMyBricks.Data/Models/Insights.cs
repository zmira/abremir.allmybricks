﻿using System;
using LiteDB;

namespace abremir.AllMyBricks.Data.Models
{
    public class Insights
    {
        [BsonId(false)]
        public byte Id { get; set; }

        public DateTimeOffset? DataSynchronizationTimestamp { get; set; }
    }
}
