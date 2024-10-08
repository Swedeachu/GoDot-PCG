using Godot;
using System.Collections.Generic;
using static Enemy;
using static Item;

// this has evolved into the main game manager
public partial class ShakeAndBake : NavigationRegion2D {

  private PCG pcg;
  private PackedScene pcgScene;
  private PackedScene portalScene;

  public int level = 0;

  public static List<Node2D> iShouldntExistList = new List<Node2D>();

  public static ShakeAndBake Instance { get; private set; }

  public void Restart(bool restartLevel = true) {
    if (restartLevel) level = 0;
    Enemy.killsThisRound = 0;
    Enemy.neededKills = 0;
    var player = KillActorsAndGetPlayer();
    ReplaceTileMap();
    if (player != null) {
      player.Respawn();
      if (restartLevel) player.allowDash = false;
    }
  }

  public override void _Ready() {
    // Load the PCG scene once and reuse it when needed
    pcgScene = (PackedScene)ResourceLoader.Load("res://tile_map.tscn");
    Instance = this;
    portalScene = GD.Load<PackedScene>("res://portal.tscn");
    GenerateNewTileMap();
  }

  public override void _Process(double delta) {
    // good lord this is bad
    if (Input.IsActionJustPressed("1")) {
      level = 0;
      Restart(false);
    }
    if (Input.IsActionJustPressed("2")) {
      level = 1;
      Restart(false);
    }
    if (Input.IsActionJustPressed("3")) {
      level = 2;
      Restart(false);
    }
    if (Input.IsActionJustPressed("4")) {
      level = 3;
      Restart(false);
    }
    if (Input.IsActionJustPressed("5")) {
      level = 4;
      Restart(false);
    }

    // Detect if the "R" key is pressed
    if (Input.IsActionJustPressed("reset")) { // we need to go through and kill all nodes named Enemy
      Restart();
      return;
    }

    // P key
    if (Input.IsActionJustPressed("portal")) {
      Vector2 mouseScreenPos = GetViewport().GetMousePosition();

      // Get the screen transform and canvas transform and invert them
      Transform2D screenTransform = GetViewport().GetScreenTransform();
      Transform2D canvasTransform = GetCanvasTransform();

      // Calculate the world position by applying the inverse of the transforms to the screen position
      Vector2 mouseWorldPos = (screenTransform * canvasTransform).AffineInverse() * mouseScreenPos;
      MakePortalToNewLevel(mouseWorldPos);
    }
  }

  // clears all actors that need to be cleaned up such as enemies and items, and finds the player while iterating
  private Player KillActorsAndGetPlayer() {
    Node root = GetTree().Root;
    Player plr = null;

    /*
    foreach (Node child in root.GetChildren()) {
      GD.Print("hit: " + child.Name);
      if (child is Enemy || child is Item || child is Portal || child.Name == "marker") {
        child.QueueFree();
      }
    }
    */

    // this is great, assuming we manage this list properly and add/remove to it during gameplay
    foreach (var child in iShouldntExistList) {
      child.QueueFree();
      GD.Print("deleting from list: " + child.Name);
    }
    iShouldntExistList.Clear();

    Node world = root.GetNode("World");
    foreach (Node child in world.GetChildren()) {
      if (child is Player player) {
        player.SetHealth(10); // back to max health
        plr = player;
      }
    }

    return plr;
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

    // Generate the map with the descriptor
    pcg.Generate(GetMapDescriptor(), GetEnemyWaveDescriptor(), GetItemWaveDescriptor());

    BakeNavigationPolygon();
  }

  private MapGenerationDescriptor GetMapDescriptor() {
    // Declare variables for room shape weights and biome weights
    Dictionary<RoomShape, float> roomShapeWeights = new Dictionary<RoomShape, float>();
    Dictionary<BiomeType, float> biomeWeights = new Dictionary<BiomeType, float>();

    // Initialize biome weights dictionary
    biomeWeights = new Dictionary<BiomeType, float>();

    // Add to biome weights based on the level
    switch (level) {
      case 0:
      roomShapeWeights = new Dictionary<RoomShape, float> {
        { RoomShape.Circle, 1 },
      };
      biomeWeights.Add(BiomeType.Temperate, 0.45f); // cave
      pcg.roomCount = 10;
      break;

      case 1:
      roomShapeWeights = new Dictionary<RoomShape, float> {
        { RoomShape.Hexagon, 1 },
      };
      biomeWeights.Add(BiomeType.Jungle, 0.25f); // forest basically
      pcg.roomCount = 15;
      break;

      case 2:
      roomShapeWeights = new Dictionary<RoomShape, float> {
        { RoomShape.Octagon, 0.50f },
        { RoomShape.Circle, 0.50f },
      };
      biomeWeights.Add(BiomeType.Hot, 0.45f); // desert/ocean
      pcg.roomCount = 20;
      break;

      case 3:
      roomShapeWeights = new Dictionary<RoomShape, float> {
        { RoomShape.Triangle, 4 },
      };
      biomeWeights.Add(BiomeType.Cold, 0.25f); // ice
      pcg.roomCount = 30;
      break;

      case 4:
      biomeWeights.Add(BiomeType.Jungle, 0.50f); // Jungle
      biomeWeights.Add(BiomeType.Temperate, 0.25f); // cave
      biomeWeights.Add(BiomeType.Hot, 0.25f); // Desert

      roomShapeWeights = new Dictionary<RoomShape, float> {
        { RoomShape.Rectangle, 0.25f },
        { RoomShape.Circle, 0.25f },
        { RoomShape.Hexagon, 0.25f },
        { RoomShape.Octagon, 0.15f },
        { RoomShape.Triangle, 0.10f }
      };
      pcg.roomCount = 40;
      break;
    }

    // Create and return the descriptor with the configured weights
    return new MapGenerationDescriptor {
      RoomShapeWeights = roomShapeWeights,
      BiomeWeights = biomeWeights
    };
  }

  public EnemyWaveDescriptor GetEnemyWaveDescriptor() {
    // Create a new instance of EnemyWaveDescriptor
    var enemyWaveDescriptor = new EnemyWaveDescriptor();

    // Add enemies based on the level
    switch (level) {
      case 0:
      // Add trivial enemies for the first level
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Trivial, 10);
      break;

      case 1:
      // Add more challenging enemies
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Trivial, 5);
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Easy, 5);
      break;

      case 2:
      // Add more medium and hard enemies
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Easy, 5);
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Medium, 5);
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Hard, 3);
      break;

      case 3:
      // Add harder enemies 
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Medium, 5);
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Hard, 7);
      break;

      case 4:
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Boss, 1);
      break;

      default:
      // Default enemy wave setup
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Trivial, 3);
      enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Easy, 2);
      break;
    }

    return enemyWaveDescriptor;
  }

  public ItemWaveDescriptor GetItemWaveDescriptor() {
    // Create a new instance of ItemWaveDescriptor
    var itemWaveDescriptor = new ItemWaveDescriptor();

    // Add items based on the level
    switch (level) {
      case 0:
      // none for level 0 until the end
      // itemWaveDescriptor.ItemCounts.Add(ItemType.Heal, 3);
      // itemWaveDescriptor.ItemCounts.Add(ItemType.Speed, 3);
      break;

      case 1:
      itemWaveDescriptor.ItemCounts.Add(ItemType.Heal, 3);
      itemWaveDescriptor.ItemCounts.Add(ItemType.Speed, 4);
      itemWaveDescriptor.ItemCounts.Add(ItemType.Damage, 2);
      break;

      case 2:
      itemWaveDescriptor.ItemCounts.Add(ItemType.Speed, 5);
      itemWaveDescriptor.ItemCounts.Add(ItemType.Damage, 5);
      break;

      case 3:
      itemWaveDescriptor.ItemCounts.Add(ItemType.Heal, 2);
      itemWaveDescriptor.ItemCounts.Add(ItemType.Speed, 3);
      itemWaveDescriptor.ItemCounts.Add(ItemType.Damage, 5);
      break;

      // Add more levels as needed
      default:
      itemWaveDescriptor.ItemCounts.Add(ItemType.Heal, 1);
      itemWaveDescriptor.ItemCounts.Add(ItemType.Speed, 1);
      break;
    }

    return itemWaveDescriptor;
  }

  public void LevelUp() {
    level++;
    Restart(false);
  }

  public void HandleKill(Enemy enemy) {
    TelemetryManager.Instance.AddKill(enemy.enemyType);
    killsThisRound++;

    if (level == 0 && killsThisRound == 5) {
      GD.Print("dash unlocked");
      DashUnlocked();
    }

    if (enemy.enemyType == EnemyType.Boss) {
      GD.Print("crazy mode");
      UltraTime();
    }

    // if we hit the kill count spawn a teleported here to that can warp the player to the next level
    if (killsThisRound >= neededKills) {
      GD.Print("kill count reached");
      MakePortalToNewLevel(enemy.GlobalPosition);
    }
  }

  private void UltraTime() {
    Node root = GetTree().Root;

    Node world = root.GetNode("World");
    foreach (Node child in world.GetChildren()) {
      if (child is Player player) {
        player.SetMaxHealth(100);
        player.SetHealth(100);
        player.Scale = new Vector2(3, 3);
        player.Speed = 600;
        player.tripleShotActive = true;
        var textureRect = player.GetNode<TextureRect>("TextureRect");
        textureRect.SelfModulate = new Color(1, 0.5f, 1); // Pink
        break;
      }
    }

    // now spawn a shit ton of easy guys to mow down
    var enemyWaveDescriptor = new EnemyWaveDescriptor();
    enemyWaveDescriptor.EnemyCounts.Add(EnemyType.Trivial, 200);
    pcg.enemyWaveDescriptor = enemyWaveDescriptor;
    pcg.SpawnEnemyWave();
  }

  private void DashUnlocked() {
    Node root = GetTree().Root;

    Node world = root.GetNode("World");
    foreach (Node child in world.GetChildren()) {
      if (child is Player player) {
        player.allowDash = true;
        ShowFloatingText("Dash Unlocked! (space)", new Color(1, 0, 0), player.GlobalPosition);
        break;
      }
    }
  }

  private void ShowFloatingText(string text, Color color, Vector2 position) {
    GD.Print("showing text: " + text);
    PackedScene floatingTextScene = (PackedScene)ResourceLoader.Load("res://textPrefab.tscn");
    Node2D floatingText = (Node2D)floatingTextScene.Instantiate();
    GetTree().Root.AddChild(floatingText); // or add it to a specific part of your scene
    CustomText textNode = (CustomText)floatingText.GetNode("TextLabel");
    textNode.Text = text;
    textNode.Modulate = color;
    floatingText.VisibilityLayer = 10;
    floatingText.GlobalPosition = position;
    floatingText.Scale = new Vector2(3, 3);
  }

  public void MakePortalToNewLevel(Vector2 position) {
    var portal = (Portal)portalScene.Instantiate();
    GetTree().Root.CallDeferred("add_child", portal);
    portal.GlobalPosition = position;
  }

}
