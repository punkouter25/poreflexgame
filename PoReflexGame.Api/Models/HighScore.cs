using Azure;
using Azure.Data.Tables;
using System;
using System.Text.Json.Serialization; // Add this for [JsonIgnore]

namespace PoReflexGame.Api.Models
{
    public class HighScore : ITableEntity
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        
        // ITableEntity properties - marked as nullable and ignored for incoming JSON
        [JsonIgnore]
        public string? PartitionKey { get; set; }
        [JsonIgnore]
        public DateTimeOffset? Timestamp { get; set; }
        [JsonIgnore]
        public string? RowKey { get; set; }
        [JsonIgnore]
        public ETag ETag { get; set; }
    }
}
