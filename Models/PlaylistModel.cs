using System;

namespace YoutubeMusic.Models
{
    public class PlaylistModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int SongCount { get; set; }
        public string Creator { get; set; } = string.Empty;
    }
}
