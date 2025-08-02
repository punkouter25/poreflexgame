using Godot;
using System;
using System.Linq;

public partial class AndroidGameManager : Node
{
	private const string SAVE_PATH = "user://player_data_android.save";
	
	public PlayerData PlayerData { get; private set; } = new();
	
	[Signal]
	public delegate void GameStartedEventHandler();
	
	[Signal]
	public delegate void GameEndedEventHandler(float[] reactionTimes);
	
	public override void _Ready()
	{
		GD.Print("AndroidGameManager: Starting initialization");
		try
		{
			LoadPlayerData();
			GD.Print("AndroidGameManager: Initialization successful");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"AndroidGameManager: Initialization failed - {ex.Message}");
			// Create default data if loading fails
			PlayerData = new PlayerData();
		}
	}
	
	public void StartGame()
	{
		GD.Print("AndroidGameManager: Game started");
		EmitSignal(SignalName.GameStarted);
	}
	
	public void EndGame(float[] reactionTimes)
	{
		GD.Print($"AndroidGameManager: Game ended with times: [{string.Join(", ", reactionTimes)}]");
		
		try
		{
			var gameRecord = new GameRecord(reactionTimes);
			PlayerData.AddGameRecord(gameRecord);
			
			GD.Print($"AndroidGameManager: New game record added with average: {gameRecord.AverageTime}");
			SavePlayerData();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"AndroidGameManager: Error saving game record - {ex.Message}");
		}
		
		EmitSignal(SignalName.GameEnded, reactionTimes);
	}
	
	public float GetBestAverageTime()
	{
		try
		{
			if (PlayerData.GameRecords.Count > 0)
			{
				return PlayerData.GameRecords.Min(record => record.AverageTime);
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"AndroidGameManager: Error getting best time - {ex.Message}");
		}
		return 99999.0f; // Default for no records
	}
	
	private void LoadPlayerData()
	{
		if (FileAccess.FileExists(SAVE_PATH))
		{
			try
			{
				using var file = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
				if (file != null)
				{
					var json = new Json();
					var parseResult = json.Parse(file.GetAsText());
					
					if (parseResult == Error.Ok)
					{
						var data = json.Data.AsGodotDictionary();
						
						// Load serialized records
						if (data.ContainsKey("serialized_records"))
						{
							var recordsArray = data["serialized_records"].AsGodotArray();
							var serializedRecords = new string[recordsArray.Count];
							for (int i = 0; i < recordsArray.Count; i++)
							{
								serializedRecords[i] = recordsArray[i].AsString();
							}
							PlayerData.SerializedRecords = serializedRecords;
						}
						
						GD.Print($"AndroidGameManager: Loaded {PlayerData.GameRecords.Count} game records");
					}
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"AndroidGameManager: Failed to load player data - {ex.Message}");
				PlayerData = new PlayerData();
			}
		}
		else
		{
			GD.Print("AndroidGameManager: No save file found, creating new player data");
			PlayerData = new PlayerData();
		}
	}
	
	private void SavePlayerData()
	{
		try
		{
			using var file = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Write);
			if (file != null)
			{
				var recordsArray = new Godot.Collections.Array();
				foreach (var record in PlayerData.SerializedRecords)
				{
					recordsArray.Add(record);
				}
				
				var data = new Godot.Collections.Dictionary
				{
					["serialized_records"] = recordsArray
				};
				
				var json = Json.Stringify(data);
				file.StoreString(json);
				GD.Print("AndroidGameManager: Player data saved successfully");
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"AndroidGameManager: Failed to save player data - {ex.Message}");
		}
	}
}
