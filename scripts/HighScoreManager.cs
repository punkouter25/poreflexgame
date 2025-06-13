using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text; // For Encoding.UTF8

public class HighScoreData
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
    public DateTime Timestamp { get; set; }
}

public partial class HighScoreManager : Node
{
    [Export] private string PlayerName { get; set; } = "Player";
    [Export] private string ApiBaseUrl { get; set; } = "http://localhost:5000/api/highscores"; // Placeholder API URL

    private HttpRequest _httpRequest;
    private Label _highScoreLabel;
    private const int MaxDisplayedScores = 10;

    public override void _Ready()
    {
        try
        {
            _httpRequest = new HttpRequest();
            AddChild(_httpRequest); // Add HttpRequest as a child of this node

            _highScoreLabel = GetNode<Label>("../UI/HighScoreLabel");

            if (_highScoreLabel == null)
            {
                GD.PrintErr("HighScoreLabel not found!");
                return;
            }

            _httpRequest.RequestCompleted += OnHttpRequestCompleted;

            RefreshHighScores();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error initializing HighScoreManager: {e.Message}");
        }
    }

    public void SubmitScore(int score)
    {
        if (string.IsNullOrEmpty(ApiBaseUrl))
        {
            GD.PrintErr("API Base URL is not set!");
            return;
        }

        var highScore = new HighScoreData { PlayerName = PlayerName, Score = score, Timestamp = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(highScore);
        var headers = new string[] { "Content-Type: application/json" };

        GD.Print($"Submitting score: {json}");
        _httpRequest.Request(ApiBaseUrl, headers, HttpClient.Method.Post, json);
    }

    public void RefreshHighScores()
    {
        if (string.IsNullOrEmpty(ApiBaseUrl))
        {
            GD.PrintErr("API Base URL is not set!");
            return;
        }

        GD.Print("Refreshing high scores...");
        _httpRequest.Request($"{ApiBaseUrl}?top={MaxDisplayedScores}");
    }

    private void OnHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        var responseBody = Encoding.UTF8.GetString(body);
        GD.Print($"HTTP Request Completed. Result: {result}, Response Code: {responseCode}, Body: {responseBody}");

        if (responseCode >= 200 && responseCode < 300)
        {
            // Success
            OnRequestSuccess(responseBody);
        }
        else
        {
            // Failure
            OnRequestFailed((int)responseCode, responseBody);
        }
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

            // Check if the response is a list of high scores (GET request) or a confirmation (POST request)
            // A simple way to differentiate is to check for the "value" property which is typical for OData/JSON arrays
            if (response.Contains("\"value\"")) // Assuming the GET response wraps the array in a "value" property
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
                RefreshHighScores(); // Refresh scores after successful submission
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
        DisplayError($"Failed to connect to server (Status: {statusCode})");
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
