using System;
using System.Collections.Generic;
using System.Text;

namespace YoutubeMusic.Models
{
    public class SongModel
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
