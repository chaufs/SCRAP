using SCRAP.domain.entities;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCRAP.winforms
{
    public static class ApiConfig
    {
        // Change this to match your running API
        public static readonly string BaseUrl = "http://localhost:5153/";

        private static readonly Lazy<HttpClient> _http = new(() =>
        {
            var c = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            return c;
        });

        public static HttpClient Http => _http.Value;
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        // Set the current API user header for the simple ApiKeyAuthHandler
        public static void SetApiUser(string? username)
        {
            // remove existing header if present
            if (Http.DefaultRequestHeaders.Contains("X-Api-User"))
                Http.DefaultRequestHeaders.Remove("X-Api-User");

            if (!string.IsNullOrWhiteSpace(username))
                Http.DefaultRequestHeaders.Add("X-Api-User", username);
        }


    }


}
