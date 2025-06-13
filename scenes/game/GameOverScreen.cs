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
private LeaderboardManager _leaderboardManager;

private const string AVERAGE_SCORE_PATH = "CenterContainer/VBoxContainer/ScoreContainer/AverageScore";
private const string HIGH_SCORE_LABEL_PATH = "CenterContainer/VBoxContainer/ScoreContainer/HighScoreLabel";
private const string TIMES_GRID_PATH = "CenterContainer/VBoxContainer/TimesGrid";
private const string TRY_AGAIN_BUTTON_PATH = "CenterContainer/VBoxContainer/ButtonsContainer/TryAgainButton";
private const string MAIN_MENU_BUTTON_PATH = "CenterContainer/VBoxContainer/ButtonsContainer/MainMenuButton";

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
_averageScore = GetNode<Label>(AVERAGE_SCORE_PATH);
_highScoreLabel = GetNode<Label>(HIGH_SCORE_LABEL_PATH);
_timesGrid = GetNode<Control>(TIMES_GRID_PATH);
_tryAgainButton = GetNode<Button>(TRY_AGAIN_BUTTON_PATH);
_mainMenuButton = GetNode<Button>(MAIN_MENU_BUTTON_PATH);

		// Try to get LeaderboardManager
		try
		{
			_leaderboardManager = GetNode<LeaderboardManager>("/root/LeaderboardManager");
			if (_leaderboardManager != null)
			{
				_leaderboardManager.ScoreSubmitted += OnScoreSubmitted;
				GD.Print("Connected to LeaderboardManager");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Could not find LeaderboardManager: {e.Message}");
		}

		// Connect signals
		_tryAgainButton.Pressed += OnTryAgainPressed;
		_mainMenuButton.Pressed += OnMainMenuPressed;

		DisplayResults();
	}

private void DisplayResults()
{
	DisplayAverageScore();
	DisplayIndividualTimes();
}

private void DisplayAverageScore()
{
	var latestRecord = _gameManager.PlayerData.GameRecords[0];
	var average = latestRecord.AverageTime;
	_averageScore.Text = $"Average: {average:F3}s";

	var bestScore = _gameManager.GetBestScore();
	if (average <= bestScore)
	{
		_highScoreLabel.Text = "New High Score!";
		GD.Print($"New high score achieved: {average}");
		
		// Submit the high score to the leaderboard
		SubmitHighScore(average);
	}
}

private void SubmitHighScore(float score)
{
	if (_leaderboardManager != null && _gameManager?.PlayerData != null)
	{
		string playerName = string.IsNullOrEmpty(_gameManager.PlayerData.Initials) ? "Anonymous" : _gameManager.PlayerData.Initials;
		GD.Print($"Submitting high score: {playerName} - {score:F3}s");
		_leaderboardManager.SubmitHighScore(playerName, score);
	}
	else
	{
		GD.PrintErr("Cannot submit high score: LeaderboardManager or PlayerData not available");
	}
}

private void OnScoreSubmitted(bool success, string message)
{
	if (success)
	{
		GD.Print($"Score submitted successfully: {message}");
	}
	else
	{
		GD.PrintErr($"Failed to submit score: {message}");
	}
}

private void DisplayIndividualTimes()
{
	var latestRecord = _gameManager.PlayerData.GameRecords[0];
	var times = latestRecord.IndividualTimes;

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
	_mainNode ??= GetNode<Main>("/root/Main");

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
