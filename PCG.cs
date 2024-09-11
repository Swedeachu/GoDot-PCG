using Godot;
using System;

public partial class PCG : TileMap {

  private FastNoiseLite noise = new FastNoiseLite();
  private FastNoiseLite temperatureNoise = new FastNoiseLite();

  private int chunkWidth = 200;
  private int chunkHeight = 200;
  private int[,] mapGrid;  // Stores the cave map (1: floor, 0: wall)

  private float fillProbability = 0.45f;  // Initial fill rate for walls
  private int smoothingIterations = 5;    // How many times to smooth the map

  // biome 1 (cold biome)
  private Vector2I iceWall = new Vector2I(3, 2);
  private Vector2I snowFloor = new Vector2I(1, 0);

  // biome 2 (temperate biome)
  private Vector2I stoneWall = new Vector2I(2, 0);
  private Vector2I dirtFloor = new Vector2I(0, 3);

  // biome 3 (hot biome)
  private Vector2I sandFloor = new Vector2I(1, 3);
  private Vector2I dryWall = new Vector2I(3, 0);

  // biome 4 (jungle/forest biome)
  private Vector2I grassFloor = new Vector2I(2, 3);
  private Vector2I grassWall = new Vector2I(2, 1);

  public override void _Ready() {
    // Initialize noise parameters
    noise.Seed = (int)GD.Randi();
    temperatureNoise.Seed = (int)GD.Randi();
    temperatureNoise.Frequency = 0.01f;  // Adjust this for larger or smaller biomes

    GenerateMap();
  }

  private void GenerateMap() {
    // Initialize the map with random walls
    mapGrid = new int[chunkWidth, chunkHeight];
    InitializeMap();

    // Apply smoothing iterations to make the cave more cohesive
    for (int i = 0; i < smoothingIterations; i++) {
      SmoothMap();
    }

    // Render the cave by placing tiles based on biomes
    RenderMap();
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

  private void RenderMap() {
    for (int x = 0; x < chunkWidth; x++) {
      for (int y = 0; y < chunkHeight; y++) {
        // Get the temperature value for this tile from the noise function
        float temperature = temperatureNoise.GetNoise2D(x, y);

        // Determine the biome based on temperature
        Vector2I floorTile = dirtFloor;  // Default temperate biome
        Vector2I wallTile = stoneWall;   // Default wall

        if (temperature < -0.3f) {
          // Cold biome
          floorTile = snowFloor;
          wallTile = iceWall;
        } else if (temperature > 0.3f) {
          // Hot biome
          floorTile = sandFloor;
          wallTile = dryWall;
        } else if (temperature > 0 && temperature <= 0.3f) {
          // Jungle/forest biome
          floorTile = grassFloor;
          wallTile = grassWall;
        }

        // Set the wall or floor tiles based on the biome
        if (mapGrid[x, y] == 0)  // Wall is 0
        {
          SetCell(0, new Vector2I(x, y), 0, wallTile);  // Set wall tile based on biome
        } else { // Floor is 1
          SetCell(0, new Vector2I(x, y), 0, floorTile);  // Set floor tile based on biome
        }
      }
    }
  }

}
