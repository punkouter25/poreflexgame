using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class HighScoreManager : Node
{
    [Export] private string PlayerName { get; set; } = "Player";
    
    private AzureTableStorage _azureStorage;
    private Label _highScoreLabel;
    
    // Struct to hold high score data
    public struct HighScore
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
    }
    
    public override void _Ready()
    {
        _azureStorage = GetNode<AzureTableStorage>("../AzureTableStorage");
        _highScoreLabel = GetNode<Label>("../UI/HighScoreLabel");
        
        // Connect to the signals
        _azureStorage.RequestSuccess += OnRequestSuccess;
        _azureStorage.RequestFailed += OnRequestFailed;
        
        // Load high scores when the game starts
        RefreshHighScores();
    }
    
    public void SubmitScore(int score)
    {
        _azureStorage.SubmitHighScore(PlayerName, score);
    }
    
    public void RefreshHighScores()
    {
        _azureStorage.GetHighScores(10); // Get top 10 high scores
    }
    
    private void OnRequestSuccess(string response)
    {
        // Parse the JSON response from Azure
        if (response.Contains("value"))
        {
            // This is a query response with multiple records
            try
            {
                // Parse the JSON array
                var jsonDoc = JsonDocument.Parse(response);
                var scoreList = new List<HighScore>();
                
                // Extract each high score entry
                foreach (var element in jsonDoc.RootElement.GetProperty("value").EnumerateArray())
                {
                    var highScore = new HighScore
                    {
                        PlayerName = element.GetProperty("PlayerName").GetString(),
                        Score = element.GetProperty("Score").GetInt32()
                    };
                    scoreList.Add(highScore);
                }
                
                DisplayHighScores(scoreList);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to parse high scores: {e.Message}");
            }
        }
        else
        {
            // This is probably a submission confirmation
            GD.Print("Score submitted successfully!");
            
            // Refresh the high scores after submitting
            RefreshHighScores();
        }
    }
    
    private void OnRequestFailed(int statusCode, string error)
    {
        GD.PrintErr($"Request failed with status {statusCode}: {error}");
    }
    
    private void DisplayHighScores(List<HighScore> scores)
    {
        string scoreText = "HIGH SCORES\n\n";
        
        for (int i = 0; i < scores.Count; i++)
        {
            scoreText += $"{i+1}. {scores[i].PlayerName}: {scores[i].Score}\n";
        }
        
        _highScoreLabel.Text = scoreText;
    }
}
