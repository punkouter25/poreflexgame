using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text; // For Encoding.UTF8

public partial class DiagnosticManager : Control
{
	private Label _statusLabel;
	private Button _testButton;
	private Godot.Timer _testTimer;
	private bool _isTesting = false;
	
	[Export] private string ApiBaseUrl { get; set; } = "http://localhost:5000/api/highscores"; // Placeholder API URL
	
	private const float TEST_INTERVAL = 5.0f; // Test every 5 seconds
	
	public override void _Ready()
	{
		try
		{
			// Get nodes from the correct paths
			_statusLabel = GetNode<Label>("SafeArea/Panel/ScrollContainer/VBoxContainer/MarginContainer/VBoxContainer/StatusLabel");
			_testButton = GetNode<Button>("SafeArea/Panel/ScrollContainer/VBoxContainer/MarginContainer/VBoxContainer/TestButton");
			_testTimer = GetNode<Godot.Timer>("TestTimer");
			
			// Verify UI nodes
			if (_statusLabel == null || _testButton == null || _testTimer == null)
			{
				GD.PrintErr("Failed to find required UI nodes!");
				return;
			}
			
			// Connect signals
			_testButton.Pressed += OnTestButtonPressed;
			_testTimer.Timeout += OnTestTimerTimeout;

			// Initial layout update
			UpdateLayout();
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error in _Ready: {e.Message}\n{e.StackTrace}");
			UpdateStatus("Error initializing diagnostics");
		}
	}
	
	public override void _ExitTree()
	{
		// Clean up signal connections
		if (_testButton != null)
			_testButton.Pressed -= OnTestButtonPressed;
		
		if (_testTimer != null)
			_testTimer.Timeout -= OnTestTimerTimeout;
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
	
	private async void RunDiagnostics()
	{
		try
		{
			_isTesting = true;
			_testButton.Disabled = true;
			_statusLabel.Text = "Running diagnostics...";
			
			var results = new List<string>();
			
			// Test API Connection (replaces TestAzureConnection)
			var apiResult = await TestApiConnection();
			results.Add($"API Connection: {apiResult}");
			
			// Test Internet Connection
			var internetResult = await TestInternetConnection();
			results.Add($"Internet: {internetResult}");
			
			// Test Database Connection (placeholder)
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
	
	private async Task<string> TestApiConnection()
	{
		HttpRequest httpRequest = null;
		try
		{
			if (string.IsNullOrEmpty(ApiBaseUrl))
			{
				return "❌ API URL not set";
			}

			httpRequest = new HttpRequest();
			AddChild(httpRequest);
			
			var tcs = new TaskCompletionSource<bool>();
			
			httpRequest.RequestCompleted += (result, code, headers, body) =>
			{
				// Consider any 2xx status code as success for API connectivity
				tcs.TrySetResult(code >= 200 && code < 300);
			};
			
			var error = httpRequest.Request(ApiBaseUrl); // Make a GET request to the high scores API
			
			if (error != Error.Ok)
			{
				return "❌ Failed to send API request";
			}
			
			// Wait for response or timeout
			var timeout = Task.Delay(5000); // 5 second timeout
			var completedTask = await Task.WhenAny(tcs.Task, timeout);
			
			if (completedTask == timeout)
			{
				return "❌ API Timeout";
			}
			
			return await tcs.Task ? "✅ API Connected" : "❌ API Failed";
		}
		catch (Exception e)
		{
			return $"❌ API Error: {e.Message}";
		}
		finally
		{
			if (httpRequest != null)
			{
				httpRequest.QueueFree();
			}
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
	
	private Task<string> TestDatabaseConnection()
	{
		try
		{
			// This is a placeholder for actual database testing
			// In a real scenario, this would involve making a request to the API
			// that in turn tests its connection to the database.
			return Task.FromResult("✅ Connected (Placeholder)");
		}
		catch (Exception e)
		{
			return Task.FromResult($"❌ Error: {e.Message}");
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
