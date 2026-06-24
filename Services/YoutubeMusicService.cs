using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using YoutubeMusic.Models;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models.Search;
using YouTubeMusicAPI.Models.Library;
using YouTubeMusicAPI.Models.Info;
using YouTubeSessionGenerator;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
namespace YoutubeMusic.Services
{
    public class YouTubeMusicService
    {
        private YouTubeMusicClient _client;
        private string _visitorData = "";
        private string _poToken = "";
        private bool _isInitialized = false;
        private IEnumerable<Cookie>? _currentCookies;
        private System.Net.Http.HttpClient _httpClient;
        private readonly YoutubeClient _ytExplodeClient;

        public YouTubeMusicService()
        {
            _ytExplodeClient = new YoutubeClient();
            // Výchozí inicializace bez cookies
            InitializeClient(null);
        }

        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

#if WINDOWS
            try 
            {
                var jsEnv = new YouTubeSessionGenerator.Js.Environments.NodeEnvironment();
                var config = new YouTubeSessionConfig { JsEnvironment = jsEnv };
                var creator = new YouTubeSessionCreator(config);
                _visitorData = await creator.VisitorDataAsync();
                _poToken = await creator.ProofOfOriginTokenAsync(_visitorData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při generování PoTokenu: {ex.Message}");
            }
#endif
            _isInitialized = true;
            InitializeClient(_currentCookies);
        }

        public void InitializeClient(IEnumerable<Cookie>? cookies)
        {
            _currentCookies = cookies;
            _client = new YouTubeMusicClient(
                visitorData: _visitorData,
                poToken: _poToken,
                cookies: cookies
            );
            
            // Set up cookies for raw HttpClient if available
            var handler = new System.Net.Http.HttpClientHandler();
            string sapisid = null;

            if (cookies != null)
            {
                handler.CookieContainer = new System.Net.CookieContainer();
                foreach (var c in cookies)
                {
                    if (c.Name == "SAPISID" || c.Name == "__Secure-3PAPISID") sapisid = c.Value;
                    handler.CookieContainer.Add(new Uri("https://music.youtube.com"), c);
                }
            }
            _httpClient = new System.Net.Http.HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("X-Origin", "https://music.youtube.com");
            
            if (!string.IsNullOrEmpty(sapisid))
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string input = $"{timestamp} {sapisid} https://music.youtube.com";
                using var sha1 = System.Security.Cryptography.SHA1.Create();
                byte[] hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"SAPISIDHASH {timestamp}_{hash}");
            }
        }

        public async Task<List<SongModel>> SearchSongsAsync(string query)
        {
            var results = new List<SongModel>();

            try
            {
                var searchResults = _client.SearchAsync(query, SearchCategory.Songs);
                var bufferedSearchResults = await searchResults.FetchItemsAsync(0, 20);

                foreach (var song in bufferedSearchResults.Cast<SongSearchResult>())
                {
                    string artistName = song.Artists?.FirstOrDefault()?.Name ?? "Neznámý interpret";

                    results.Add(new SongModel
                    {
                        VideoId = song.Id,
                        Title = song.Name,
                        Artist = artistName,
                        ThumbnailUrl = song.Thumbnails?.FirstOrDefault()?.Url
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při hledání: {ex.Message}");
            }

            return results;
        }

        public async Task<string?> GetAudioStreamUrlAsync(string videoId)
        {
            try
            {
                var streamManifest = await _ytExplodeClient.Videos.Streams.GetManifestAsync(videoId);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (audioStreamInfo != null)
                {
                    return audioStreamInfo.Url;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při získávání URL streamu přes YoutubeExplode: {ex.Message}");
                return null;
            }
        }

        // --- Nové knihovní metody ---

        public async Task<List<SongModel>> GetLibrarySongsAsync()
        {
            var results = new List<SongModel>();
            try
            {
                var songs = await _client.GetLibrarySongsAsync();
                if (songs != null)
                {
                    foreach (var song in songs)
                    {
                        string artistName = song.Artists?.FirstOrDefault()?.Name ?? "Neznámý interpret";
                        results.Add(new SongModel
                        {
                            VideoId = song.Id,
                            Title = song.Name,
                            Artist = artistName,
                            ThumbnailUrl = song.Thumbnails?.FirstOrDefault()?.Url
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování skladeb z knihovny: {ex.Message}");
            }
            return results;
        }

        public async Task<List<PlaylistModel>> GetLibraryPlaylistsAsync()
        {
            var results = new List<PlaylistModel>();
            try
            {
                var playlists = await _client.GetLibraryCommunityPlaylistsAsync();
                if (playlists != null)
                {
                    foreach (var playlist in playlists)
                    {
                        results.Add(new PlaylistModel
                        {
                            Id = playlist.Id,
                            Title = playlist.Name,
                            ThumbnailUrl = playlist.Thumbnails?.FirstOrDefault()?.Url,
                            SongCount = playlist.SongCount,
                            Creator = playlist.Creator?.Name ?? "Komunita"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování playlistů z knihovny: {ex.Message}");
            }
            return results;
        }

        public async Task<List<AlbumModel>> GetLibraryAlbumsAsync()
        {
            var results = new List<AlbumModel>();
            try
            {
                var albums = await _client.GetLibraryAlbumsAsync();
                if (albums != null)
                {
                    foreach (var album in albums)
                    {
                        string artistName = album.Artists?.FirstOrDefault()?.Name ?? "Neznámý interpret";
                        results.Add(new AlbumModel
                        {
                            Id = album.Id,
                            Title = album.Name,
                            ThumbnailUrl = album.Thumbnails?.FirstOrDefault()?.Url,
                            ArtistName = artistName,
                            ReleaseYear = album.ReleaseYear
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování alb z knihovny: {ex.Message}");
            }
            return results;
        }

        public async Task<List<ArtistModel>> GetLibraryArtistsAsync()
        {
            var results = new List<ArtistModel>();
            try
            {
                var artists = await _client.GetLibraryArtistsAsync();
                if (artists != null)
                {
                    foreach (var artist in artists)
                    {
                        results.Add(new ArtistModel
                        {
                            Id = artist.Id,
                            Name = artist.Name,
                            ThumbnailUrl = artist.Thumbnails?.FirstOrDefault()?.Url,
                            SongCount = artist.SongCount
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování interpretů z knihovny: {ex.Message}");
            }
            return results;
        }

        public async Task<List<SongModel>> GetPlaylistSongsAsync(string playlistId)
        {
            var results = new List<SongModel>();
            try
            {
                var playlistSongs = _client.GetCommunityPlaylistSongsAsync(playlistId);
                var items = await playlistSongs.FetchItemsAsync(0, 100);
                foreach (var song in items)
                {
                    string artistName = song.Artists?.FirstOrDefault()?.Name ?? "Neznámý interpret";
                    results.Add(new SongModel
                    {
                        VideoId = song.Id,
                        Title = song.Name,
                        Artist = artistName,
                        ThumbnailUrl = song.Thumbnails?.FirstOrDefault()?.Url
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování skladeb playlistu: {ex.Message}");
            }
            return results;
        }

        public async Task<List<SongModel>> GetAlbumSongsAsync(string albumId)
        {
            var results = new List<SongModel>();
            try
            {
                var albumInfo = await _client.GetAlbumInfoAsync(albumId);
                if (albumInfo?.Songs != null)
                {
                    string artistName = albumInfo.Artists?.FirstOrDefault()?.Name ?? "Neznámý interpret";
                    string? thumbnailUrl = albumInfo.Thumbnails?.FirstOrDefault()?.Url;

                    foreach (var song in albumInfo.Songs)
                    {
                        results.Add(new SongModel
                        {
                            VideoId = song.Id,
                            Title = song.Name,
                            Artist = artistName,
                            ThumbnailUrl = thumbnailUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování skladeb alba: {ex.Message}");
            }
            return results;
        }
        // --- Nové funkce pro Domů a Rádio ---

        public async Task<List<SongModel>> GetHomeRecommendationsAsync()
        {
            var results = new List<SongModel>();
            try
            {
                var body = new {
                    context = new {
                        client = new {
                            clientName = "WEB_REMIX",
                            clientVersion = "1.20230508.01.00",
                            hl = "cs",
                            gl = "CZ",
                            visitorData = _visitorData
                        }
                    },
                    browseId = "FEmusic_home"
                };

                var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://music.youtube.com/youtubei/v1/browse", content);
                response.EnsureSuccessStatusCode();

                var jsonStr = await response.Content.ReadAsStringAsync();
                var root = System.Text.Json.Nodes.JsonNode.Parse(jsonStr);
                
                ExtractSongsFromNode(root, results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování Home: {ex.Message}");
            }
            return results.GroupBy(r => r.VideoId).Select(g => g.First()).Take(40).ToList();
        }

        public async Task<List<SongModel>> GetUpNextRadioAsync(string videoId)
        {
            var results = new List<SongModel>();
            try
            {
                var body = new {
                    context = new {
                        client = new {
                            clientName = "WEB_REMIX",
                            clientVersion = "1.20230508.01.00",
                            hl = "cs",
                            gl = "CZ",
                            visitorData = _visitorData
                        }
                    },
                    videoId = videoId
                };

                var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://music.youtube.com/youtubei/v1/next", content);
                response.EnsureSuccessStatusCode();

                var jsonStr = await response.Content.ReadAsStringAsync();
                var root = System.Text.Json.Nodes.JsonNode.Parse(jsonStr);
                
                ExtractSongsFromNode(root, results);
                
                // Odstranit aktuálně přehrávaný song a vzít limit
                results.RemoveAll(r => r.VideoId == videoId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při stahování Up Next Rádia: {ex.Message}");
            }
            return results.GroupBy(r => r.VideoId).Select(g => g.First()).Take(30).ToList();
        }

        private void ExtractSongsFromNode(System.Text.Json.Nodes.JsonNode? node, List<SongModel> results)
        {
            if (node == null) return;

            if (node is System.Text.Json.Nodes.JsonObject obj)
            {
                if (obj.ContainsKey("musicTwoRowItemRenderer"))
                {
                    var renderer = obj["musicTwoRowItemRenderer"];
                    var videoId = renderer?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString();
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        var title = renderer?["title"]?["runs"]?[0]?["text"]?.ToString();
                        var artist = renderer?["subtitle"]?["runs"]?[0]?["text"]?.ToString();
                        var thumb = renderer?["thumbnailRenderer"]?["musicThumbnailRenderer"]?["thumbnail"]?["thumbnails"]?[0]?["url"]?.ToString();
                        
                        results.Add(new SongModel { VideoId = videoId, Title = title ?? "Neznámé", Artist = artist ?? "Neznámý interpret", ThumbnailUrl = thumb });
                    }
                }
                else if (obj.ContainsKey("musicResponsiveListItemRenderer"))
                {
                    var renderer = obj["musicResponsiveListItemRenderer"];
                    var videoId = renderer?["playlistItemData"]?["videoId"]?.ToString() 
                               ?? renderer?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString()
                               ?? renderer?["overlay"]?["musicItemThumbnailOverlayRenderer"]?["content"]?["musicPlayButtonRenderer"]?["playNavigationEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        var title = renderer?["flexColumns"]?[0]?["musicResponsiveListItemFlexColumnRenderer"]?["text"]?["runs"]?[0]?["text"]?.ToString();
                        var artist = renderer?["flexColumns"]?[1]?["musicResponsiveListItemFlexColumnRenderer"]?["text"]?["runs"]?[0]?["text"]?.ToString() ?? "Neznámý interpret";
                        var thumb = renderer?["thumbnail"]?["musicThumbnailRenderer"]?["thumbnail"]?["thumbnails"]?[0]?["url"]?.ToString();
                        
                        results.Add(new SongModel { VideoId = videoId, Title = title ?? "Neznámé", Artist = artist, ThumbnailUrl = thumb });
                    }
                }
                
                foreach (var prop in obj)
                {
                    ExtractSongsFromNode(prop.Value, results);
                }
            }
            else if (node is System.Text.Json.Nodes.JsonArray arr)
            {
                foreach (var item in arr)
                {
                    ExtractSongsFromNode(item, results);
                }
            }
        }
    }
}