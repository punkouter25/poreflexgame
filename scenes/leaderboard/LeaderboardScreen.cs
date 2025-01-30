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

    public override void _Ready()
    {
        GD.Print("Leaderboard screen initialized");

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
        var main = GetTree().GetRoot().GetNode<Main>("Main");
        if (main != null)
        {
            main.ChangeScene(MAIN_MENU_SCENE);
        }
        else
        {
            GD.PushError("Could not find Main node");
            // Fallback to direct scene change if Main node is not found
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

        // TODO: Implement leaderboard data loading
        // This will be implemented when we add the leaderboard functionality
    }
} 