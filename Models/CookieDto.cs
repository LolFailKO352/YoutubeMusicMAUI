using System;

namespace Melodium.Models
{
    public class CookieDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Domain { get; set; } = ".youtube.com";
        public string Path { get; set; } = "/";
    }
}
