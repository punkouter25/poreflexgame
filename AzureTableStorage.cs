using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Godot.Collections; // For Godot's Dictionary
using Json = Godot.Json; // Alias for Godot's JSON

// Configuration class for Azure Storage settings
public class AzureStorageConfig
{
    public string StorageAccountName { get; set; }
    public string StorageAccountKey { get; set; }
    public string TableName { get; set; }
    public string ApiVersion { get; set; } = "2019-07-07";
}

public partial class AzureTableStorage : Node
{
    [Signal]
    public delegate void RequestSuccessEventHandler(string response);

    [Signal]
    public delegate void RequestFailedEventHandler(int statusCode, string error);

    [Signal]
    public delegate void TablesCreatedEventHandler();

    private HttpRequest _httpRequest;
    
    // Development settings for Azurite
    private const bool USE_AZURITE = true;
    private const string AZURITE_ENDPOINT = "http://127.0.0.1:10002";
    private const string DEV_ACCOUNT_NAME = "devstoreaccount1";
    private const string DEV_ACCOUNT_KEY = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
    
    // Production settings
    private const string PROD_ENDPOINT = "https://{0}.table.core.windows.net";
    private const string PROD_ACCOUNT_NAME = "poreflexgamestorage";
    private const string PROD_ACCOUNT_KEY = ""; // Load this from secure configuration
    
    // Common settings
    private string _tableName = "highscores";
    private const string API_VERSION = "2019-07-07";

    // Current settings based on environment
    private string StorageEndpoint => USE_AZURITE ? AZURITE_ENDPOINT : string.Format(PROD_ENDPOINT, AccountName);
    private string AccountName => USE_AZURITE ? DEV_ACCOUNT_NAME : PROD_ACCOUNT_NAME;
    private string AccountKey => USE_AZURITE ? DEV_ACCOUNT_KEY : PROD_ACCOUNT_KEY;

    public override void _Ready()
    {
        _httpRequest = GetNode<HttpRequest>("HTTPRequest");
        _httpRequest.RequestCompleted += OnRequestCompleted;
        
        // Print connection info
        GD.Print($"Using {(USE_AZURITE ? "Azurite" : "Production")} storage endpoint: {StorageEndpoint}");

        // Create tables if they don't exist
        CreateTablesIfNotExists();
    }

    public void GetHighScores(int topN = 10)
    {
        string url = USE_AZURITE ? $"{StorageEndpoint}/{AccountName}/{_tableName}()?$filter=PartitionKey eq 'Scores'&$top={topN}&$orderby=Score desc" : $"{StorageEndpoint}/{_tableName}()?$filter=PartitionKey eq 'Scores'&$top={topN}&$orderby=Score desc";
        
        var headers = new string[]
        {
            $"Accept: application/json;odata=nometadata",
            $"x-ms-date: {GetUtcNow()}",
            $"x-ms-version: {API_VERSION}",
            $"Authorization: {CreateAuthorizationHeader("GET", "", $"/{AccountName}/{_tableName}")}"
        };

        GD.Print($"Requesting high scores from: {url}");
        Error error = _httpRequest.Request(url, headers, HttpClient.Method.Get);
        if (error != Error.Ok)
        {
            GD.PrintErr($"HTTP Request failed: {error}");
            EmitSignal(SignalName.RequestFailed, 0, $"HTTP Request failed: {error}");
        }
    }

    public void SubmitHighScore(string playerName, int score)
    {
        var entity = new Godot.Collections.Dictionary<string, Variant>
        {
            { "PartitionKey", "Scores" },
            { "RowKey", Guid.NewGuid().ToString() },
            { "PlayerName", playerName },
            { "Score", score },
            { "Timestamp", DateTime.UtcNow.ToString("o") }
        };

        string jsonBody = Json.Stringify(entity);
        string url = USE_AZURITE ? $"{StorageEndpoint}/{AccountName}/{_tableName}" : $"{StorageEndpoint}/{_tableName}";

        var headers = new string[]
        {
            "Content-Type: application/json",
            $"Accept: application/json;odata=nometadata",
            $"x-ms-date: {GetUtcNow()}",
            $"x-ms-version: {API_VERSION}",
            $"Authorization: {CreateAuthorizationHeader("POST", jsonBody, $"/{AccountName}/{_tableName}")}"
        };

        GD.Print($"Submitting score to: {url}");
        Error error = _httpRequest.Request(url, headers, HttpClient.Method.Post, jsonBody);
        if (error != Error.Ok)
        {
            GD.PrintErr($"HTTP Request failed: {error}");
            EmitSignal(SignalName.RequestFailed, 0, $"HTTP Request failed: {error}");
        }
    }

    private async Task CreateTablesIfNotExists()
    {
        await CreateTableIfNotExists("highscores");
        await CreateTableIfNotExists("stats");
        EmitSignal(SignalName.TablesCreated);
    }

    private async Task CreateTableIfNotExists(string tableName)
    {
        string url = $"{StorageEndpoint}/Tables";
        var body = new Godot.Collections.Dictionary<string, string>
        {
            { "TableName", tableName }
        };
        string jsonBody = Json.Stringify(body);

        var headers = new string[]
        {
            "Content-Type: application/json",
            $"Accept: application/json;odata=nometadata",
            $"x-ms-date: {GetUtcNow()}",
            $"x-ms-version: {API_VERSION}",
            $"Authorization: {CreateAuthorizationHeader("POST", jsonBody, "/Tables")}"
        };

        GD.Print($"Attempting to create table: {tableName}");
        Error error = _httpRequest.Request(url, headers, HttpClient.Method.Post, jsonBody);

        if (error != Error.Ok)
        {
            GD.PrintErr($"HTTP Request failed for table creation {tableName}: {error}");
            // Depending on desired behavior, you might want to emit a signal or handle this error differently
        }
        // Note: Azure Table Storage Create Table operation is idempotent.
        // If the table already exists, it will return 409 Conflict, which is expected and not an error in this context.
        // We handle the response in OnRequestCompleted.
    }


    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        string response = Encoding.UTF8.GetString(body);
        
        if (responseCode >= 200 && responseCode < 300)
        {
            GD.Print($"Request successful: {response}");
            EmitSignal(SignalName.RequestSuccess, response);
        }
        else
        {
            GD.PrintErr($"Request failed with code {responseCode}: {response}");
            EmitSignal(SignalName.RequestFailed, responseCode, response);
        }
    }

    private string CreateAuthorizationHeader(string httpVerb, string content, string canonicalizedResource)
    {
        string contentLength = string.IsNullOrEmpty(content) ? "0" : content.Length.ToString();
        string contentType = httpVerb == "GET" ? "" : "application/json";
        string date = GetUtcNow();
        string headers = $"x-ms-date:{date}\nx-ms-version:{API_VERSION}";

        // String to sign format: VERB\n[Content-MD5]\n[Content-Type]\n[Date]\n[CanonicalizedHeaders]\n[CanonicalizedResource]
        string stringToSign = 
            $"{httpVerb}\n\n{contentType}\n\n{headers}\n{canonicalizedResource}";

        // Create signature
        byte[] keyBytes = Convert.FromBase64String(AccountKey);
        byte[] stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        
        using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
        {
            byte[] signatureBytes = hmac.ComputeHash(stringToSignBytes);
            string signature = Convert.ToBase64String(signatureBytes);
            return $"SharedKey {AccountName}:{signature}";
        }
    }

    private string GetUtcNow()
    {
        return DateTime.UtcNow.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }
}
