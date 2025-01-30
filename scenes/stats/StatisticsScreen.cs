using Godot;
using System;
using System.Linq;

public partial class StatisticsScreen : Control
{
    private const string MAIN_MENU_SCENE = "res://scenes/menu/main_menu.tscn";

    private GameManager _gameManager;
    private Label _bestTimeLabel;
    private Label _averageTimeLabel;
    private Label _totalGamesLabel;
    private Graph _graph;
    private Button _backButton;

    public override void _Ready()
    {
        GD.Print("Statistics screen initialized");

        // Get node references
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _bestTimeLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatsContainer/BestTime");
        _averageTimeLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatsContainer/AverageTime");
        _totalGamesLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatsContainer/TotalGames");
        _graph = GetNode<Graph>("MarginContainer/VBoxContainer/Graph");
        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/Header/BackButton");

        // Connect signals
        _backButton.Pressed += OnBackPressed;

        // Display statistics
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var records = _gameManager.PlayerData.GameRecords;
        if (records.Count > 0)
        {
            // Calculate statistics
            var validTimes = records.SelectMany(r => r.IndividualTimes.Where(t => t < 99999.0f));
            if (validTimes.Any())
            {
                float bestTime = validTimes.Min();
                float averageTime = validTimes.Average();

                _bestTimeLabel.Text = $"Best Time: {bestTime:F3}s";
                _averageTimeLabel.Text = $"Average Time: {averageTime:F3}s";
            }
            else
            {
                _bestTimeLabel.Text = "Best Time: N/A";
                _averageTimeLabel.Text = "Average Time: N/A";
            }

            _totalGamesLabel.Text = $"Total Games: {records.Count}";

            // Update graph with average times from each game
            var averageTimes = records.Select(r => r.AverageTime).ToArray();
            _graph.SetData(averageTimes);
        }
        else
        {
            _bestTimeLabel.Text = "Best Time: N/A";
            _averageTimeLabel.Text = "Average Time: N/A";
            _totalGamesLabel.Text = "Total Games: 0";
        }
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
} 