using Godot;
using System;
using System.Linq;

public partial class GameOverScreen : Control
{
    private const string GAME_SCENE = "res://scenes/game/game_screen.tscn";
    private const string MAIN_MENU_SCENE = "res://scenes/menu/main_menu.tscn";

    private GameManager _gameManager;
    private Label _averageScore;
    private Label _highScoreLabel;
    private Control _timesGrid;
    private Button _tryAgainButton;
    private Button _mainMenuButton;

    public override void _Ready()
    {
        GD.Print("Game over screen initialized");

        // Get node references
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _averageScore = GetNode<Label>("CenterContainer/VBoxContainer/ScoreContainer/AverageScore");
        _highScoreLabel = GetNode<Label>("CenterContainer/VBoxContainer/ScoreContainer/HighScoreLabel");
        _timesGrid = GetNode<Control>("CenterContainer/VBoxContainer/TimesGrid");
        _tryAgainButton = GetNode<Button>("CenterContainer/VBoxContainer/ButtonsContainer/TryAgainButton");
        _mainMenuButton = GetNode<Button>("CenterContainer/VBoxContainer/ButtonsContainer/MainMenuButton");

        // Connect signals
        _tryAgainButton.Pressed += OnTryAgainPressed;
        _mainMenuButton.Pressed += OnMainMenuPressed;

        DisplayResults();
    }

    private void DisplayResults()
    {
        var latestRecord = _gameManager.PlayerData.GameRecords[0];
        var times = latestRecord.IndividualTimes;
        var average = latestRecord.AverageTime;

        // Display average score
        _averageScore.Text = $"Average: {average:F3}s";

        // Check if it's a high score
        var bestScore = _gameManager.GetBestScore();
        if (average <= bestScore)
        {
            _highScoreLabel.Text = "New High Score!";
            GD.Print($"New high score achieved: {average}");
        }

        // Display individual times
        for (int i = 0; i < times.Length; i++)
        {
            var timeLabel = _timesGrid.GetNode<Label>($"Bar{i + 1}Time");
            if (timeLabel != null)
            {
                timeLabel.Text = $"{times[i]:F3}s";

                // Highlight best and worst times
                if (times[i] == times.Where(t => t < 99999.0f).Min())
                {
                    timeLabel.Modulate = Colors.Green;
                }
                else if (times[i] == times.Max())
                {
                    timeLabel.Modulate = Colors.Red;
                }
            }
        }
    }

    private void OnTryAgainPressed()
    {
        GD.Print("Starting new game");
        ChangeScene(GAME_SCENE);
    }

    private void OnMainMenuPressed()
    {
        GD.Print("Returning to main menu");
        ChangeScene(MAIN_MENU_SCENE);
    }

    private void ChangeScene(string scenePath)
    {
        var main = GetTree().GetRoot().GetNodeOrNull<Main>("Main");
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