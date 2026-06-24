using System;

namespace YoutubeMusic.Models
{
    public class AlbumModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
    }
}
