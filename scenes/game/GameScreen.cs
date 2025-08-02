using Godot;
using System;
using System.Collections.Generic;

public partial class GameScreen : Control
{
	private const string GAME_OVER_SCENE = "res://scenes/game/game_over_screen.tscn";
	private const string MAIN_MENU_SCENE = "res://scenes/menu/main_menu.tscn";
	private const float BAR_SPEED = 300.0f;  // pixels per second (increased from 100 to 300)
	private const int TOTAL_BARS = 6;
	private const float MAX_BAR_HEIGHT = 300.0f;  // Maximum height the bar can grow before game over
	private const float MIN_DELAY = 0.5f;  // Minimum delay before bar starts moving
	private const float MAX_DELAY = 1.5f;  // Maximum delay before bar starts moving
	private const float MAX_REACTION_TIME = 1.0f; // Maximum time in seconds before penalty

	private GameManager _gameManager;
	private Label _currentBarLabel;
	private Button _stopButton;
	private Button _exitButton;
	private Control _barsContainer;
	private AcceptDialog _penaltyDialog;
	private Main _mainNode;

	private int _currentBarIndex = 0;
	private ColorRect _currentBar;
	private Label _currentTimeLabel;
	private ulong _barStartTime = 0;
	private List<float> _recordedTimes = new();
	private bool _isBarMoving = false;
	private float _barPosition = 0.0f;
	private int _barDirection = 1;
	private float _startDelay = 0.0f;
	private float _delayTimer = 0.0f;
	private bool _waitingForDelay = false;

	public override void _Ready()
	{
		GD.Print("Game screen initialized");
		
		// Get node references
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_currentBarLabel = GetNode<Label>("CurrentBarLabel");
		_stopButton = GetNode<Button>("StopButton");
		_exitButton = GetNode<Button>("ExitButton");
		_barsContainer = GetNode<Control>("BarsContainer");

		// Create penalty dialog
		_penaltyDialog = new AcceptDialog();
		_penaltyDialog.DialogText = "PENALTY!\nToo early or too slow!";
		_penaltyDialog.Title = "Game Over";
		_penaltyDialog.Confirmed += OnPenaltyDialogConfirmed;
		AddChild(_penaltyDialog);

		// Connect signals
		_stopButton.Pressed += OnStopPressed;
		_exitButton.Pressed += OnExitPressed;

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

		// Initialize first bar
		SetupCurrentBar();
		StartBarMovement();
	}

	public override void _Process(double delta)
	{
		if (_waitingForDelay)
		{
			_delayTimer += (float)delta;
			if (_delayTimer >= _startDelay)
			{
				_waitingForDelay = false;
				_isBarMoving = true;
				_barStartTime = Time.GetTicksMsec();
				GD.Print("Delay finished, bar starting to move");
			}
		}
		else if (_isBarMoving)
		{
			// Update bar position
			_barPosition += BAR_SPEED * (float)delta * _barDirection;

			// Calculate new height - growing upward from bottom
			float newHeight = 50.0f + _barPosition;  // Start at 50 and grow upward

			// Update bar size - maintain fixed width of 30
			_currentBar.CustomMinimumSize = new Vector2(30, newHeight);

			// Update time internally but don't show it
			float currentTimeSeconds = (Time.GetTicksMsec() - _barStartTime) / 1000.0f;

			// Check if bar has hit the top or time exceeded
			if (newHeight >= MAX_BAR_HEIGHT || currentTimeSeconds >= MAX_REACTION_TIME)
			{
				GD.Print("Game Over - " + (newHeight >= MAX_BAR_HEIGHT ? "Bar hit top!" : "Time exceeded!"));
				ShowPenalty("Too slow! Try to react faster!");
				return;
			}
		}
	}

	private void SetupCurrentBar()
	{
		var barContainer = _barsContainer.GetChild(_currentBarIndex);
		_currentBar = barContainer.GetNode<ColorRect>($"Bar{_currentBarIndex + 1}");
		_currentTimeLabel = barContainer.GetNode<Label>("TimeLabel");
		_currentTimeLabel.Hide(); // Hide the time label initially
		_currentTimeLabel.Text = ""; // Set empty text instead of "0.000s"
	}

	private void StartBarMovement()
	{
		GD.Print($"Starting bar {_currentBarIndex + 1}");
		_currentBarLabel.Text = $"Round: {_currentBarIndex + 1}/{TOTAL_BARS}";

		// Reset bar properties
		_barPosition = 0.0f;
		_barDirection = 1;
		_currentBar.CustomMinimumSize = new Vector2(30, 50);  // Fixed width of 30, height of 50
		_currentBar.Modulate = new Color(0.2f, 0.65098f, 0.2f);  // Reset color to green

		// Set up random delay
		_startDelay = (float)GD.RandRange(MIN_DELAY, MAX_DELAY);
		_delayTimer = 0.0f;
		_waitingForDelay = true;
		_isBarMoving = false;
		GD.Print($"Waiting for delay: {_startDelay} seconds");

		_stopButton.Disabled = false;
	}

	private void OnStopPressed()
	{
		if (_waitingForDelay)
		{
			GD.Print("Pressed too early!");
			ShowPenalty("Too early! Wait for the bar to start moving!");
			return;
		}

		if (!_isBarMoving)
			return;

		_isBarMoving = false;
		_stopButton.Disabled = true;

		// Calculate time
		float reactionTime = (Time.GetTicksMsec() - _barStartTime) / 1000.0f;
		_recordedTimes.Add(reactionTime);

		GD.Print($"Bar {_currentBarIndex + 1} time: {reactionTime}");

		// Show and update the time label
		_currentTimeLabel.Text = $"{reactionTime:F3}s";
		_currentTimeLabel.Show();

		// Preserve the bar's width when highlighting
		float currentHeight = _currentBar.CustomMinimumSize.Y;
		_currentBar.Modulate = new Color(1, 1, 0);  // Yellow highlight
		_currentBar.CustomMinimumSize = new Vector2(30, currentHeight);  // Keep the same width

		// Check if game is complete
		_currentBarIndex++;
		if (_currentBarIndex >= TOTAL_BARS)
		{
			EndGame();
		}
		else
		{
			// Setup next bar
			SetupCurrentBar();
			StartBarMovement();
		}
	}

	private void ShowPenalty(string message)
	{
		_isBarMoving = false;
		_stopButton.Disabled = true;
		
		// Preserve width when showing penalty
		float currentHeight = _currentBar.CustomMinimumSize.Y;
		_currentBar.Modulate = new Color(1, 0, 0);  // Red highlight for failure
		_currentBar.CustomMinimumSize = new Vector2(30, currentHeight);  // Keep the same width
		
		_penaltyDialog.DialogText = "PENALTY!\n" + message;
		_penaltyDialog.PopupCentered();
	}

	private void OnPenaltyDialogConfirmed()
	{
		ReturnToMainMenu();
	}

	private void ReturnToMainMenu()
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

	private void EndGame()
	{
		GD.Print("Game complete, recording score");
		_gameManager.RecordGameScore(_recordedTimes.ToArray());

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
			GD.Print($"Changing scene to: {GAME_OVER_SCENE} through Main node");
			_mainNode.ChangeScene(GAME_OVER_SCENE);
		}
		else
		{
			GD.PushError("Could not find Main node, falling back to direct scene change");
			GD.Print("Current scene tree structure:");
			PrintSceneTree(GetTree().Root, 0);
			Error error = GetTree().ChangeSceneToFile(GAME_OVER_SCENE);
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

	private void OnExitPressed()
	{
		GD.Print("Exiting to main menu");
		ReturnToMainMenu();
	}
}
