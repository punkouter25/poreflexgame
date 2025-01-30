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
    private Main _main;

    public override void _Ready()
    {
        GD.Print("Player registration initialized");

        // Get node references
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _initialsInput = GetNode<LineEdit>("CenterContainer/VBoxContainer/InitialsInput");
        _continueButton = GetNode<Button>("CenterContainer/VBoxContainer/ContinueButton");
        _errorLabel = GetNode<Label>("CenterContainer/VBoxContainer/ErrorLabel");

        // Get Main node reference
        Node currentScene = GetParent();
        _main = currentScene.GetParent<Main>();
        if (_main == null)
        {
            GD.PushError("Could not find Main node");
            return;
        }

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

        // Transition to main menu
        _main.ChangeScene(MAIN_MENU_SCENE);
    }
} 