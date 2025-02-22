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
	private Main _mainNode;

	public override void _Ready()
	{
		GD.Print("Game over screen initialized");
		
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

		// Get other node references
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
			GD.Print($"Changing scene to: {scenePath} through Main node");
			_mainNode.ChangeScene(scenePath);
		}
		else
		{
			GD.PushError("Could not find Main node, falling back to direct scene change");
			GD.Print("Current scene tree structure:");
			PrintSceneTree(GetTree().Root, 0);
			Error error = GetTree().ChangeSceneToFile(scenePath);
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
