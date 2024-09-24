using Godot;
using System;

public partial class ShakeAndBake : NavigationRegion2D
{

  private PCG pcg;
  private PackedScene pcgScene;

  public override void _Ready() {
    // Load the PCG scene once and reuse it when needed
    pcgScene = (PackedScene)ResourceLoader.Load("res://tile_map.tscn");
    GenerateNewTileMap();
  }

  public override void _Process(double delta) {
    // Detect if the "R" key is pressed
    if (Input.IsActionJustPressed("reset")) { // we need to go through and kill all nodes named Enemy
      ReplaceTileMap();
    }
  }

  private void ReplaceTileMap() {
    // Remove the existing tile map if it exists
    if (pcg != null) {
      RemoveChild(pcg);
      pcg.QueueFree(); // Mark the old pcg instance for deletion
    }

    // Generate a new tile map
    GenerateNewTileMap();
  }

  private void GenerateNewTileMap() {
    // Instantiate a new PCG tile map
    pcg = (PCG)pcgScene.Instantiate();
    AddChild(pcg);
    pcg.Position = Vector2.Zero;
    BakeNavigationPolygon();
  }

}
