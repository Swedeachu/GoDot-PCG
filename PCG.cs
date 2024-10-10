using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Enemy;

public class MapGenerationDescriptor {
  public Dictionary<RoomShape, float> RoomShapeWeights { get; set; }
  public Dictionary<BiomeType, float> BiomeWeights { get; set; }
}

public partial class PCG : TileMap {

  public static PCG Instance { get; private set; }

  private FastNoiseLite noise = new FastNoiseLite();
  private FastNoiseLite temperatureNoise = new FastNoiseLite();

  public int roomCount = 20;
  private int chunkWidth = 200;
  private int chunkHeight = 200;
  private int[,] mapGrid;  // Stores the cave map (1: floor, 0: wall)

  private float fillProbability = 0.45f;  // Initial fill rate for walls
  private int smoothingIterations = 4;    // How many times to smooth the map

  // Biome tiles
  private Dictionary<BiomeType, (Vector2I floorTile, Vector2I wallTile)> biomeTiles;

  // Noise settings for biome generation
  private float temperatureNoiseFrequency = 0.01f;  // Adjust for biome size

  // The following have a 50% chance of spawning in a fittable floor part of the scene in each biome region (happens last in the generation process)
  public PackedScene enemyScene;
  public PackedScene itemScene;

  // To keep track of occupied tiles to prevent overlapping structures
  private bool[,] occupied;

  private Random rand = new Random();

  // List of rooms
  private List<Room> rooms = new List<Room>();

  private Room spawnRoom;
  Vector2 spawnWorldPosition;

  // To protect rooms and corridors during smoothing
  private bool[,] protectedCells;

  private MapGenerationDescriptor mapDescriptor;
  public EnemyWaveDescriptor enemyWaveDescriptor;
  private ItemWaveDescriptor itemWaveDescriptor;

  private Vector2I spawnCellPos;

  private List<(BiomeType biome, float minTemp, float maxTemp)> biomeTemperatureRanges;

  public override void _Ready() {
    Instance = this;
    // Initialize noise parameters
    noise.Seed = (int)GD.Randi();
    temperatureNoise.Seed = (int)GD.Randi();
    temperatureNoise.Frequency = temperatureNoiseFrequency;

    enemyScene = GD.Load<PackedScene>("res://enemy.tscn");
    itemScene = GD.Load<PackedScene>("res://item.tscn");

    occupied = new bool[chunkWidth, chunkHeight];

    InitializeBiomeTiles();
  }

  public void Generate(MapGenerationDescriptor mapDescriptor, EnemyWaveDescriptor enemyWave, ItemWaveDescriptor itemWaveDescriptor) {
    this.mapDescriptor = mapDescriptor;
    this.enemyWaveDescriptor = enemyWave;
    this.itemWaveDescriptor = itemWaveDescriptor;
    InitializeProtectedCells();
    GenerateMap();
    SpawnPlayerInRandomRoom();
    SpawnEnemyWave();
    SpawnItemWave();
  }

  private void InitializeBiomeTiles() {
    biomeTiles = new Dictionary<BiomeType, (Vector2I floorTile, Vector2I wallTile)>
    {
            // Biome 1 (cold biome, snow and ice)
            { BiomeType.Cold, (new Vector2I(1, 0), new Vector2I(3, 2)) },

            // Biome 2 (temperate biome kind of like a cave, dirt and rock)
            { BiomeType.Temperate, (new Vector2I(0, 3), new Vector2I(2, 0)) },

            // Biome 3 (hot biome, sand and ocean)
            { BiomeType.Hot, (new Vector2I(1, 3), new Vector2I(3, 0)) },

            // Biome 4 (jungle/forest biome)
            { BiomeType.Jungle, (new Vector2I(2, 3), new Vector2I(2, 1)) }
        };
  }

  // put the player in a random room, then go into each other room randomly and spawn stuff in them
  public void SpawnPlayerInRandomRoom() {
    // Ensure there are rooms available
    if (rooms.Count > 0) {
      // Select a random room
      spawnRoom = rooms[rand.Next(rooms.Count)];

      // Find a valid spawn position in the selected room
      spawnWorldPosition = FindSpawnPositionInRoom(spawnRoom, 100, true);

      // Locate the Player node in the scene tree
      var worldNode = GetNode<Node>("/root/World");
      var player = worldNode.GetNode<Player>("Player");

      // Set the player's global position
      player.GlobalPosition = spawnWorldPosition;
    } else {
      GD.PrintErr("No rooms available to spawn the player.");
    }
  }

  public void SpawnBoss() {
    List<Room> eligibleRooms = new List<Room>(rooms);
    Room room = eligibleRooms[rand.Next(eligibleRooms.Count)];
    // Find a valid spawn position in the selected room
    Vector2 spawnWorldPosition = FindSpawnPositionInRoom(room);
    // Spawn an enemy
    var enemy = (Enemy)enemyScene.Instantiate();
    GetTree().Root.CallDeferred("add_child", enemy);
    enemy.GlobalPosition = spawnWorldPosition;
    enemy.SetEnemyType(EnemyType.Boss);
  }

  private void SpawnItemWave() {
    List<Room> eligibleRooms = new List<Room>(rooms);

    var itemCounts = itemWaveDescriptor.ItemCounts;

    // Iterate through each item type in the descriptor
    foreach (var entry in itemCounts) {
      var itemType = entry.Key;
      int count = entry.Value;

      for (int i = 0; i < count; i++) {
        if (eligibleRooms.Count > 0) {
          // Select a random room from the eligible rooms
          Room room = eligibleRooms[rand.Next(eligibleRooms.Count)];
          eligibleRooms.Remove(room); // Don't want repeat rooms with multiple items

          // Find a valid spawn position in the selected room
          Vector2 spawnWorldPosition = FindSpawnPositionInRoom(room);

          // Spawn item and set position
          var item = (Item)itemScene.Instantiate();
          GetTree().Root.CallDeferred("add_child", item);
          item.GlobalPosition = spawnWorldPosition;

          // Set the item type based on the descriptor
          item.SetType(itemType);
        } else {
          GD.PrintErr("No eligible rooms available to spawn item.");
          break;
        }
      }
    }
  }

  public void SpawnEnemyWave() {
    // Define the minimum tile distance (Manhattan distance) required between the player's position and the enemy spawn positions
    Vector2I playerGridPos = spawnCellPos;
    GD.Print("\nPlayer grid position: " + playerGridPos);
    int minimumTileDistance = 100; // Minimum Manhattan distance in tile units

    // Get the enemy wave descriptor
    var enemyCounts = enemyWaveDescriptor.EnemyCounts;

    // Iterate through each enemy type in the descriptor
    foreach (var entry in enemyCounts) {
      EnemyType enemyType = entry.Key;
      int count = entry.Value;

      for (int i = 0; i < count; i++) {
        Vector2 spawnWorldPosition = FindRandomValidSpawnPosition(minimumTileDistance, playerGridPos);

        if (spawnWorldPosition == Vector2.Zero) {
          GD.PrintErr("No valid spawn position found for enemy.");
          return;
        }

        // Spawn enemy and set position
        var enemy = (Enemy)enemyScene.Instantiate();
        GetTree().Root.CallDeferred("add_child", enemy);
        enemy.GlobalPosition = spawnWorldPosition;
        GD.Print($"Spawning Enemy of type {enemyType} at {spawnWorldPosition}");
        enemy.SetEnemyType(enemyType);
      }
    }
  }

  private Vector2 FindRandomValidSpawnPosition(int minimumTileDistance, Vector2I playerGridPos, int maxAttempts = 1000) {
    Vector2 spawnWorldPosition = Vector2.Zero;
    bool positionFound = false;

    for (int attempt = 0; attempt < maxAttempts; attempt++) {
      // Pick random coordinates within the map bounds
      int spawnX = rand.Next(0, chunkWidth);
      int spawnY = rand.Next(0, chunkHeight);

      // Check if the tile is a floor tile and not adjacent to a wall
      if (mapGrid[spawnX, spawnY] == 1 && !IsNextToWall(spawnX, spawnY)) {
        // Calculate the Manhattan distance between the player's grid position and the spawn position
        int manhattanDistance = Mathf.Abs(playerGridPos.X - spawnX) + Mathf.Abs(playerGridPos.Y - spawnY);

        if (manhattanDistance >= minimumTileDistance) {
          // Convert tile coordinates to local coordinates, then to world coordinates
          Vector2I cellPosition = new Vector2I(spawnX, spawnY);
          Vector2 localPosition = MapToLocal(cellPosition);
          spawnWorldPosition = this.ToGlobal(localPosition);

          positionFound = true;
          break;
        }
      }
    }

    if (!positionFound) {
      // Fallback to (0,0) if no valid position was found after max attempts
      return Vector2.Zero;
    }

    return spawnWorldPosition;
  }


  private float RoomPointDistance(Room room, Vector2 vec) {
    Vector2I cellPositionA = new Vector2I(room.X, room.Y);
    Vector2 localPositionA = MapToLocal(cellPositionA);
    var centerWorldPositionA = this.ToGlobal(localPositionA);

    // Calculate the Euclidean distance
    float distanceX = centerWorldPositionA.X - vec.X;
    float distanceY = centerWorldPositionA.Y - vec.Y;
    return Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);
  }

  private void SpawnEnemyInRoom(Room room) {
    var enemy = (Enemy)enemyScene.Instantiate();
    GetTree().Root.CallDeferred("add_child", enemy);
    enemy.GlobalPosition = MapToLocal(new Vector2I(room.CenterX, room.CenterY));
  }

  private Vector2 FindSpawnPositionInRoom(Room room, int maxAttempts = 100, bool cache = false) {
    Vector2 spawnWorldPosition = Vector2.Zero;
    bool positionFound = false;

    for (int i = 0; i < maxAttempts; i++) {
      // Select a random tile within the room's boundaries
      int spawnX = rand.Next(room.X, room.X + room.Width);
      int spawnY = rand.Next(room.Y, room.Y + room.Height);

      // Check if the selected tile is a floor and not adjacent to a wall
      if (mapGrid[spawnX, spawnY] == 1 && !IsNextToWall(spawnX, spawnY)) {
        // Convert tile coordinates to local coordinates
        Vector2I cellPosition = new Vector2I(spawnX, spawnY);
        Vector2 localPosition = MapToLocal(cellPosition);
        if (cache) spawnCellPos = cellPosition;

        // Convert local coordinates to global coordinates
        spawnWorldPosition = this.ToGlobal(localPosition);

        positionFound = true;
        break;
      }
    }

    if (!positionFound) {
      // Fallback to the center of the room if no valid position was found
      Vector2I centerCell = new Vector2I(room.CenterX, room.CenterY);
      Vector2 localCenterPosition = MapToLocal(centerCell);
      spawnWorldPosition = this.ToGlobal(localCenterPosition);
    }

    return spawnWorldPosition;
  }

  // Helper method to calculate the distance between two rooms
  private float GetRoomDistance(Room roomA, Room roomB) {
    if (roomA == null || roomB == null) {
      return float.MaxValue; // If either room is null, return maximum possible distance
    }

    // maybe make a helper method for converting room tile positions to world positions

    Vector2I cellPositionA = new Vector2I(roomA.X, roomA.Y);
    Vector2 localPositionA = MapToLocal(cellPositionA);
    var centerWorldPositionA = this.ToGlobal(localPositionA);

    Vector2I cellPositionB = new Vector2I(roomB.X, roomB.Y);
    Vector2 localPositionB = MapToLocal(cellPositionB);
    var centerWorldPositionB = this.ToGlobal(localPositionB);

    // Calculate the Euclidean distance between the center points of the two rooms
    float distanceX = centerWorldPositionA.X - centerWorldPositionB.X;
    float distanceY = centerWorldPositionA.Y - centerWorldPositionB.Y;
    return Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);
  }

  private bool IsNextToWall(int x, int y) {
    // Check the four neighboring tiles (up, down, left, right)
    int[,] directions = new int[,] {
        { 0, -1 },  // Up
        { 0, 1 },   // Down
        { -1, 0 },  // Left
        { 1, 0 }    // Right
    };

    // Loop through each direction and check for walls
    for (int i = 0; i < 4; i++) {
      int nx = x + directions[i, 0];
      int ny = y + directions[i, 1];

      // Ensure we are within map bounds and check if it's a wall (assuming wall tiles are not 1)
      if (nx >= 0 && ny >= 0 && nx < mapGrid.GetLength(0) && ny < mapGrid.GetLength(1)) {
        if (mapGrid[nx, ny] != 1) {
          return true;  // Neighboring tile is a wall
        }
      }
    }

    return false;  // No neighboring walls
  }

  private void InitializeProtectedCells() {
    protectedCells = new bool[chunkWidth, chunkHeight];
  }

  private void GenerateMap() {
    // Initialize the map grid with walls
    mapGrid = new int[chunkWidth, chunkHeight];
    for (int x = 0; x < chunkWidth; x++) {
      for (int y = 0; y < chunkHeight; y++) {
        mapGrid[x, y] = 0; // Wall
      }
    }

    // Generate rooms and corridors
    GenerateRooms();
    ConnectRoomsWithBranches();

    // Apply smoothing iterations to make the cave more cohesive
    for (int i = 0; i < smoothingIterations; i++) {
      SmoothMap();
    }

    // Compute biome temperature ranges based on the descriptor
    ComputeBiomeTemperatureRanges();

    // Render the cave by placing tiles based on biomes
    RenderMap();
  }

  private void GenerateRooms() {
    for (int i = 0; i < roomCount; i++) {
      // Randomly select size and style
      int sizeCategory = rand.Next(3); // 0: small, 1: medium, 2: large
      int style = rand.Next(3); // 0: rounded corners, 1: rough edges, 2: interior columns
      RoomShape shape = GetRandomRoomShape(mapDescriptor.RoomShapeWeights);
      float rotation = 0;

      // Assign rotation for non-rectangle shapes
      if (shape != RoomShape.Rectangle) {
        rotation = rand.Next(0, 360); // Random rotation between 0 and 359 degrees
      }

      int width = 0, height = 0;

      switch (sizeCategory) {
        case 0: // Small
        width = rand.Next(6, 10);
        height = rand.Next(6, 10);
        break;
        case 1: // Medium
        width = rand.Next(12, 16);
        height = rand.Next(12, 16);
        break;
        case 2: // Large
        width = rand.Next(20, 24);
        height = rand.Next(20, 24);
        break;
      }

      int x = rand.Next(1, chunkWidth - width - 1);
      int y = rand.Next(1, chunkHeight - height - 1);

      Room room = new Room(x, y, width, height, style, shape, rotation);

      // Check for overlaps
      bool overlaps = false;
      foreach (Room otherRoom in rooms) {
        if (room.Intersects(otherRoom)) {
          overlaps = true;
          break;
        }
      }

      if (!overlaps) {
        rooms.Add(room);

        /*
        Vector2I cellPosition = new Vector2I(room.X, room.Y);
        Vector2 localPosition = MapToLocal(cellPosition);
        var centerWorldPosition = this.ToGlobal(localPosition);
        PackedScene xScene = GD.Load<PackedScene>("res://x.tscn");
        var marker = (Node2D)xScene.Instantiate();
        GetTree().Root.CallDeferred("add_child", marker);
        marker.Name = "marker";
        marker.GlobalPosition = centerWorldPosition;
        GD.Print("spawning marker: " + centerWorldPosition.ToString());
        if (room == spawnRoom) marker.SelfModulate = new Color(0, 1, 0);
        ShakeAndBake.iShouldntExistList.Add(marker);
        */

        CarveRoom(room);
      }
    }
  }

  private RoomShape GetRandomRoomShape(Dictionary<RoomShape, float> weights) {
    float totalWeight = weights.Values.Sum();
    float randomValue = (float)(rand.NextDouble() * totalWeight);
    foreach (var kvp in weights) {
      if (randomValue < kvp.Value) {
        return kvp.Key;
      }
      randomValue -= kvp.Value;
    }
    // Default return, in case of rounding errors
    return weights.Keys.First();
  }

  private void CarveRoom(Room room) {
    switch (room.Shape) {
      case RoomShape.Rectangle:
      CarveRectangle(room);
      break;
      case RoomShape.Circle:
      CarveCircle(room);
      break;
      case RoomShape.Octagon:
      CarvePolygon(room, 8);
      break;
      case RoomShape.Hexagon:
      CarvePolygon(room, 6);
      break;
      case RoomShape.Triangle:
      CarveTriangle(room);
      break;
    }
  }

  private void ComputeBiomeTemperatureRanges() {
    var sortedBiomes = mapDescriptor.BiomeWeights.OrderBy(kvp => kvp.Key).ToList(); // Or some specific order
    float totalWeight = mapDescriptor.BiomeWeights.Values.Sum();

    float cumulative = 0;
    biomeTemperatureRanges = new List<(BiomeType biome, float minTemp, float maxTemp)>();

    foreach (var kvp in sortedBiomes) {
      float weight = kvp.Value / totalWeight;
      float minProb = cumulative;
      cumulative += weight;
      float maxProb = cumulative;
      // Map probabilities to temperature thresholds
      float minTemp = minProb * 2 - 1; // Map [0,1] to [-1,1]
      float maxTemp = maxProb * 2 - 1;
      biomeTemperatureRanges.Add((kvp.Key, minTemp, maxTemp));
    }
  }

  private BiomeType GetBiomeTypeFromTemperature(float temperature) {
    foreach (var range in biomeTemperatureRanges) {
      if (temperature >= range.minTemp && temperature < range.maxTemp) {
        return range.biome;
      }
    }
    // In case temperature == 1.0 (max value)
    return biomeTemperatureRanges.Last().biome;
  }

  private void CarveRectangle(Room room) {
    for (int x = room.X; x < room.X + room.Width; x++) {
      for (int y = room.Y; y < room.Y + room.Height; y++) {
        bool carveTile = true;

        if (room.Style == 0) { // Rounded corners
          if ((x == room.X || x == room.X + room.Width - 1) && (y == room.Y || y == room.Y + room.Height - 1)) {
            carveTile = false; // Leave corners as walls
          }
        } else if (room.Style == 1) { // Rough edges
          if (rand.NextDouble() < 0.1) {
            carveTile = false; // Randomly leave some walls
          }
        } else if (room.Style == 2) { // Interior columns
          if ((x - room.X) % 3 == 0 && (y - room.Y) % 3 == 0) {
            mapGrid[x, y] = 0; // Place a wall tile
            protectedCells[x, y] = true;
            continue;
          }
        }

        if (carveTile) {
          mapGrid[x, y] = 1; // Floor
          protectedCells[x, y] = true; // Protect this cell
        } else {
          mapGrid[x, y] = 0; // Wall
          protectedCells[x, y] = true;
        }
      }
    }
  }

  private void CarveCircle(Room room) {
    int centerX = room.X + room.Width / 2;
    int centerY = room.Y + room.Height / 2;
    float radiusX = room.Width / 2f;
    float radiusY = room.Height / 2f;

    for (int x = room.X; x < room.X + room.Width; x++) {
      for (int y = room.Y; y < room.Y + room.Height; y++) {
        // Apply rotation if needed
        Vector2 relativePos = new Vector2(x - centerX, y - centerY);
        float radians = Mathf.DegToRad(room.Rotation);
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float rotatedX = relativePos.X * cos - relativePos.Y * sin;
        float rotatedY = relativePos.X * sin + relativePos.Y * cos;

        // Ellipse equation
        if ((rotatedX * rotatedX) / (radiusX * radiusX) + (rotatedY * rotatedY) / (radiusY * radiusY) <= 1) {
          mapGrid[x, y] = 1; // Floor
          protectedCells[x, y] = true;
        } else {
          mapGrid[x, y] = 0; // Wall
          protectedCells[x, y] = true;
        }
      }
    }
  }

  private void CarvePolygon(Room room, int sides) {
    // Approximate a regular polygon within the room bounds
    int centerX = room.X + room.Width / 2;
    int centerY = room.Y + room.Height / 2;
    float radius = Mathf.Min(room.Width, room.Height) / 2f;

    for (int x = room.X; x < room.X + room.Width; x++) {
      for (int y = room.Y; y < room.Y + room.Height; y++) {
        Vector2 point = new Vector2(x - centerX, y - centerY);
        // Apply rotation
        float radians = Mathf.DegToRad(room.Rotation);
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float rotatedX = point.X * cos - point.Y * sin;
        float rotatedY = point.X * sin + point.Y * cos;

        // Compute angle and distance
        float angle = Mathf.Atan2(rotatedY, rotatedX);
        float distance = rotatedX * Mathf.Cos(angle * sides / 2) + rotatedY * Mathf.Sin(angle * sides / 2);
        float threshold = radius * Mathf.Cos(Mathf.Pi / sides);

        if (Mathf.Abs(distance) <= threshold) {
          mapGrid[x, y] = 1; // Floor
          protectedCells[x, y] = true;
        } else {
          mapGrid[x, y] = 0; // Wall
          protectedCells[x, y] = true;
        }
      }
    }
  }

  private void CarveTriangle(Room room) {
    // Carve an isoceles or equilateral triangle within the room bounds
    int centerX = room.X + room.Width / 2;
    int centerY = room.Y + room.Height / 2;
    float height = room.Height / 2f;
    float baseWidth = room.Width / 2f;

    for (int x = room.X; x < room.X + room.Width; x++) {
      for (int y = room.Y; y < room.Y + room.Height; y++) {
        Vector2 point = new Vector2(x - centerX, y - centerY);
        // Apply rotation
        float radians = Mathf.DegToRad(room.Rotation);
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float rotatedX = point.X * cos - point.Y * sin;
        float rotatedY = point.X * sin + point.Y * cos;

        // Simple triangle condition (pointing upwards)
        if (rotatedY >= 0 && rotatedY <= height) {
          float relativeX = Mathf.Abs(rotatedX);
          if (relativeX <= (baseWidth * (height - rotatedY) / height)) {
            mapGrid[x, y] = 1; // Floor
            protectedCells[x, y] = true;
          } else {
            mapGrid[x, y] = 0; // Wall
            protectedCells[x, y] = true;
          }
        } else {
          mapGrid[x, y] = 0; // Wall
          protectedCells[x, y] = true;
        }
      }
    }
  }

  private Vector2 RotatePoint(Vector2 point, float radians) {
    float cos = Mathf.Cos(radians);
    float sin = Mathf.Sin(radians);
    return new Vector2(point.X * cos - point.Y * sin, point.X * sin + point.Y * cos);
  }

  private bool IsPointInRegularPolygon(Vector2 point, int sides, float radius) {
    if (sides < 3) return false;
    float angle = 2 * Mathf.Pi / sides;
    float step = Mathf.Pi / sides;
    for (int i = 0; i < sides; i++) {
      float theta = i * angle;
      Vector2 vertex = new Vector2(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta));
      Vector2 nextVertex = new Vector2(radius * Mathf.Cos(theta + angle), radius * Mathf.Sin(theta + angle));
      // Ray-casting algorithm can be implemented here if needed
    }
    // Placeholder for polygon inclusion logic
    return false;
  }

  private void ConnectRoomsWithBranches() {
    int branchCount = 3;
    var branches = CreateBranches(branchCount);

    foreach (var branch in branches) {
      for (int i = 0; i < branch.Count - 1; i++) {
        int corridorStyle = rand.Next(3); // 0: straight, 1: snaking, 2: forking
        Room roomA = branch[i];
        Room roomB = branch[i + 1];

        switch (corridorStyle) {
          case 0:
          //CreateStraightCorridor(roomA, roomB);
          //break;
          case 1:
          CreateSnakingCorridor(roomA, roomB);
          break;
          case 2:
          CreateForkingCorridor(roomA, roomB);
          break;
        }
      }
    }

    // Connect branches together
    for (int i = 0; i < branches.Count - 1; i++) {
      Room roomA = branches[i][rand.Next(branches[i].Count)];
      Room roomB = branches[i + 1][rand.Next(branches[i + 1].Count)];
      // CreateStraightCorridor(roomA, roomB);
      CreateSnakingCorridor(roomA, roomB);
    }
  }

  private List<List<Room>> CreateBranches(int branchCount) {
    List<List<Room>> branches = new List<List<Room>>();
    for (int i = 0; i < branchCount; i++) {
      branches.Add(new List<Room>());
    }

    // Shuffle rooms to randomize distribution
    List<Room> shuffledRooms = new List<Room>(rooms);
    for (int i = 0; i < shuffledRooms.Count; i++) {
      int j = rand.Next(i, shuffledRooms.Count);
      var temp = shuffledRooms[i];
      shuffledRooms[i] = shuffledRooms[j];
      shuffledRooms[j] = temp;
    }

    for (int i = 0; i < shuffledRooms.Count; i++) {
      branches[i % branchCount].Add(shuffledRooms[i]);
    }

    return branches;
  }

  private void CreateStraightCorridor(Room roomA, Room roomB) {
    int x1 = roomA.CenterX;
    int y1 = roomA.CenterY;
    int x2 = roomB.CenterX;
    int y2 = roomB.CenterY;

    if (rand.Next(2) == 0) {
      CarveHorizontalTunnel(x1, x2, y1);
      CarveVerticalTunnel(y1, y2, x2);
    } else {
      CarveVerticalTunnel(y1, y2, x1);
      CarveHorizontalTunnel(x1, x2, y2);
    }
  }

  private void CreateSnakingCorridor(Room roomA, Room roomB) {
    int x = roomA.CenterX;
    int y = roomA.CenterY;

    int targetX = roomB.CenterX;
    int targetY = roomB.CenterY;

    while (x != targetX || y != targetY) {
      int dx = 0, dy = 0;

      if (x != targetX && (rand.Next(2) == 0 || y == targetY)) {
        dx = x < targetX ? 1 : -1;
      } else if (y != targetY) {
        dy = y < targetY ? 1 : -1;
      }

      // Randomly adjust x or y to make the path snake
      if (rand.NextDouble() < 0.3) {
        if (dx != 0 && y > 1 && y < chunkHeight - 1) {
          dy = rand.Next(2) == 0 ? 1 : -1;
        } else if (dy != 0 && x > 1 && x < chunkWidth - 1) {
          dx = rand.Next(2) == 0 ? 1 : -1;
        }
      }

      x += dx;
      y += dy;

      CarveCorridorTile(x, y);
    }
  }

  private void CreateForkingCorridor(Room roomA, Room roomB) {
    int x1 = roomA.CenterX;
    int y1 = roomA.CenterY;
    int x2 = roomB.CenterX;
    int y2 = roomB.CenterY;

    int midX = (x1 + x2) / 2 + rand.Next(-5, 6);
    int midY = (y1 + y2) / 2 + rand.Next(-5, 6);

    CarveHorizontalTunnel(x1, midX, y1);
    CarveVerticalTunnel(y1, midY, midX);
    CarveHorizontalTunnel(midX, x2, midY);
    CarveVerticalTunnel(midY, y2, x2);
  }

  private void CarveHorizontalTunnel(int x1, int x2, int y) {
    for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++) {
      CarveCorridorTile(x, y);
    }
  }

  private void CarveVerticalTunnel(int y1, int y2, int x) {
    for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++) {
      CarveCorridorTile(x, y);
    }
  }

  private void CarveCorridorTile(int x, int y) {
    // Carve a 4x4 area centered at (x, y) to ensure wide corridors
    for (int dx = -2; dx <= 2; dx++) {
      for (int dy = -2; dy <= 2; dy++) {
        int nx = x + dx;
        int ny = y + dy;
        if (nx >= 0 && nx < chunkWidth && ny >= 0 && ny < chunkHeight) {
          mapGrid[nx, ny] = 1; // Floor
          protectedCells[nx, ny] = true;
        }
      }
    }
  }

  private void SmoothMap() {
    int[,] newMap = (int[,])mapGrid.Clone();

    for (int x = 1; x < chunkWidth - 1; x++) {
      for (int y = 1; y < chunkHeight - 1; y++) {
        if (protectedCells[x, y]) {
          continue; // Skip protected cells
        }

        int neighborWallCount = GetNeighborWallCount(x, y);

        if (neighborWallCount > 4) {
          newMap[x, y] = 0; // Wall
        } else if (neighborWallCount < 4) {
          newMap[x, y] = 1; // Floor
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

        // Determine the biome based on temperature and descriptor
        BiomeType biomeType = GetBiomeTypeFromTemperature(temperature);

        // Get the floor and wall tiles for the biome
        var tiles = biomeTiles[biomeType];
        Vector2I floorTile = tiles.floorTile;
        Vector2I wallTile = tiles.wallTile;

        // Set the wall or floor tiles based on the biome
        if (mapGrid[x, y] == 0)  // Wall is 0
        {
          SetCell(0, new Vector2I(x, y), 0, wallTile);  // Set wall tile based on biome
        } else // Floor is 1
          {
          SetCell(0, new Vector2I(x, y), 0, floorTile);  // Set floor tile based on biome
        }
      }
    }
  }

}

public enum RoomShape {
  Rectangle,
  Circle,
  Octagon,
  Hexagon,
  Triangle
}

public enum BiomeType {
  Cold,
  Temperate,
  Hot,
  Jungle
}

public class Room {

  public int X, Y, Width, Height;
  public int Style; // 0: Rounded corners, 1: Rough edges, 2: Interior columns
  public RoomShape Shape; // New property for room shape
  public float Rotation; // Rotation angle in degrees

  public Room(int x, int y, int width, int height, int style, RoomShape shape, float rotation = 0) {
    X = x;
    Y = y;
    Width = width;
    Height = height;
    Style = style;
    Shape = shape;
    Rotation = rotation;
  }

  public int CenterX {
    get { return X + Width / 2; }
  }

  public int CenterY {
    get { return Y + Height / 2; }
  }

  public bool Intersects(Room other) {
    return (X <= other.X + other.Width && X + Width >= other.X &&
            Y <= other.Y + other.Height && Y + Height >= other.Y);
  }

}
