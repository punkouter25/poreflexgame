using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Godot.Collections; // For Godot's Dictionary
using Json = Godot.Json; // Alias for Godot's JSON

public partial class AzureTableStorage : Node
{
    private HttpRequest _httpRequest;
    
    // Azure Storage credentials - you should load these from a secure configuration
    [Export] private string StorageAccountName = "poreflexgamestorage";
    [Export] private string StorageAccountKey = ""; // Load this securely, not hardcoded
    [Export] private string TableName = "highscores";

    public override void _Ready()
    {
        _httpRequest = GetNode<HttpRequest>("HTTPRequest");
        _httpRequest.RequestCompleted += OnRequestCompleted;
    }

    // Submit a new high score
    public void SubmitHighScore(string playerName, int score)
    {
        // Create entity for Azure Table
        var entity = new Godot.Collections.Dictionary<string, Variant>
        {
            { "PartitionKey", "Scores" },
            { "RowKey", Guid.NewGuid().ToString() },
            { "PlayerName", playerName },
            { "Score", score },
            { "Timestamp", DateTime.UtcNow.ToString("o") }
        };

        string jsonBody = Json.Stringify(entity);
        string url = $"https://{StorageAccountName}.table.core.windows.net/{TableName}";

        // Headers for insert operation
        var headers = new string[]
        {
            "Content-Type: application/json",
            $"Accept: application/json;odata=nometadata",
            $"x-ms-date: {GetUtcNow()}",
            $"x-ms-version: 2019-07-07",
            $"Authorization: {CreateAuthorizationHeader("POST", jsonBody, "/"+TableName)}"
        };

        Error error = _httpRequest.Request(url, headers, HttpClient.Method.Post, jsonBody);
        if (error != Error.Ok)
        {
            GD.PrintErr($"HTTP Request failed: {error}");
        }
    }

    // Retrieve high scores
    public void GetHighScores(int topN = 10)
    {
        string url = $"https://{StorageAccountName}.table.core.windows.net/{TableName}()?$filter=PartitionKey eq 'Scores'&$top={topN}&$orderby=Score desc";
        
        // Headers for query operation
        var headers = new string[]
        {
            $"Accept: application/json;odata=nometadata",
            $"x-ms-date: {GetUtcNow()}",
            $"x-ms-version: 2019-07-07",
            $"Authorization: {CreateAuthorizationHeader("GET", "", $"/{TableName}()?$filter=PartitionKey eq 'Scores'&$top={topN}&$orderby=Score desc")}"
        };

        Error error = _httpRequest.Request(url, headers, HttpClient.Method.Get);
        if (error != Error.Ok)
        {
            GD.PrintErr($"HTTP Request failed: {error}");
        }
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        if (responseCode >= 200 && responseCode < 300)
        {
            string response = Encoding.UTF8.GetString(body);
            GD.Print("Request successful: " + response);
            EmitSignal(SignalName.RequestSuccess, response);
        }
        else
        {
            string errorResponse = Encoding.UTF8.GetString(body);
            GD.PrintErr($"Request failed with code {responseCode}: {errorResponse}");
            EmitSignal(SignalName.RequestFailed, responseCode, errorResponse);
        }
    }

    // Create authorization signature required by Azure Storage REST API
    private string CreateAuthorizationHeader(string httpVerb, string content, string canonicalizedResource)
    {
        string contentLength = string.IsNullOrEmpty(content) ? "0" : content.Length.ToString();
        string contentType = httpVerb == "GET" ? "" : "application/json";
        string date = GetUtcNow();
        string headers = $"x-ms-date:{date}\nx-ms-version:2019-07-07";

        // String to sign format: VERB\n[Content-MD5]\n[Content-Type]\n[Date]\n[CanonicalizedHeaders]\n[CanonicalizedResource]
        string stringToSign = 
            $"{httpVerb}\n\n{contentType}\n\n{headers}\n{canonicalizedResource}";

        // Create signature
        byte[] keyBytes = Convert.FromBase64String(StorageAccountKey);
        byte[] stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        
        using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
        {
            byte[] signatureBytes = hmac.ComputeHash(stringToSignBytes);
            string signature = Convert.ToBase64String(signatureBytes);
            return $"SharedKey {StorageAccountName}:{signature}";
        }
    }

    private string GetUtcNow()
    {
        return DateTime.UtcNow.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    [Signal]
    public delegate void RequestSuccessEventHandler(string response);

    [Signal]
    public delegate void RequestFailedEventHandler(int statusCode, string error);
}
