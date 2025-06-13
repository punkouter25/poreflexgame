using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;

public partial class LeaderboardManager : Node
{
    [Signal]
    public delegate void HighScoresLoadedEventHandler(Godot.Collections.Array<Godot.Collections.Dictionary> scores);
    
    [Signal]
    public delegate void ScoreSubmittedEventHandler(bool success, string message);

    private const string TableName = "highscores";
    private TableServiceClient _tableServiceClient;
    private TableClient _tableClient;

    public override void _Ready()
    {
        try
        {
            // Initialize the TableServiceClient with Azurite connection string
            string connectionString = "UseDevelopmentStorage=true"; // Azurite connection string
            _tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = _tableServiceClient.GetTableClient(TableName);
            
            // Create the table if it doesn't exist
            CreateTableAsync();
            
            GD.Print("LeaderboardManager initialized with Azurite");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to initialize LeaderboardManager: {e.Message}");
        }
    }

    private async void CreateTableAsync()
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            GD.Print($"Table '{TableName}' ready");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to create table: {e.Message}");
        }
    }

    public async void SubmitHighScore(string playerName, float score)
    {
        try
        {
            var highScoreEntity = new TableEntity("Scores", Guid.NewGuid().ToString())
            {
                { "PlayerName", playerName },
                { "Score", score },
                { "Timestamp", DateTime.UtcNow }
            };

            await _tableClient.AddEntityAsync(highScoreEntity);
            GD.Print($"Inserted high score: {playerName} - {score:F3}s");
            EmitSignal(SignalName.ScoreSubmitted, true, "Score submitted successfully!");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to submit score: {e.Message}");
            EmitSignal(SignalName.ScoreSubmitted, false, $"Failed to submit score: {e.Message}");
        }
    }

    public async void LoadHighScores(int maxScores = 10)
    {
        try
        {
            var highScores = new List<HighScoreEntry>();
            
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>(filter: "PartitionKey eq 'Scores'"))
            {
                var entry = new HighScoreEntry
                {
                    PlayerName = entity.GetString("PlayerName") ?? "Unknown",
                    Score = (float)entity.GetDouble("Score"),
                    Timestamp = entity.GetDateTime("Timestamp") ?? DateTime.UtcNow
                };
                highScores.Add(entry);
            }

            // Sort by score (ascending for best times)
            highScores.Sort((x, y) => x.Score.CompareTo(y.Score));
            
            // Take only the requested number of scores
            var topScores = highScores.Take(maxScores).ToArray();
            
            // Convert to Godot-compatible format
            var godotScores = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var score in topScores)
            {
                var scoreDict = new Godot.Collections.Dictionary
                {
                    { "PlayerName", score.PlayerName },
                    { "Score", score.Score },
                    { "Timestamp", score.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") }
                };
                godotScores.Add(scoreDict);
            }
            
            GD.Print($"Loaded {topScores.Length} high scores");
            EmitSignal(SignalName.HighScoresLoaded, godotScores);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load high scores: {e.Message}");
            EmitSignal(SignalName.HighScoresLoaded, new Godot.Collections.Array<Godot.Collections.Dictionary>());
        }
    }
}

public class HighScoreEntry
{
    public string PlayerName { get; set; } = "";
    public float Score { get; set; }
    public DateTime Timestamp { get; set; }
}
