using Godot;
using System;
using System.Linq;
using System.IO; // Added for file deletion

public partial class GameManager : Node
{
	private const string SAVE_PATH = "user://player_data.tres";

	public PlayerData PlayerData { get; private set; } = new();
	private float[] _currentGameScores = Array.Empty<float>();
	private float _bestTime = float.MaxValue;

	public override void _Ready()
	{
		GD.Print("GameManager initialized");
		LoadPlayerData();
	}

	public void RecordGameScore(float[] times)
	{
		_currentGameScores = times;
		
		// Create new game record
		var record = new GameRecord(times);
		PlayerData.AddGameRecord(record);
		
		// Calculate best time excluding failed attempts (99999.0f)
		var validTimes = times.Where(t => t < 99999.0f);
		if (validTimes.Any())
		{
			float averageTime = validTimes.Average();
			if (averageTime < _bestTime)
			{
				_bestTime = averageTime;
				GD.Print($"New best time: {_bestTime:F2} seconds");
			}
		}

		// Save updated player data
		SavePlayerData();
	}

	public float[] GetCurrentGameScores()
	{
		return _currentGameScores;
	}

	public float GetBestScore()
	{
		return _bestTime;
	}

	public void SavePlayerData()
	{
		Error error = ResourceSaver.Save(PlayerData, SAVE_PATH);
		if (error != Error.Ok)
		{
			GD.PushError($"Failed to save player data. Error code: {error}");
			GD.Print($"Failed to save player data. Error code: {error}");
		}
		else
		{
			GD.Print("Player data saved successfully");
		}
	}

	private void LoadPlayerData()
	{
		if (ResourceLoader.Exists(SAVE_PATH))
		{
			var resource = ResourceLoader.Load<PlayerData>(SAVE_PATH);
			if (resource != null)
			{
				PlayerData = resource;
				GD.Print("Player data loaded successfully");

				// Update best time from loaded records
				if (PlayerData.GameRecords.Count > 0)
				{
					_bestTime = PlayerData.GameRecords.Min(r => r.AverageTime);
				}
			}
			else
			{
				GD.PushError("Failed to load player data");
				GD.Print("Failed to load player data");
			}
		}
		else
		{
			GD.Print("No saved player data found, using defaults");
		}
	}

	public void ResetPlayerData()
	{
		PlayerData = new PlayerData();
		_currentGameScores = Array.Empty<float>();
		_bestTime = float.MaxValue;
		
		// Delete the save file if it exists
		string absoluteSavePath = Path.Combine(OS.GetUserDataDir(), "player_data.tres");
		if (File.Exists(absoluteSavePath))
		{
			File.Delete(absoluteSavePath);
		}
		
		GD.Print("Player data reset successfully");
	}
}
