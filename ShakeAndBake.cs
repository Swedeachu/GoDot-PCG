using Godot;
using System.Collections.Generic;
using static PCG;

public partial class ShakeAndBake : NavigationRegion2D {

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
      KillEnemies();
      ReplaceTileMap();
    }
  }

  private void KillEnemies() {
    Node root = GetTree().Root;

    foreach (Node child in root.GetChildren()) {
      if (child is Enemy || child is Item) {
        child.QueueFree();
      }
    }

    Node world = root.GetNode("World");
    foreach (Node child in world.GetChildren()) {
      if (child is Player player) {
        player.SetHealth(10); // back to max health
      }
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

    // Create a mixed descriptor that will make a balanced randomized world
    MapGenerationDescriptor descriptor = new MapGenerationDescriptor {
      RoomShapeWeights = new Dictionary<RoomShape, float>
        {
            { RoomShape.Rectangle, 0.25f },
            { RoomShape.Circle, 0.25f },
            { RoomShape.Hexagon, 0.25f },
            { RoomShape.Octagon, 0.15f },
            { RoomShape.Triangle, 0.10f }
        },
      BiomeWeights = new Dictionary<BiomeType, float>
        {
            { BiomeType.Cold, 0f },
            { BiomeType.Temperate, 0.45f },
            //{ BiomeType.Hot, 0.45f },
            //{ BiomeType.Jungle, 0.45f }
        }
    };

    // Generate the map with the descriptor
    pcg.Generate(descriptor);

    BakeNavigationPolygon();
  }

}
