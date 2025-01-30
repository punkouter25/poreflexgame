using Godot;
using System;

public partial class ScoreData : Resource
{
    [Export]
    public int Rank { get; set; }

    [Export]
    public string Initials { get; set; }

    [Export]
    public float Score { get; set; }

    [Export]
    public string Date { get; set; }

    public ScoreData()
    {
        Rank = 0;
        Initials = "";
        Score = 0.0f;
        Date = "";
    }

    public ScoreData(int rank, string initials, float score, string date)
    {
        Rank = rank;
        Initials = initials;
        Score = score;
        Date = date;
    }
} 