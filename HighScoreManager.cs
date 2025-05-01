using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

// Separate class for high score data
public class HighScoreData
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
    public DateTime Timestamp { get; set; }
}

public partial class HighScoreManager : Node
{
    [Export] private string PlayerName { get; set; } = "Player";
    
    private AzureTableStorage _azureStorage;
    private Label _highScoreLabel;
    private const int MaxDisplayedScores = 10;
    
    public override void _Ready()
    {
        try
        {
            _azureStorage = GetNode<AzureTableStorage>("../AzureTableStorage");
            _highScoreLabel = GetNode<Label>("../UI/HighScoreLabel");
            
            if (_azureStorage == null)
            {
                GD.PrintErr("AzureTableStorage node not found!");
                return;
            }
            
            if (_highScoreLabel == null)
            {
                GD.PrintErr("HighScoreLabel not found!");
                return;
            }
            
            // Connect to the signals
            _azureStorage.RequestSuccess += OnRequestSuccess;
            _azureStorage.RequestFailed += OnRequestFailed;
            
            // Load high scores when the game starts
            RefreshHighScores();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error initializing HighScoreManager: {e.Message}");
        }
    }
    
    public void SubmitScore(int score)
    {
        if (_azureStorage == null)
        {
            GD.PrintErr("Cannot submit score: AzureTableStorage not initialized");
            return;
        }
        
        _azureStorage.SubmitHighScore(PlayerName, score);
    }
    
    public void RefreshHighScores()
    {
        if (_azureStorage == null)
        {
            GD.PrintErr("Cannot refresh scores: AzureTableStorage not initialized");
            return;
        }
        
        _azureStorage.GetHighScores(MaxDisplayedScores);
    }
    
    private void OnRequestSuccess(string response)
    {
        try
        {
            if (string.IsNullOrEmpty(response))
            {
                GD.PrintErr("Received empty response from server");
                return;
            }

            if (response.Contains("value"))
            {
                var jsonDoc = JsonDocument.Parse(response);
                var scoreList = new List<HighScoreData>();
                
                foreach (var element in jsonDoc.RootElement.GetProperty("value").EnumerateArray())
                {
                    try
                    {
                        var highScore = new HighScoreData
                        {
                            PlayerName = element.GetProperty("PlayerName").GetString() ?? "Unknown",
                            Score = element.GetProperty("Score").GetInt32(),
                            Timestamp = DateTime.Parse(element.GetProperty("Timestamp").GetString() ?? DateTime.UtcNow.ToString("o"))
                        };
                        scoreList.Add(highScore);
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr($"Error parsing high score entry: {e.Message}");
                    }
                }
                
                DisplayHighScores(scoreList);
            }
            else
            {
                GD.Print("Score submitted successfully!");
                RefreshHighScores();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error processing high scores: {e.Message}");
            DisplayError("Failed to load high scores");
        }
    }
    
    private void OnRequestFailed(int statusCode, string error)
    {
        GD.PrintErr($"Request failed with status {statusCode}: {error}");
        DisplayError("Failed to connect to server");
    }
    
    private void DisplayHighScores(List<HighScoreData> scores)
    {
        if (_highScoreLabel == null)
        {
            GD.PrintErr("Cannot display scores: HighScoreLabel not initialized");
            return;
        }

        if (scores.Count == 0)
        {
            _highScoreLabel.Text = "No high scores available";
            return;
        }

        var scoreText = new System.Text.StringBuilder("HIGH SCORES\n\n");
        
        for (int i = 0; i < scores.Count; i++)
        {
            scoreText.AppendLine($"{i+1}. {scores[i].PlayerName}: {scores[i].Score}");
        }
        
        _highScoreLabel.Text = scoreText.ToString();
    }
    
    private void DisplayError(string message)
    {
        if (_highScoreLabel != null)
        {
            _highScoreLabel.Text = message;
        }
    }
}
