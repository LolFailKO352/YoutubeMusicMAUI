using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text;

namespace YoutubeMusic.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, string> _cache;

        public TranslationService()
        {
            _httpClient = new HttpClient();
            _cache = new ConcurrentDictionary<string, string>();
        }

        public async Task<string> TranslateAsync(string text, string targetLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            if (string.IsNullOrWhiteSpace(targetLanguageCode) || targetLanguageCode == "cs") return text; // Base language is Czech

            string cacheKey = $"{targetLanguageCode}:{text}";
            if (_cache.TryGetValue(cacheKey, out string? cachedTranslation))
            {
                return cachedTranslation;
            }

            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=cs&tl={targetLanguageCode}&dt=t&q={Uri.EscapeDataString(text)}";
                var response = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var sentencesArray = root[0];
                    if (sentencesArray.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var sentence in sentencesArray.EnumerateArray())
                        {
                            if (sentence.ValueKind == JsonValueKind.Array && sentence.GetArrayLength() > 0)
                            {
                                sb.Append(sentence[0].GetString());
                            }
                        }
                        string result = sb.ToString();
                        _cache[cacheKey] = result;
                        return result;
                    }
                }
                
                return text; // fallback
            }
            catch
            {
                return text; // fallback on error
            }
        }
    }
}
