using Godot;
using System;
using System.Text.RegularExpressions;

public partial class MainMenu : Control
{
    private const string GAME_SCENE = "res://scenes/game/game_screen.tscn";
    private const string STATS_SCENE = "res://scenes/stats/statistics_screen.tscn";
    private const string LEADERBOARD_SCENE = "res://scenes/leaderboard/leaderboard_screen.tscn";
    private const string REGISTRATION_SCENE = "res://scenes/registration/player_registration.tscn";

    private GameManager _gameManager;
    private Label _playerInfo;
    private Button _playButton;
    private Button _statsButton;
    private Button _leaderboardButton;
    private LineEdit _initialsInput;

    public override void _Ready()
    {
        GD.Print("Main menu initialized");

        // Get node references
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _playerInfo = GetNode<Label>("PlayerInfo");
        _playButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/PlayButton");
        _statsButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/StatsButton");
        _leaderboardButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/LeaderboardButton");
        _initialsInput = GetNode<LineEdit>("CenterContainer/VBoxContainer/InitialsContainer/InitialsInput");

        // Connect button signals
        _playButton.Pressed += OnPlayPressed;
        _statsButton.Pressed += OnStatsPressed;
        _leaderboardButton.Pressed += OnLeaderboardPressed;

        // Connect initials input signal
        _initialsInput.TextChanged += OnInitialsChanged;
        
        // Clear initials on start
        _initialsInput.Text = "";
        _gameManager.PlayerData.Initials = "";

        // Display player info
        UpdatePlayerInfo();
    }

    private void UpdatePlayerInfo()
    {
        var initials = _gameManager.PlayerData.Initials;
        var bestScore = _gameManager.GetBestScore();

        var infoText = initials;
        if (bestScore < float.PositiveInfinity)
        {
            infoText += $" - Best: {bestScore:F3}";
        }

        _playerInfo.Text = infoText;
        GD.Print($"Updated player info: {infoText}");
    }

    private void OnInitialsChanged(string newText)
    {
        // Convert to uppercase
        string upperText = newText.ToUpper();
        
        // Remove any non-letter characters
        string lettersOnly = Regex.Replace(upperText, "[^A-Z]", "");
        
        // Update the text if it was modified
        if (lettersOnly != newText)
        {
            _initialsInput.Text = lettersOnly;
            _initialsInput.CaretColumn = lettersOnly.Length;
        }
    }

    private void OnPlayPressed()
    {
        // Update player initials before starting the game
        _gameManager.PlayerData.Initials = _initialsInput.Text.ToUpper();
        GD.Print($"Starting new game with initials: {_gameManager.PlayerData.Initials}");
        ChangeScene(GAME_SCENE);
    }

    private void OnStatsPressed()
    {
        GD.Print("Opening statistics");
        ChangeScene(STATS_SCENE);
    }

    private void OnLeaderboardPressed()
    {
        GD.Print("Opening leaderboard");
        ChangeScene(LEADERBOARD_SCENE);
    }

    private void ChangeScene(string scenePath)
    {
        var main = GetTree().GetRoot().GetNode<Main>("Main");
        if (main != null)
        {
            GD.Print($"Changing scene to: {scenePath}");
            main.ChangeScene(scenePath);
        }
        else
        {
            GD.PushError("Could not find Main node");
            // Fallback to direct scene change if Main node is not found
            Error error = GetTree().ChangeSceneToFile(scenePath);
            if (error != Error.Ok)
            {
                GD.PushError($"Failed to change scene. Error code: {error}");
            }
        }
    }
} 