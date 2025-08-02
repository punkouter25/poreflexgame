using Godot;
using System;

public partial class PlayerRegistration : Control
{
    private const int MAX_INITIALS_LENGTH = 3;
    private const string MAIN_MENU_SCENE = "res://scenes/menu/main_menu.tscn";

    private GameManager _gameManager;
    private LineEdit _initialsInput;
    private Button _continueButton;
    private Label _errorLabel;
    private Main _mainNode;

    public override void _Ready()
    {
        GD.Print("Player registration initialized");

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
        _initialsInput = GetNode<LineEdit>("CenterContainer/VBoxContainer/InitialsInput");
        _continueButton = GetNode<Button>("CenterContainer/VBoxContainer/ContinueButton");
        _errorLabel = GetNode<Label>("CenterContainer/VBoxContainer/ErrorLabel");

        // Connect signals
        _initialsInput.TextChanged += OnInitialsChanged;
        _continueButton.Pressed += OnContinuePressed;

        // Initial setup
        _initialsInput.MaxLength = MAX_INITIALS_LENGTH;
        _continueButton.Disabled = true;
        _errorLabel.Text = "";
    }

    private void OnInitialsChanged(string newText)
    {
        // Convert to uppercase
        string upperText = newText.ToUpper();
        if (upperText != newText)
        {
            _initialsInput.Text = upperText;
            _initialsInput.CaretColumn = upperText.Length;
        }

        // Validate input
        bool isValid = ValidateInitials(upperText);
        _continueButton.Disabled = !isValid;
        _errorLabel.Text = isValid ? "" : "Please enter 3 letters";
    }

    private bool ValidateInitials(string initials)
    {
        if (string.IsNullOrEmpty(initials) || initials.Length != MAX_INITIALS_LENGTH)
            return false;

        // Check if all characters are letters
        foreach (char c in initials)
        {
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    private void OnContinuePressed()
    {
        string initials = _initialsInput.Text.ToUpper();
        if (!ValidateInitials(initials))
        {
            _errorLabel.Text = "Invalid initials";
            return;
        }

        GD.Print($"Registering player with initials: {initials}");
        
        // Save player data
        _gameManager.PlayerData.Initials = initials;
        _gameManager.SavePlayerData();

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