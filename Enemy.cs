using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D {

  public enum EnemyType {
    Trivial,
    Easy,
    Medium,
    Hard,
    Boss
  }

  public static int killsThisRound = 0;
  public static int neededKills = 0;

  public class EnemyWaveDescriptor {
    public Dictionary<EnemyType, int> EnemyCounts { get; set; }

    public EnemyWaveDescriptor() {
      EnemyCounts = new Dictionary<EnemyType, int>();
    }
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
  public EnemyType enemyType;

  public PackedScene BulletScene;
  public PackedScene itemScene;

  private RandomNumberGenerator rand = new RandomNumberGenerator();

  // Dash-related variables for Boss
  private bool isDashing = false;
  private float dashSpeed = 400f; // Increased speed during dash
  private float dashDuration = 0.5f; // Duration of dash in seconds
  private float dashCooldown = 5f; // Cooldown between dashes in seconds
  private float timeSinceLastDash = 0f; // Timer to track cooldown

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
    Enemy.neededKills++; // increase needed kill count

    FindPlayer();
  }

  public override void _PhysicsProcess(double delta) {
    // Update dash timers
    if (enemyType == EnemyType.Boss) {
      timeSinceLastDash += (float)delta;

      if (isDashing) {
        // Continue dashing
        Velocity = Velocity.Normalized() * dashSpeed;
        dashDuration -= (float)delta;
        if (dashDuration <= 0f) {
          isDashing = false;
          dashDuration = 0.5f; // Reset dash duration
        }
      } else {
        // Regular movement
        Vector2 direction = Vector2.Zero;

        // Get the next position along the path and calculate the direction
        Vector2 nextPathPos = navigationAgent.GetNextPathPosition();
        if (nextPathPos != Vector2.Zero) {
          direction = (nextPathPos - GlobalPosition).Normalized();
        }

        // Apply velocity smoothing using linear interpolation (lerp)
        Velocity = Velocity.Lerp(direction * speed, acceleration * (float)delta);

        // Decide randomly to dash
        // For example, 20% chance to dash every second
        if (timeSinceLastDash >= dashCooldown) {
          float dashChance = 0.2f; // 20% chance
          rand.Randomize();
          if (rand.Randf() <= dashChance) {
            InitiateDash(direction);
          }
        }
      }
    } else {
      // Non-Boss enemy movement logic
      Vector2 direction = Vector2.Zero;

      // Get the next position along the path and calculate the direction
      Vector2 nextPathPos = navigationAgent.GetNextPathPosition();
      if (nextPathPos != Vector2.Zero) {
        direction = (nextPathPos - GlobalPosition).Normalized();
      }

      // Apply velocity smoothing using linear interpolation (lerp)
      Velocity = Velocity.Lerp(direction * speed, acceleration * (float)delta);
    }

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
      DropItem();
      ShakeAndBake.Instance.HandleKill(this);
      QueueFree();
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
      // Double shot too but it's faster
      ShootBullet(direction.Rotated(Mathf.DegToRad(-15)));  // Left bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(15)));   // Right bullet
      break;

      case EnemyType.Hard:
      QuadShot(direction);
      break;

      case EnemyType.Boss:
      BossAttack(direction);
      break;
    }
  }

  private void QuadShot(Vector2 direction) {
    // Quad shot: 4 bullets in different diagonal directions
    ShootBullet(direction.Rotated(Mathf.DegToRad(-30)));  // Far left bullet
    ShootBullet(direction.Rotated(Mathf.DegToRad(-10)));  // Left bullet
    ShootBullet(direction.Rotated(Mathf.DegToRad(10)));   // Right bullet
    ShootBullet(direction.Rotated(Mathf.DegToRad(30)));   // Far right bullet
  }

  private void BossAttack(Vector2 direction) {
    if (health > maxHealth / 2) {
      ShootBullet(direction);
      shootCooldown = 0.3f;
    } else {
      QuadShot(direction);
      var bullet2 = ShootBullet(direction);
      shootCooldown = 1f;
      bullet2.Modulate = new Color(1, 1, 0); // yellow
    }
  }

  /// <summary>
  /// Initiates a dash towards the specified direction.
  /// </summary>
  /// <param name="direction">The direction vector to dash towards.</param>
  private void InitiateDash(Vector2 direction) {
    isDashing = true;
    Velocity = direction.Normalized() * dashSpeed;
    timeSinceLastDash = 0f;
    GD.Print("Boss is dashing!");
  }

  /// <summary>
  /// Function to spawn a bullet in a given direction.
  /// </summary>
  /// <param name="direction">The direction to shoot the bullet towards.</param>
  /// <returns>The instantiated Bullet2 object.</returns>
  private Bullet2 ShootBullet(Vector2 direction) {
    var bullet = (Bullet2)BulletScene.Instantiate();
    bullet.Position = GlobalPosition;
    bullet.Initialize(direction, true);
    bullet.Modulate = new Color(1, 0, 0);
    GetParent().AddChild(bullet);
    return bullet;
  }

  /// <summary>
  /// Sets different attributes based on the enemy type.
  /// </summary>
  /// <param name="type">The type of enemy to set.</param>
  public void SetEnemyType(EnemyType type) {
    var textureRect = GetNode<TextureRect>("TextureRect");
    enemyType = type;
    // Modulate doesn't work because I think we need to do it on the sprite
    switch (enemyType) {
      case EnemyType.Trivial:
      speed = 250f;
      maxHealth = 5;
      shootRange = 450f;
      shootCooldown = 3.0f;
      textureRect.SelfModulate = new Color(0, 1, 0); // Green
      break;

      case EnemyType.Easy:
      speed = 350f;
      maxHealth = 10;
      shootRange = 600f;
      shootCooldown = 2.5f;
      textureRect.SelfModulate = new Color(0, 0, 1); // Blue
      break;

      case EnemyType.Medium:
      speed = 220f;
      maxHealth = 15;
      shootRange = 300f;
      shootCooldown = 1.5f;
      textureRect.SelfModulate = new Color(1, 1, 0); // Yellow
      break;

      case EnemyType.Hard:
      speed = 220f;
      maxHealth = 20;
      shootRange = 250f;
      shootCooldown = 1f;
      textureRect.SelfModulate = new Color(1, 0, 0); // Red
      break;

      case EnemyType.Boss:
      speed = 200f;
      maxHealth = 150;
      shootRange = 400f;
      shootCooldown = 0.3f;
      Scale = new Vector2(3, 3);
      textureRect.SelfModulate = new Color(1, 0.5f, 1); // Pink
      break;
    }
    health = maxHealth;
  }

}
