using Godot;
using System;

public partial class LeaderboardScreen : Control
{
    private const string MAIN_MENU_SCENE = "res://scenes/menu/main_menu.tscn";

    private GameManager _gameManager;
    private Button _backButton;
    private Label _playerRankLabel;
    private VBoxContainer _scoresList;
    private Label _loadingLabel;
    private Label _errorLabel;
    private Main _mainNode;

    public override void _Ready()
    {
        GD.Print("Leaderboard screen initialized");

        // Get the Main node from the root
        var root = GetTree().Root;
        foreach (var child in root.GetChildren())
        {
            if (child is Main mainNode)
            {
                _mainNode = mainNode;
                GD.Print("Found Main node in root children");
                break;
            }
        }
        
        if (_mainNode == null)
        {
            GD.PushWarning("Could not find Main node during initialization");
            PrintSceneTree(GetTree().Root, 0);  // Print scene tree for debugging
        }

        // Get node references
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/Header/BackButton");
        _playerRankLabel = GetNode<Label>("MarginContainer/VBoxContainer/PlayerRank");
        _scoresList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScoresList");
        _loadingLabel = GetNode<Label>("MarginContainer/VBoxContainer/LoadingLabel");
        _errorLabel = GetNode<Label>("MarginContainer/VBoxContainer/ErrorLabel");

        // Connect signals
        _backButton.Pressed += OnBackPressed;

        // Load leaderboard data
        LoadLeaderboard();
    }

    private void OnBackPressed()
    {
        GD.Print("Returning to main menu");
        
        // Try to find Main node again if we don't have it
        if (_mainNode == null)
        {
            var root = GetTree().Root;
            foreach (var child in root.GetChildren())
            {
                if (child is Main mainNode)
                {
                    _mainNode = mainNode;
                    GD.Print("Found Main node before scene change");
                    break;
                }
            }
        }

        if (_mainNode != null)
        {
            GD.Print($"Changing scene to: {MAIN_MENU_SCENE} through Main node");
            _mainNode.ChangeScene(MAIN_MENU_SCENE);
        }
        else
        {
            GD.PushError("Could not find Main node, falling back to direct scene change");
            GD.Print("Current scene tree structure:");
            PrintSceneTree(GetTree().Root, 0);
            Error error = GetTree().ChangeSceneToFile(MAIN_MENU_SCENE);
            if (error != Error.Ok)
            {
                GD.PushError($"Failed to change scene. Error code: {error}");
            }
        }
    }

    private void LoadLeaderboard()
    {
        // Clear existing scores
        foreach (Node child in _scoresList.GetChildren())
        {
            child.QueueFree();
        }

        _loadingLabel.Show();
        _errorLabel.Hide();

        // Try to get LeaderboardManager and load scores
        try
        {
            var leaderboardManager = GetNode<LeaderboardManager>("/root/LeaderboardManager");
            if (leaderboardManager != null)
            {
                leaderboardManager.HighScoresLoaded += OnHighScoresLoaded;
                leaderboardManager.LoadHighScores(10);
                GD.Print("Loading high scores from LeaderboardManager");
            }
            else
            {
                GD.PrintErr("LeaderboardManager not found");
                ShowError("Leaderboard service not available");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load leaderboard: {e.Message}");
            ShowError("Failed to load leaderboard");
        }
    }

    private void OnHighScoresLoaded(Godot.Collections.Array<Godot.Collections.Dictionary> scores)
    {
        _loadingLabel.Hide();
        
        if (scores.Count == 0)
        {
            ShowError("No high scores available");
            return;
        }

        // Display the scores
        for (int i = 0; i < scores.Count; i++)
        {
            var scoreData = scores[i];
            var playerName = scoreData["PlayerName"].AsString();
            var score = scoreData["Score"].AsSingle();
            var timestamp = scoreData["Timestamp"].AsString();

            // Create a label for each score
            var scoreLabel = new Label();
            scoreLabel.Text = $"{i + 1}. {playerName}: {score:F3}s";
            scoreLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            
            _scoresList.AddChild(scoreLabel);
        }

        // Update player rank if we have player data
        UpdatePlayerRank(scores);
    }

    private void UpdatePlayerRank(Godot.Collections.Array<Godot.Collections.Dictionary> scores)
    {
        try
        {
            var gameManager = GetNode<GameManager>("/root/GameManager");
            if (gameManager?.PlayerData != null)
            {
                string playerInitials = gameManager.PlayerData.Initials;
                if (!string.IsNullOrEmpty(playerInitials))
                {
                    for (int i = 0; i < scores.Count; i++)
                    {
                        var scoreData = scores[i];
                        if (scoreData["PlayerName"].AsString() == playerInitials)
                        {
                            _playerRankLabel.Text = $"Your Rank: #{i + 1}";
                            return;
                        }
                    }
                }
            }
            _playerRankLabel.Text = "Your Rank: Not ranked";
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to update player rank: {e.Message}");
            _playerRankLabel.Text = "Your Rank: Unknown";
        }
    }

    private void ShowError(string message)
    {
        _loadingLabel.Hide();
        _errorLabel.Text = message;
        _errorLabel.Show();
    }

    private void PrintSceneTree(Node node, int level)
    {
        var indent = new string(' ', level * 2);
        GD.Print($"{indent}{node.Name} ({node.GetType()})");
        foreach (Node child in node.GetChildren())
        {
            PrintSceneTree(child, level + 1);
        }
    }
}
