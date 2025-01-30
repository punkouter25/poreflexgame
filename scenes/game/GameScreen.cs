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
			float newHeight = 50.0f * (1.0f + (_barPosition / 50.0f));  // Adjusted scaling factor

			// Update bar size
			_currentBar.CustomMinimumSize = new Vector2(_currentBar.CustomMinimumSize.X, newHeight);
			_currentBar.Size = new Vector2(_currentBar.Size.X, newHeight);

			// Update time label - now in seconds with 3 decimal places
			float currentTimeSeconds = (Time.GetTicksMsec() - _barStartTime) / 1000.0f;
			_currentTimeLabel.Text = $"{currentTimeSeconds:F3}s";

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
		_currentTimeLabel.Text = "0.000s";
	}

	private void StartBarMovement()
	{
		GD.Print($"Starting bar {_currentBarIndex + 1}");
		_currentBarLabel.Text = $"Round: {_currentBarIndex + 1}/{TOTAL_BARS}";

		// Reset bar properties
		_barPosition = 0.0f;
		_barDirection = 1;
		_currentBar.CustomMinimumSize = new Vector2(_currentBar.CustomMinimumSize.X, 50);
		_currentBar.Size = new Vector2(_currentBar.Size.X, 50);
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

		// Highlight result
		_currentBar.Modulate = new Color(1, 1, 0);  // Yellow highlight

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
		_currentBar.Modulate = new Color(1, 0, 0);  // Red highlight for failure
		_penaltyDialog.DialogText = "PENALTY!\n" + message;
		_penaltyDialog.PopupCentered();
	}

	private void OnPenaltyDialogConfirmed()
	{
		ReturnToMainMenu();
	}

	private void ReturnToMainMenu()
	{
		GD.Print("Returning to main menu after penalty");
		var tree = GetTree();
		if (tree != null)
		{
			tree.CreateTimer(0.1).Timeout += () =>
			{
				Error error = tree.ChangeSceneToFile(MAIN_MENU_SCENE);
				if (error != Error.Ok)
				{
					GD.PushError($"Failed to change scene. Error code: {error}");
					GD.Print($"Failed to change scene. Error code: {error}");
				}
			};
		}
		else
		{
			GD.PushError("Could not get SceneTree");
			GD.Print("Could not get SceneTree");
		}
	}

	private void EndGame()
	{
		GD.Print("Game complete, recording score");
		_gameManager.RecordGameScore(_recordedTimes.ToArray());

		// Use direct scene transition
		var tree = GetTree();
		if (tree != null)
		{
			GD.Print("Transitioning to game over screen");
			tree.CreateTimer(0.1).Timeout += () =>
			{
				Error error = tree.ChangeSceneToFile(GAME_OVER_SCENE);
				if (error != Error.Ok)
				{
					GD.PushError($"Failed to change scene. Error code: {error}");
					GD.Print($"Failed to change scene. Error code: {error}");
				}
			};
		}
		else
		{
			GD.PushError("Could not get SceneTree");
			GD.Print("Could not get SceneTree");
		}
	}

	private void OnExitPressed()
	{
		GD.Print("Exiting to main menu");
		ReturnToMainMenu();
	}
} 
