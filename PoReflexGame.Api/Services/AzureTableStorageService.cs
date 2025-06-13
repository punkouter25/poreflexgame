using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables; // Use Azure.Data.Tables for modern .NET

namespace PoReflexGame.Api.Services
{
    // Configuration class for Azure Storage settings
    public class AzureStorageConfig
    {
        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string TableName { get; set; }
        public string ApiVersion { get; set; } = "2019-07-07";
        public string ConnectionString { get; set; }
    }

    public class AzureTableStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureTableStorageService> _logger;
        private readonly AzureStorageConfig _config;

        private static (string accountName, string accountKey, string endpoint) ParseConnectionString(string connectionString)
        {
            if (connectionString == "UseDevelopmentStorage=true")
            {
                return ("devstoreaccount1", 
                       "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==", 
                       "http://127.0.0.1:10002");
            }
            
            var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    parts[keyValue[0]] = keyValue[1];
                }
            }

            parts.TryGetValue("AccountName", out string accountName);
            parts.TryGetValue("AccountKey", out string accountKey);
            parts.TryGetValue("TableEndpoint", out string tableEndpoint);
            parts.TryGetValue("DefaultEndpointsProtocol", out string defaultProtocol);

            string endpoint = "";
            if (!string.IsNullOrEmpty(tableEndpoint))
            {
                endpoint = tableEndpoint.TrimEnd('/');
            }
            else if (!string.IsNullOrEmpty(defaultProtocol) && !string.IsNullOrEmpty(accountName))
            {
                endpoint = $"{defaultProtocol}://{accountName}.table.core.windows.net";
            }
            
            return (accountName, accountKey, endpoint);
        }

        private (string accountName, string accountKey, string endpoint) ConnectionDetails { get; }

        private string StorageEndpoint => ConnectionDetails.endpoint;
        private string AccountName => ConnectionDetails.accountName;
        private string AccountKey => ConnectionDetails.accountKey;
        private string TableName => _config.TableName;
        private string ApiVersion => _config.ApiVersion;

        public AzureTableStorageService(HttpClient httpClient, ILogger<AzureTableStorageService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = new AzureStorageConfig();
            configuration.GetSection("AzureStorage").Bind(_config);

            // Determine connection string to use
            string connectionStringToUse = string.IsNullOrEmpty(_config.ConnectionString) ? 
                                           "UseDevelopmentStorage=true" : 
                                           _config.ConnectionString;

            ConnectionDetails = ParseConnectionString(connectionStringToUse);

            if (string.IsNullOrEmpty(ConnectionDetails.accountName) || string.IsNullOrEmpty(ConnectionDetails.accountKey))
            {
                _logger.LogError("Storage account configuration is missing. Please configure connection string.");
                throw new InvalidOperationException("Storage account configuration is missing.");
            }
            
            _logger.LogInformation($"Loaded Azure Storage configuration - Account: {ConnectionDetails.accountName}");
            _logger.LogInformation($"Using {(connectionStringToUse == "UseDevelopmentStorage=true" ? "Azurite" : "Production")} storage endpoint: {StorageEndpoint}");

            // Ensure tables exist on startup
            Task.Run(async () => await CreateTablesIfNotExists());
        }

        public async Task<List<T>> GetEntitiesAsync<T>(string tableName, string filter = "", int topN = 10, string orderBy = "") where T : ITableEntity, new()
        {
            string url = $"{StorageEndpoint}/{tableName}()";
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={filter}");
            }
            if (topN > 0)
            {
                queryParams.Add($"$top={topN}");
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                queryParams.Add($"$orderby={orderBy}");
            }

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("x-ms-date", GetUtcNow());
            request.Headers.Add("x-ms-version", ApiVersion);
            request.Headers.Authorization = new AuthenticationHeaderValue("SharedKey", $"{AccountName}:{CreateAuthorizationSignature("GET", "", $"/{AccountName}/{tableName}")}");

            _logger.LogInformation($"Requesting entities from: {url}");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Request successful: {jsonResponse}");
                
                var jsonDoc = JsonDocument.Parse(jsonResponse);
                var entities = new List<T>();
                
                if (jsonDoc.RootElement.TryGetProperty("value", out JsonElement valueElement))
                {
                    foreach (var element in valueElement.EnumerateArray())
                    {
                        // Deserialize using System.Text.Json
                        var entity = JsonSerializer.Deserialize<T>(element.GetRawText());
                        entities.Add(entity);
                    }
                }
                return entities;
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Request failed with status {response.StatusCode}: {errorResponse}");
                throw new HttpRequestException($"Request failed: {response.StatusCode} - {errorResponse}");
            }
        }

        public async Task AddEntityAsync<T>(string tableName, T entity) where T : ITableEntity
        {
            string url = $"{StorageEndpoint}/{tableName}";
            string jsonBody = JsonSerializer.Serialize(entity);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-ms-date", GetUtcNow());
            request.Headers.Add("x-ms-version", ApiVersion);
            request.Headers.Authorization = new AuthenticationHeaderValue("SharedKey", $"{AccountName}:{CreateAuthorizationSignature("POST", jsonBody, $"/{AccountName}/{tableName}")}");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(jsonBody).Length; // Add Content-Length header

            _logger.LogInformation($"Adding entity to: {url}");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Request failed with status {response.StatusCode}: {errorResponse}");
                throw new HttpRequestException($"Request failed: {response.StatusCode} - {errorResponse}");
            }
            _logger.LogInformation($"Entity added successfully to {tableName}.");
        }

        private async Task CreateTablesIfNotExists()
        {
            await CreateTableIfNotExists("highscores");
            await CreateTableIfNotExists("stats");
            _logger.LogInformation("Tables creation process completed.");
        }

        private async Task CreateTableIfNotExists(string tableName)
        {
            string url = $"{StorageEndpoint}/Tables";
            var body = new Dictionary<string, string>
            {
                { "TableName", tableName }
            };
            string jsonBody = JsonSerializer.Serialize(body);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-ms-date", GetUtcNow());
            request.Headers.Add("x-ms-version", ApiVersion);
            request.Headers.Authorization = new AuthenticationHeaderValue("SharedKey", $"{AccountName}:{CreateAuthorizationSignature("POST", jsonBody, $"/{AccountName}/Tables")}"); // Corrected CanonicalizedResource
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            _logger.LogInformation($"Attempting to create table: {tableName}");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation($"Table '{tableName}' created or already exists.");
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to create table '{tableName}' with status {response.StatusCode}: {errorResponse}");
                throw new HttpRequestException($"Failed to create table '{tableName}': {response.StatusCode} - {errorResponse}");
            }
        }

        private string CreateAuthorizationSignature(string httpVerb, string content, string canonicalizedResource)
        {
            string contentType = httpVerb == "GET" ? "" : "application/json";
            string date = GetUtcNow();
            string headers = $"x-ms-date:{date}\nx-ms-version:{ApiVersion}";

            // String to sign format: VERB\n[Content-MD5]\n[Content-Type]\nDate\nCanonicalizedHeaders\nCanonicalizedResource
            // For Table Storage, Content-MD5 is typically empty.
            string stringToSign = 
                $"{httpVerb}\n\n{contentType}\n{date}\n{headers}\n{canonicalizedResource}";

            // Create signature
            byte[] keyBytes = Convert.FromBase64String(AccountKey);
            byte[] stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
            
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] signatureBytes = hmac.ComputeHash(stringToSignBytes);
                string signature = Convert.ToBase64String(signatureBytes);
                return signature;
            }
        }

        private string GetUtcNow()
        {
            return DateTime.UtcNow.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
