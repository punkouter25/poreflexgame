using Godot;
using System;

public partial class Main : Node
{
	[Signal]
	public delegate void SceneChangedEventHandler(string sceneName);

	private Node _currentScene;

	public override void _Ready()
	{
		GD.Print("Main scene initializing...");
		GD.Print($"Main node path: {GetPath()}");
		GD.Print("Scene tree structure:");
		PrintSceneTree(GetTree().GetRoot(), 0);

		_currentScene = GetNode("CurrentScene").GetChildCount() > 0 
			? GetNode("CurrentScene").GetChild(0) 
			: null;
		GD.Print($"Current scene: {(_currentScene != null ? _currentScene.Name : "None")}");
		
		// Load the main menu scene as the initial scene
		if (_currentScene == null)
		{
			GD.Print("No current scene, loading main menu...");
			ChangeScene("res://scenes/menu/main_menu.tscn");
		}
		
		GD.Print("Main scene initialized");
	}

	private void PrintSceneTree(Node node, int level)
	{
		var indent = new string(' ', level * 2);
		GD.Print($"{indent}{node.Name} ({node.GetClass()})");
		foreach (Node child in node.GetChildren())
		{
			PrintSceneTree(child, level + 1);
		}
	}

	public void ChangeScene(string scenePath)
	{
		GD.Print($"Main: Received request to change scene to: {scenePath}");

		var currentSceneNode = GetNode("CurrentScene");

		// Remove current scene if it exists
		if (_currentScene != null)
		{
			GD.Print($"Main: Removing current scene: {_currentScene.Name}");
			currentSceneNode.RemoveChild(_currentScene);
			_currentScene.QueueFree();
		}
		else
		{
			GD.Print("Main: No current scene to remove");
		}

		// Load and instance new scene
		GD.Print($"Main: Attempting to load scene: {scenePath}");
		var newScene = GD.Load<PackedScene>(scenePath);
		if (newScene != null)
		{
			GD.Print("Main: Scene loaded successfully, instantiating...");
			_currentScene = newScene.Instantiate();
			GD.Print("Main: Adding new scene to CurrentScene node");
			currentSceneNode.AddChild(_currentScene);
			GD.Print("Main: Scene change complete, emitting signal");
			EmitSignal(SignalName.SceneChanged, scenePath);
		}
		else
		{
			GD.PushError($"Failed to load scene: {scenePath}");
			GD.Print($"Main: Failed to load scene: {scenePath}");
		}
	}
} 
