using SCRAP.domain.entities;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCRAP.winforms
{
    public static class ApiConfig
    {
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_settings.json");

        public static string BaseUrl { get; private set; } = "http://localhost:5153/";
        public static string CompanyCode { get; private set; } = "DEMO";

        private static readonly Lazy<HttpClient> _http = new(() =>
        {
            LoadSettings();
            var c = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            if (!string.IsNullOrWhiteSpace(CompanyCode))
            {
                c.DefaultRequestHeaders.Add("X-Company-Code", CompanyCode);
            }
            return c;
        });

        public static HttpClient Http => _http.Value;

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        // Set the current API user header for ApiKeyAuthHandler
        public static void SetApiUser(string? username)
        {
            if (Http.DefaultRequestHeaders.Contains("X-Api-User"))
                Http.DefaultRequestHeaders.Remove("X-Api-User");

            if (!string.IsNullOrWhiteSpace(username))
                Http.DefaultRequestHeaders.Add("X-Api-User", username);
        }

        // Set and persist CompanyCode for Option B multi-tenancy
        public static void SetCompanyCode(string companyCode)
        {
            CompanyCode = (companyCode ?? string.Empty).Trim().ToUpperInvariant();

            if (Http.DefaultRequestHeaders.Contains("X-Company-Code"))
                Http.DefaultRequestHeaders.Remove("X-Company-Code");

            if (!string.IsNullOrWhiteSpace(CompanyCode))
                Http.DefaultRequestHeaders.Add("X-Company-Code", CompanyCode);

            SaveSettings();
        }

        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("BaseUrl", out var b) && !string.IsNullOrWhiteSpace(b.GetString()))
                        BaseUrl = b.GetString()!;
                    if (doc.RootElement.TryGetProperty("CompanyCode", out var c) && !string.IsNullOrWhiteSpace(c.GetString()))
                        CompanyCode = c.GetString()!.ToUpperInvariant();
                }
                else
                {
                    SaveSettings();
                }
            }
            catch { }
        }

        public static void SaveSettings()
        {
            try
            {
                var content = JsonSerializer.Serialize(new
                {
                    BaseUrl,
                    CompanyCode
                }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, content);
            }
            catch { }
        }
    }
}
