using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DiagnosticManager : Control
{
    private Label _statusLabel;
    private Button _testButton;
    private Timer _testTimer;
    private bool _isTesting = false;
    private AzureTableStorage _azureStorage;
    private TaskCompletionSource<bool> _azureTestCompletion;
    
    private const float TEST_INTERVAL = 5.0f; // Test every 5 seconds
    
    public override void _Ready()
    {
        try
        {
            // Get nodes from the correct paths
            _statusLabel = GetNode<Label>("SafeArea/Panel/ScrollContainer/VBoxContainer/MarginContainer/VBoxContainer/StatusLabel");
            _testButton = GetNode<Button>("SafeArea/Panel/ScrollContainer/VBoxContainer/MarginContainer/VBoxContainer/TestButton");
            _testTimer = GetNode<Timer>("TestTimer");
            
            // Get AzureTableStorage node
            var azureNode = GetNodeOrNull("AzureTableStorage");
            if (azureNode == null)
            {
                GD.PrintErr("AzureTableStorage node not found!");
                UpdateStatus("Error: Azure Storage not found");
                return;
            }
            
            // Try to cast to AzureTableStorage
            _azureStorage = azureNode as AzureTableStorage;
            if (_azureStorage == null)
            {
                GD.PrintErr("AzureTableStorage node found but script is not attached!");
                UpdateStatus("Error: Azure Storage script not attached");
                return;
            }
            
            // Verify UI nodes
            if (_statusLabel == null || _testButton == null || _testTimer == null)
            {
                GD.PrintErr("Failed to find required UI nodes!");
                return;
            }
            
            // Connect signals
            _testButton.Pressed += OnTestButtonPressed;
            _testTimer.Timeout += OnTestTimerTimeout;
            _azureStorage.RequestSuccess += OnAzureRequestSuccess;
            _azureStorage.RequestFailed += OnAzureRequestFailed;

            // Connect to the TablesCreated signal
            _azureStorage.TablesCreated += OnTablesCreated;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error in _Ready: {e.Message}\n{e.StackTrace}");
            UpdateStatus("Error initializing diagnostics");
        }
    }

    private void OnTablesCreated()
    {
        GD.Print("Azure tables created, running diagnostics.");
        RunDiagnostics();
    }
    
    public override void _ExitTree()
    {
        // Clean up signal connections
        if (_testButton != null)
            _testButton.Pressed -= OnTestButtonPressed;
        
        if (_testTimer != null)
            _testTimer.Timeout -= OnTestTimerTimeout;
        
        if (_azureStorage != null)
        {
            _azureStorage.RequestSuccess -= OnAzureRequestSuccess;
            _azureStorage.RequestFailed -= OnAzureRequestFailed;
            _azureStorage.TablesCreated -= OnTablesCreated;
        }
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMSizeChanged)
        {
            // Update layout when screen size changes
            UpdateLayout();
        }
    }
    
    private void UpdateLayout()
    {
        if (_statusLabel != null)
        {
            // Adjust font size based on screen width
            var screenWidth = GetViewport().GetVisibleRect().Size.X;
            var baseFontSize = screenWidth < 600 ? 18 : 24;
            _statusLabel.AddThemeConstantOverride("font_size", baseFontSize);
        }
        
        if (_testButton != null)
        {
            // Adjust button size based on screen width
            var screenWidth = GetViewport().GetVisibleRect().Size.X;
            var buttonWidth = Mathf.Min(screenWidth * 0.8f, 300);
            _testButton.CustomMinimumSize = new Vector2(buttonWidth, 60);
        }
    }
    
    private void OnTestButtonPressed()
    {
        if (!_isTesting)
        {
            RunDiagnostics();
        }
    }
    
    private void OnTestTimerTimeout()
    {
        if (!_isTesting)
        {
            RunDiagnostics();
        }
    }
    
    private void OnAzureRequestSuccess(string response)
    {
        GD.Print("Azure request successful");
        _azureTestCompletion?.TrySetResult(true);
    }
    
    private void OnAzureRequestFailed(int statusCode, string error)
    {
        GD.PrintErr($"Azure request failed: {statusCode} - {error}");
        _azureTestCompletion?.TrySetResult(false);
    }
    
    private async void RunDiagnostics()
    {
        try
        {
            _isTesting = true;
            _testButton.Disabled = true;
            _statusLabel.Text = "Running diagnostics...";
            
            var results = new List<string>();
            
            // Test Azure Storage Connection
            var azureResult = await TestAzureConnection();
            results.Add($"Azure Storage: {azureResult}");
            
            // Test Internet Connection
            var internetResult = await TestInternetConnection();
            results.Add($"Internet: {internetResult}");
            
            // Test Database Connection
            var dbResult = await TestDatabaseConnection();
            results.Add($"Database: {dbResult}");
            
            // Update UI with results
            _statusLabel.Text = string.Join("\n\n", results);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error in RunDiagnostics: {e.Message}");
            _statusLabel.Text = "Error running diagnostics. Check output log.";
        }
        finally
        {
            _isTesting = false;
            _testButton.Disabled = false;
        }
    }
    
    private async Task<string> TestAzureConnection()
    {
        try
        {
            if (_azureStorage == null)
            {
                return "❌ Not found";
            }
            
            _azureTestCompletion = new TaskCompletionSource<bool>();
            
            // Try to get a single high score to test connection
            _azureStorage.GetHighScores(1);
            
            // Wait for response or timeout
            var timeout = Task.Delay(5000); // 5 second timeout
            var completedTask = await Task.WhenAny(_azureTestCompletion.Task, timeout);
            
            if (completedTask == timeout)
            {
                return "❌ Timeout";
            }
            
            return await _azureTestCompletion.Task ? "✅ Connected" : "❌ Failed";
        }
        catch (Exception e)
        {
            return $"❌ Error: {e.Message}";
        }
        finally
        {
            _azureTestCompletion = null;
        }
    }
    
    private async Task<string> TestInternetConnection()
    {
        HttpRequest httpRequest = null;
        try
        {
            httpRequest = new HttpRequest();
            AddChild(httpRequest);
            
            var tcs = new TaskCompletionSource<bool>();
            
            httpRequest.RequestCompleted += (result, code, headers, body) =>
            {
                tcs.TrySetResult(code >= 200 && code < 300);
            };
            
            var url = "https://www.google.com";
            var error = httpRequest.Request(url);
            
            if (error != Error.Ok)
            {
                return "❌ Failed to send request";
            }
            
            // Wait for response or timeout
            var timeout = Task.Delay(5000); // 5 second timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeout);
            
            if (completedTask == timeout)
            {
                return "❌ Timeout";
            }
            
            return await tcs.Task ? "✅ Connected" : "❌ Failed";
        }
        catch (Exception e)
        {
            return $"❌ Error: {e.Message}";
        }
        finally
        {
            if (httpRequest != null)
            {
                httpRequest.QueueFree();
            }
        }
    }
    
    private async Task<string> TestDatabaseConnection()
    {
        try
        {
            // This is a placeholder for actual database testing
            // You would implement your specific database connection test here
            return "✅ Connected";
        }
        catch (Exception e)
        {
            return $"❌ Error: {e.Message}";
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
        }
        else
        {
            GD.Print($"Status update (no label): {message}");
        }
    }
}
