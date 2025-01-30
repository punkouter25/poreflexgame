using Godot;
using System;

public partial class SplashScreen : Control
{
	private const float SPLASH_DURATION = 2.5f;  // seconds
	private const string NEXT_SCENE_REGISTRATION = "res://scenes/registration/player_registration.tscn";
	private const string NEXT_SCENE_MAIN_MENU = "res://scenes/menu/main_menu.tscn";

	private GameManager _gameManager;
	private Label _loadingLabel;
	private Label _networkStatus;
	private Main _main;

	private string _dots = "";
	private float _dotsTimer = 0;
	private const float DOTS_UPDATE_INTERVAL = 0.5f;

	public override void _Ready()
	{
		GD.Print("Splash screen initialized");

		// Get node references
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_loadingLabel = GetNode<Label>("CenterContainer/VBoxContainer/LoadingLabel");
		_networkStatus = GetNode<Label>("NetworkStatus");
		
		// Get Main node reference
		Node currentScene = GetParent();
		_main = currentScene.GetParent<Main>();
		if (_main == null)
		{
			GD.PushError("Could not find Main node");
			return;
		}

		CheckNetworkStatus();
		// Start transition timer
		GetTree().CreateTimer(SPLASH_DURATION).Timeout += TransitionToNextScene;
	}

	public override void _Process(double delta)
	{
		// Update loading animation
		_dotsTimer += (float)delta;
		if (_dotsTimer >= DOTS_UPDATE_INTERVAL)
		{
			_dotsTimer = 0;
			_dots = _dots.Length >= 3 ? "." : _dots + ".";
			_loadingLabel.Text = "Loading" + _dots;
		}
	}

	private void CheckNetworkStatus()
	{
		// Simple HTTP request to check internet connectivity
		var http = new HttpRequest();
		AddChild(http);
		http.RequestCompleted += OnNetworkCheckCompleted;
		// Try to connect to a reliable server
		Error error = http.Request("https://8.8.8.8");
		if (error != Error.Ok)
		{
			UpdateNetworkStatus(false);
		}
	}

	private void OnNetworkCheckCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		var isConnected = result == (long)HttpRequest.Result.Success;
		UpdateNetworkStatus(isConnected);
	}

	private void UpdateNetworkStatus(bool isConnected)
	{
		_networkStatus.Text = isConnected ? "Online" : "Offline";
		_networkStatus.Modulate = isConnected ? Colors.Green : Colors.Red;
		GD.Print($"Network status: {_networkStatus.Text}");
	}

	private void TransitionToNextScene()
	{
		if (_main == null)
		{
			GD.PushError("Main reference is null, cannot transition");
			return;
		}

		// Check if player is already registered
		var nextScene = NEXT_SCENE_REGISTRATION;
		if (_gameManager != null && !string.IsNullOrEmpty(_gameManager.PlayerData.Initials))
		{
			nextScene = NEXT_SCENE_MAIN_MENU;
			GD.Print("Player already registered, going to main menu");
		}
		else
		{
			GD.Print("New player, going to registration");
		}

		_main.ChangeScene(nextScene);
	}
} 
