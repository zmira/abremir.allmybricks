﻿using Realms;

namespace abremir.AllMyBricks.Data.Models.Realm
{
    internal class Image : RealmObject
    {
        public string ThumbnailUrl { get; set; }
        public string LargeThumbnailUrl { get; set; }
        public string ImageUrl { get; set; }
    }
}