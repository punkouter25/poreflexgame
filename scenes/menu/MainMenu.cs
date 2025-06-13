using Godot;
using System;
using System.Text.RegularExpressions;

public partial class MainMenu : Control
{
	private const string GAME_SCENE = "res://scenes/game/game_screen.tscn";
	private const string STATS_SCENE = "res://scenes/stats/statistics_screen.tscn";
	private const string LEADERBOARD_SCENE = "res://scenes/leaderboard/leaderboard_screen.tscn";

	private GameManager _gameManager;
	private Button _playButton;
	private Button _statsButton;
	private Button _leaderboardButton;
	private LineEdit _initialsInput;
	private Main _mainNode;

	public override void _Ready()
	{
		GD.Print("Main menu initialized");

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
