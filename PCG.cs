using Godot;
using System;

public partial class PCG : TileMap {

  private FastNoiseLite noise = new FastNoiseLite();

  private int chunkWidth = 200;
  private int chunkHeight = 200;
  private int[,] mapGrid;  // Stores the cave map (1: floor, 0: wall)

  private float fillProbability = 0.45f;  // Initial fill rate for walls
  private int smoothingIterations = 5;    // How many times to smooth the map

  public override void _Ready() {
    // Initialize the noise parameters (if needed for any random use)
    noise.Seed = (int)GD.Randi();
    GenerateCave();
  }

  private void GenerateCave() {
    // Initialize the map with random walls
    mapGrid = new int[chunkWidth, chunkHeight];
    InitializeMap();

    // Apply smoothing iterations to make the cave more cohesive
    for (int i = 0; i < smoothingIterations; i++) {
      SmoothMap();
    }

    // Finally, render the cave by placing tiles
    RenderCave();
  }

  private void InitializeMap() {
    Random rand = new Random();

    for (int x = 0; x < chunkWidth; x++) {
      for (int y = 0; y < chunkHeight; y++) {
        // Fill the map randomly with walls (1) and floors (0)
        if (rand.NextDouble() < fillProbability || x == 0 || y == 0 || x == chunkWidth - 1 || y == chunkHeight - 1) {
          mapGrid[x, y] = 0; // Wall
        } else {
          mapGrid[x, y] = 1; // Floor
        }
      }
    }
  }

  private void SmoothMap() {
    int[,] newMap = (int[,])mapGrid.Clone();

    for (int x = 1; x < chunkWidth - 1; x++) {
      for (int y = 1; y < chunkHeight - 1; y++) {
        int neighborWallCount = GetNeighborWallCount(x, y);

        if (neighborWallCount > 4) {
          newMap[x, y] = 0; // More walls around, turn this into a wall
        } else if (neighborWallCount < 4) {
          newMap[x, y] = 1; // Fewer walls around, make this a floor
        }
      }
    }

    mapGrid = newMap; // Update the map with the smoothed version
  }

  private int GetNeighborWallCount(int gridX, int gridY) {
    int wallCount = 0;

    for (int x = gridX - 1; x <= gridX + 1; x++) {
      for (int y = gridY - 1; y <= gridY + 1; y++) {
        if (x >= 0 && x < chunkWidth && y >= 0 && y < chunkHeight) {
          if (x != gridX || y != gridY) {
            wallCount += mapGrid[x, y] == 0 ? 1 : 0; // Count walls (0)
          }
        } else {
          wallCount++; // Treat out-of-bounds as walls
        }
      }
    }

    return wallCount;
  }

  private void RenderCave() {
    for (int x = 0; x < chunkWidth; x++) {
      for (int y = 0; y < chunkHeight; y++) {
        if (mapGrid[x, y] == 0)  // Wall is 0
        {
          SetCell(0, new Vector2I(x, y), 0, new Vector2I(2, 0));  // wall tile here is (2, 0) in the tile map atlas
        } else { // Floor is 1
          SetCell(0, new Vector2I(x, y), 0, new Vector2I(0, 3));  // floor tile here is (0, 3) in the tile map atlas
        }
      }
    }
  }

}
