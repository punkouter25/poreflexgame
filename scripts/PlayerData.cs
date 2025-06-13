using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerData : Resource
{
    [Export]
    public string Initials { get; set; } = "";

    // Store serialized data as strings to work around export limitations
    [Export]
    public string[] SerializedRecords { get; set; } = Array.Empty<string>();

    private List<GameRecord> _gameRecords = new();
    public IReadOnlyList<GameRecord> GameRecords => _gameRecords;

    public PlayerData()
    {
        RebuildGameRecords();
    }

    private void RebuildGameRecords()
    {
        _gameRecords.Clear();
        foreach (var serializedRecord in SerializedRecords)
        {
            try
            {
                var parts = serializedRecord.Split('|');
                if (parts.Length == 3)
                {
                    // Parse times
                    var times = parts[0].Split(',')
                        .Select(t => float.Parse(t))
                        .ToArray();

                    var record = new GameRecord(times)
                    {
                        AverageTime = float.Parse(parts[1]),
                        Date = DateTime.Parse(parts[2])
                    };
                    _gameRecords.Add(record);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to parse game record: {e.Message}");
            }
        }
    }

    public void AddGameRecord(GameRecord record)
    {
        _gameRecords.Insert(0, record);
        
        // Update the serialized records
        var newSerializedRecords = new string[_gameRecords.Count];
        for (int i = 0; i < _gameRecords.Count; i++)
        {
            var gameRecord = _gameRecords[i];
            // Format: times|averageTime|date
            newSerializedRecords[i] = string.Join(',', gameRecord.IndividualTimes) + "|" 
                + gameRecord.AverageTime.ToString() + "|" 
                + gameRecord.Date.ToString("o");
        }

        SerializedRecords = newSerializedRecords;
    }
}

public class GameRecord
{
    public float[] IndividualTimes { get; set; }
    public float AverageTime { get; set; }
    public DateTime Date { get; set; }

    public GameRecord(float[] times)
    {
        IndividualTimes = times;
        // Calculate average excluding failed attempts (99999.0f)
        float sum = 0;
        int validCount = 0;
        foreach (float time in times)
        {
            if (time < 99999.0f)
            {
                sum += time;
                validCount++;
            }
        }
        AverageTime = validCount > 0 ? sum / validCount : 99999.0f;
        Date = DateTime.Now;
    }
} 