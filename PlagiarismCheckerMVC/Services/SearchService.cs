using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using PlagiarismCheckerMVC.Models;
using AngleSharp.Html.Parser;

namespace PlagiarismCheckerMVC.Services
{
    public class SearchService : ISearchService
    {
        private readonly HttpClient _httpClient;
        private readonly SearchEngineSettings _searchEngineSettings;

        public SearchService(HttpClient httpClient, IOptions<SearchEngineSettings> searchEngineSettings)
        {
            _httpClient = httpClient;
            _searchEngineSettings = searchEngineSettings.Value;
        }

        private bool IsLimitExceeded(HttpResponseMessage response)
        {
            return response.StatusCode == System.Net.HttpStatusCode.Forbidden || (int)response.StatusCode == 429;
        }

        public async Task<List<SearchItem>> SearchGoogleAsync(string query)
        {
            string apiKey = _searchEngineSettings.Google.ApiKey;
            string searchEngineId = _searchEngineSettings.Google.SearchEngineId;

            string url = $"https://www.googleapis.com/customsearch/v1?key={apiKey}&cx={searchEngineId}&q={Uri.EscapeDataString(query)}&num={_searchEngineSettings.Google.ResultsCount}";

            var response = await _httpClient.GetAsync(url);
            if (IsLimitExceeded(response))
                return new List<SearchItem>();
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<GoogleSearchResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var results = new List<SearchItem>();

            if (searchResult != null && searchResult.Items != null)
            {
                foreach (var item in searchResult.Items)
                {
                    results.Add(new SearchItem
                    {
                        Title = item.Title,
                        Link = item.Link,
                        Snippet = item.Snippet
                    });
                }
            }

            return results;
        }

        public async Task<List<SearchItem>> SearchYandexAsync(string query)
        {
            string apiKey = _searchEngineSettings.Yandex.ApiKey;
            string searchEngineId = _searchEngineSettings.Yandex.SearchEngineId;

            string url = $"https://search-maps.yandex.ru/v1/search?text={HttpUtility.UrlEncode(query)}&lang=ru_RU&apikey={apiKey}&results={_searchEngineSettings.Yandex.ResultsCount}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (IsLimitExceeded(response))
                    return new List<SearchItem>();
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<YandexSearchResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var results = new List<SearchItem>();

                if (searchResult?.Features == null)
                    return results;

                foreach (var feature in searchResult.Features)
                {
                    if (feature.Properties?.CompanyMetaData == null)
                        continue;

                    results.Add(new SearchItem
                    {
                        Title = feature.Properties.CompanyMetaData.Name ?? string.Empty,
                        Link = feature.Properties.CompanyMetaData.Url ?? string.Empty,
                        Snippet = feature.Properties.Description ?? string.Empty
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске в Яндексе: {ex.Message}");
                return new List<SearchItem>();
            }
        }

        public async Task<List<SearchItem>> SearchSerpApiAsync(string query)
        {
            var apiKey = "9582a6313569583ac20c010a6a3f8d25f3f1c7d9";
            var url = "https://google.serper.dev/search";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-API-KEY", apiKey);
            var requestContent = new StringContent("{\"q\": \"" + query + "\"}", null, "application/json");
            request.Content = requestContent;
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var results = new List<SearchItem>();
            if (doc.RootElement.TryGetProperty("organic", out var organicResults))
            {
                foreach (var item in organicResults.EnumerateArray().Take(6))
                {
                    var title = item.GetProperty("title").GetString() ?? string.Empty;
                    var link = item.GetProperty("link").GetString() ?? string.Empty;
                    var snippet = item.TryGetProperty("snippet", out var snip) ? snip.GetString() ?? string.Empty : string.Empty;
                    results.Add(new SearchItem { Title = title, Link = link, Snippet = snippet });
                }
            }
            return results;
        }
    }

    // Классы для десериализации ответа от Google API
    public class GoogleSearchResponse
    {
        public List<GoogleSearchItem> Items { get; set; }
    }

    public class GoogleSearchItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Snippet { get; set; }
    }

    // Классы для десериализации ответа от Yandex API
    public class YandexSearchResponse
    {
        public List<YandexFeature> Features { get; set; }
    }

    public class YandexFeature
    {
        public YandexProperties Properties { get; set; }
    }

    public class YandexProperties
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public YandexCompanyMetaData CompanyMetaData { get; set; }
    }

    public class YandexCompanyMetaData
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}