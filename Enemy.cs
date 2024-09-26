using Godot;
using System;
using static Godot.OpenXRInterface;

public partial class Enemy : CharacterBody2D {

  public enum EnemyType {
    Trivial,
    Easy,
    Medium,
    Hard,
    Boss
  }

  private Node2D target;

  // Speed and acceleration variables
  private float speed = 200f;
  private float acceleration = 7f;

  // NavigationAgent2D to handle pathfinding
  private NavigationAgent2D navigationAgent;

  private ProgressBar healthBar;

  private int maxHealth;
  private int health;
  private float shootRange = 300f;
  private float shootCooldown = 2.0f;
  private float timeSinceLastShot = 0f;

  // Enemy type
  private EnemyType enemyType;

  public PackedScene BulletScene;
  public PackedScene itemScene;

  private RandomNumberGenerator rand = new RandomNumberGenerator();

  public override void _Ready() {
    // Get the NavigationAgent2D from the scene tree
    navigationAgent = GetNode<NavigationAgent2D>("Navigation/NavigationAgent2D");
    var timer = GetNode<Timer>("Navigation/Timer");
    timer.Timeout += OnTimerTimeout;

    // Get the ProgressBar node
    healthBar = GetNode<ProgressBar>("ProgressBar");

    // Initialize the health bar
    health = maxHealth;
    healthBar.MaxValue = maxHealth;
    healthBar.Value = health;

    // Load the bullet scene
    BulletScene = GD.Load<PackedScene>("res://bullet2.tscn");
    itemScene = GD.Load<PackedScene>("res://item.tscn");

    FindPlayer();
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 direction = Vector2.Zero;

    // Get the next position along the path and calculate the direction
    direction = (navigationAgent.GetNextPathPosition() - GlobalPosition).Normalized();

    // Apply velocity smoothing using linear interpolation (lerp)
    Velocity = Velocity.Lerp(direction * speed, acceleration * (float)delta);

    // Move the enemy using MoveAndSlide()
    MoveAndSlide();

    // Shooting logic
    timeSinceLastShot += (float)delta;
    if (target != null && IsWithinRange(target.GlobalPosition, shootRange) && timeSinceLastShot >= shootCooldown) {
      ShootAtTarget();
      timeSinceLastShot = 0f;
    }
  }

  private void OnTimerTimeout() {
    if (target != null) {
      // Set the target position for the NavigationAgent2D to follow
      navigationAgent.TargetPosition = target.GlobalPosition;
    } else {
      FindPlayer();
    }
  }

  private void FindPlayer() {
    // Find the "World" node first
    var worldNode = GetTree().Root.GetNode("World");

    if (worldNode != null) {
      // Search for the Player node inside the World node
      var playerNode = worldNode.GetNode("Player") as Node2D;

      if (playerNode != null) {
        target = playerNode;
        // GD.Print("Player found: ", target.Name);
      } else {
        // GD.PrintErr("Player node not found!");
      }
    } else {
      GD.PrintErr("World node not found!");
    }
  }

  public void Damage(int amount) {
    health -= amount;

    // Clamp the health to be at least 0
    health = Mathf.Clamp(health, 0, maxHealth);

    // Update the progress bar to reflect the current health
    healthBar.Value = health;

    // Check if the enemy's health is 0 or below
    if (health <= 0) {
      QueueFree();
      DropItem();
      TelemetryManager.Instance.AddKill(enemyType);
      // every 10 kills spawn the boss
      if (TelemetryManager.Instance.GetTotalKills() % 10 == 0) {
        PCG.Instance.SpawnBoss();
      }
    }
  }

  // Random chance of dropping an item based on enemy difficulty level
  private void DropItem() {
    // Determine the drop chance based on enemy type
    float dropChance = 0f;

    switch (enemyType) {
      case EnemyType.Trivial:
      case EnemyType.Easy:
      case EnemyType.Medium:
      dropChance = 0.1f; // 1/10 chance
      break;

      case EnemyType.Hard:
      case EnemyType.Boss:
      dropChance = 0.33f; // 1/3 chance
      break;
    }

    // Generate a random float between 0 and 1, and drop an item if it's less than the dropChance
    rand.Randomize(); // Ensure randomness
    if (rand.Randf() <= dropChance) {
      // Spawn item and set position
      var item = (Item)itemScene.Instantiate();
      GetTree().Root.CallDeferred("add_child", item);
      item.GlobalPosition = GlobalPosition;

      // Randomize item type
      Item.ItemType type = (Item.ItemType)rand.RandiRange(0, Enum.GetNames(typeof(Item.ItemType)).Length - 1);
      item.SetType(type);
      GD.Print("Item dropped!");
    } else {
      GD.Print("No item dropped.");
    }
  }

  // Check if player is within range to shoot
  private bool IsWithinRange(Vector2 playerPosition, float range) {
    return GlobalPosition.DistanceTo(playerPosition) <= range;
  }

  // Modify shooting logic based on enemy type
  private void ShootAtTarget() {
    if (BulletScene == null) {
      GD.PrintErr("Bullet scene not loaded");
      return;
    }

    Vector2 direction = target.GlobalPosition - GlobalPosition;

    switch (enemyType) {
      case EnemyType.Trivial:
      // Single shot
      ShootBullet(direction);
      break;

      case EnemyType.Easy:
      // Double shot: 2 bullets slightly angled
      ShootBullet(direction.Rotated(Mathf.DegToRad(-10)));  // Left bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(10)));   // Right bullet
      break;

      case EnemyType.Medium:
      // Triple shot: 3 bullets slightly angled
      ShootBullet(direction.Rotated(Mathf.DegToRad(-15)));  // Left bullet
      ShootBullet(direction);  // Center bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(15)));   // Right bullet
      break;

      case EnemyType.Hard:
      // Quad shot: 4 bullets in different diagonal directions
      ShootBullet(direction.Rotated(Mathf.DegToRad(-30)));  // Far left bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(-10)));  // Left bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(10)));   // Right bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(30)));   // Far right bullet
      break;

      case EnemyType.Boss:
      // Diagonal quad shot: 4 bullets in diagonal directions
      ShootBullet(new Vector2(1, 1).Normalized());   // Bottom right
      ShootBullet(new Vector2(-1, 1).Normalized());  // Bottom left
      ShootBullet(new Vector2(1, -1).Normalized());  // Top right
      ShootBullet(new Vector2(-1, -1).Normalized()); // Top left
      break;
    }
  }

  // Function to spawn a bullet in a given direction
  private void ShootBullet(Vector2 direction) {
    var bullet = (Bullet2)BulletScene.Instantiate();
    bullet.Position = GlobalPosition;
    bullet.Initialize(direction, true); 
    GetParent().AddChild(bullet);
  }

  // Set different attributes based on the enemy type
  public void SetEnemyType(EnemyType type) {
    var textureRect = GetNode<TextureRect>("TextureRect"); 
    enemyType = type;
    // Modulate doesen't work because I think we need to do it on the sprite
    switch (enemyType) {
      case EnemyType.Trivial:
      speed = 100f;
      maxHealth = 5;
      shootRange = 150f;
      shootCooldown = 3.0f;
      textureRect.SelfModulate = new Color(1, 1, 1); // White (no modulation basically)
      break;

      case EnemyType.Easy:
      speed = 110f;
      maxHealth = 10;
      shootRange = 200f;
      shootCooldown = 2.5f;
      textureRect.SelfModulate = new Color(0, 1, 0); // Green
      break;

      case EnemyType.Medium:
      speed = 120f;
      maxHealth = 5;
      shootRange = 250f;
      shootCooldown = 2.0f;
      textureRect.SelfModulate = new Color(1, 1, 0); // Yellow
      break;

      case EnemyType.Hard:
      speed = 130f;
      maxHealth = 15;
      shootRange = 300f;
      shootCooldown = 1.5f;
      textureRect.SelfModulate = new Color(1, 0, 0); // Red
      break;

      case EnemyType.Boss:
      speed = 100f;
      maxHealth = 40;
      shootRange = 400f;
      shootCooldown = 1.0f;
      textureRect.SelfModulate = new Color(1, 0.5f, 1); // Pink
      break;
    }
    health = maxHealth;
  }

}
