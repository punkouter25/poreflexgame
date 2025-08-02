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
    private Main _mainNode;

    public override void _Ready()
    {
        GD.Print("Statistics screen initialized");

        // Get the Main node
        _mainNode = GetNode<Main>("/root/Main");
        if (_mainNode != null)
        {
            GD.Print("Found Main node successfully");
        }
        else
        {
            GD.PushWarning("Could not find Main node during initialization");
            PrintSceneTree(GetTree().Root, 0);  // Print scene tree for debugging
        }

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