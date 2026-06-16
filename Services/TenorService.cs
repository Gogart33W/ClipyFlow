using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClipyFlow.Services
{
    public class GifItem
    {
        public string Url { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
    }

    public class TenorService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public TenorService()
        {
        }

        public async Task<List<GifItem>> GetFeaturedGifsAsync(string apiKey)
        {
            return await FetchGifsAsync($"https://g.tenor.com/v1/trending?key={apiKey}&limit=20");
        }

        public async Task<List<GifItem>> SearchGifsAsync(string apiKey, string query)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            return await FetchGifsAsync($"https://g.tenor.com/v1/search?q={encodedQuery}&key={apiKey}&limit=20");
        }

        private async Task<List<GifItem>> FetchGifsAsync(string url)
        {
            var result = new List<GifItem>();
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return result;

                var jsonString = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;
                
                if (root.TryGetProperty("results", out var resultsArray))
                {
                    foreach (var gifObject in resultsArray.EnumerateArray())
                    {
                        if (gifObject.TryGetProperty("media", out var mediaArray) && mediaArray.GetArrayLength() > 0)
                        {
                            var formats = mediaArray[0];
                            string gifUrl = "";
                            string previewUrl = "";
                            
                            if (formats.TryGetProperty("gif", out var gifFmt))
                                gifUrl = gifFmt.GetProperty("url").GetString() ?? "";
                                
                            if (formats.TryGetProperty("tinygif", out var tinyFmt))
                                previewUrl = tinyFmt.GetProperty("url").GetString() ?? "";
                                
                            if (string.IsNullOrEmpty(gifUrl)) gifUrl = previewUrl;
                            
                            if (!string.IsNullOrEmpty(gifUrl))
                            {
                                result.Add(new GifItem { Url = gifUrl, PreviewUrl = previewUrl });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tenor API Error: {ex.Message}");
            }
            return result;
        }
    }
}
