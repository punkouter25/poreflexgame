using Godot;
using System;

public partial class SplashScreen : Control
{
    private const float SPLASH_DURATION = 2.5f;  // seconds
    private const string NEXT_SCENE_MAIN_MENU = "res://scenes/menu/main_menu.tscn";

    private GameManager _gameManager;
    private Label _loadingLabel;
    private Label _networkStatus;
    private Main _mainNode;
    private string _dots = "";
    private float _dotsTimer = 0;
    private const float DOTS_UPDATE_INTERVAL = 0.5f;

    public override void _Ready()
    {
        GD.Print("Splash screen initialized");

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

        // Get node references
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _loadingLabel = GetNode<Label>("CenterContainer/VBoxContainer/LoadingLabel");
        _networkStatus = GetNode<Label>("NetworkStatus");

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

        if (_mainNode == null)
        {
            GD.PushError("Could not find Main node, cannot transition");
            GD.Print("Current scene tree structure:");
            PrintSceneTree(GetTree().Root, 0);
            return;
        }

        // Always go directly to main menu, skip registration
        _mainNode.ChangeScene(NEXT_SCENE_MAIN_MENU);
        GD.Print("Going directly to main menu");
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
