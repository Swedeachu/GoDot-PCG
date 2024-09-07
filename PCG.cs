using Godot;
using System;

public partial class PCG : TileMap {

  private FastNoiseLite moisture = new FastNoiseLite();
  private FastNoiseLite temperature = new FastNoiseLite();
  private FastNoiseLite altitude = new FastNoiseLite();

  private int chunkWidth = 50;  // Width of each chunk
  private int chunkHeight = 50; // Height of each chunk
  private int gridSize = 10;

  public override void _Ready() {
    // Initialize noise parameters
    moisture.Seed = (int)GD.Randi();
    temperature.Seed = (int)GD.Randi();
    altitude.Seed = (int)GD.Randi();
    altitude.Frequency = 0.005f;

    // Generate the world around the center (0, 0) with grid size 1x1 (just one chunk)
    GenerateWorld(Vector2.Zero, gridSize, gridSize);  
  }

  // Generates a world by generating multiple chunks based on grid size
  private void GenerateWorld(Vector2 center, int gridX, int gridY) {
    // Loop over the grid dimensions (gridX, gridY)
    for (int x = -gridX / 2; x < gridX / 2; x++) {
      for (int y = -gridY / 2; y < gridY / 2; y++) {
        // Calculate the chunk position based on the center and chunk size
        Vector2 chunkPos = new Vector2(center.X + x * chunkWidth, center.Y + y * chunkHeight);
        GenerateChunk(chunkPos);  // Generate each chunk at the calculated position
      }
    }
  }

  // Generates a single chunk at the given position
  private void GenerateChunk(Vector2 position) {
    Vector2I tilePos = LocalToMap(position);  // Convert world position to map position

    for (int x = 0; x < chunkWidth; x++) {
      for (int y = 0; y < chunkHeight; y++) {
        // Get noise values for moisture, temperature, and altitude
        float moist = moisture.GetNoise2D(tilePos.X + x, tilePos.Y + y) * 10;
        float temp = temperature.GetNoise2D(tilePos.X + x, tilePos.Y + y) * 10;
        float alt = altitude.GetNoise2D(tilePos.X + x, tilePos.Y + y) * 10;

        // If altitude is low, set a specific tile
        if (alt < 2) {
          SetCell(0, new Vector2I(tilePos.X + x, tilePos.Y + y), 0, new Vector2I(3, (int)Mathf.Round((temp + 10) / 5)));
        } else {
          SetCell(0, new Vector2I(tilePos.X + x, tilePos.Y + y), 0, new Vector2I((int)(int)Mathf.Round((moist + 10) / 5), (int)Mathf.Round((temp + 10) / 5)));
        }
      }
    }
  }

}
