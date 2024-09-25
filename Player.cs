using Godot;
using System;

public partial class Player : CharacterBody2D {

  public float Speed = 300.0f;

  private int maxHealth = 10;
  private int health = 10;

  private float originalSpeed = 300.0f;  // To store the player's original speed
  private bool tripleShotActive = false;  // Track if triple shot is active

  public PackedScene BulletScene;  

  private ProgressBar healthBar;

  public override void _Ready() {
    // Load the bullet scene
    BulletScene = GD.Load<PackedScene>("res://bullet2.tscn");

    // Get the ProgressBar node
    healthBar = GetNode<ProgressBar>("ProgressBar");

    // Initialize the health bar
    healthBar.MaxValue = maxHealth;
    healthBar.Value = health;
  }

  public override void _PhysicsProcess(double delta) {
    Movement(delta);
    RotateTowardsMouse();

    // Check for shooting
    if (Input.IsActionJustPressed("shoot")) {
      Shoot();
    }

    if (Input.IsActionJustPressed("right_click")) {
      // Get the mouse position on the screen
      Vector2 mouseScreenPos = GetViewport().GetMousePosition();

      // Get the screen transform and canvas transform and invert them
      Transform2D screenTransform = GetViewport().GetScreenTransform();
      Transform2D canvasTransform = GetCanvasTransform();

      // Calculate the world position by applying the inverse of the transforms to the screen position
      Vector2 mouseWorldPos = (screenTransform * canvasTransform).AffineInverse() * mouseScreenPos;

      // Set the GlobalPosition of the node to the mouse's world position
      GlobalPosition = mouseWorldPos;
    }
  }

  // Handles player movement
  private void Movement(double delta) {
    Vector2 velocity = Velocity;

    // Get the input direction and handle the movement.
    Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

    // If there's input, move the player
    if (direction != Vector2.Zero) {
      velocity = direction * Speed;  // Apply movement in both X and Y axes
    } else {
      velocity = Vector2.Zero;  // Stop movement when there's no input
    }

    Velocity = velocity;
    MoveAndSlide();
  }

  // Rotates the player to face the mouse cursor
  private void RotateTowardsMouse() {
    // Get the player's position in the global coordinate space
    Vector2 playerPosition = GlobalPosition;

    // Get the mouse's position in the global coordinate space (relative to the world, not the window)
    Vector2 mousePosition = GetGlobalMousePosition();

    // Calculate the angle between the player's position and the mouse cursor
    float angle = (mousePosition - playerPosition).Angle();

    // Apply the angle directly to the player's rotation (no need for inversion, just use this)
    Rotation = angle;
  }

  // Shoots a bullet towards the mouse cursor
  private void Shoot() {
    if (BulletScene == null) {
      GD.PrintErr("Bullet scene not loaded");
      return;
    }

    Vector2 mousePosition = GetGlobalMousePosition();
    Vector2 direction = mousePosition - GlobalPosition;

    if (tripleShotActive) {
      // Triple shot: spawn 3 bullets with slight angle variations
      ShootBullet(direction.Rotated(Mathf.DegToRad(-15)));  // Left bullet
      ShootBullet(direction);  // Center bullet
      ShootBullet(direction.Rotated(Mathf.DegToRad(15)));  // Right bullet
    } else {
      // Single shot
      ShootBullet(direction);
    }
  }

  // Function to spawn a bullet in a given direction
  private void ShootBullet(Vector2 direction) {
    var bullet = (Bullet2)BulletScene.Instantiate();
    bullet.Position = GlobalPosition;
    bullet.Initialize(direction);  // Pass direction to the bullet
    GetParent().AddChild(bullet);
  }

  // Apply the effect of an item when collected
  public void ApplyItemEffect(Item.ItemType itemType) {
    switch (itemType) {
      case Item.ItemType.Heal:
      SetHealth(maxHealth);
      break;
      case Item.ItemType.Speed:
      ActivateSpeedBoost();
      break;
      case Item.ItemType.Damage:
      ActivateTripleShot();
      break;
    }
  }

  // Activate speed boost temporarily
  private async void ActivateSpeedBoost() {
    GD.Print("Speed boost activated!");
    Speed = originalSpeed * 2;  // Double the player's speed

    await ToSignal(GetTree().CreateTimer(5.0f), "timeout");  // Wait for 5 seconds
    Speed = originalSpeed;  // Revert to original speed
    GD.Print("Speed boost ended.");
  }

  // Activate triple shot temporarily
  private async void ActivateTripleShot() {
    GD.Print("Triple shot activated!");
    tripleShotActive = true;

    await ToSignal(GetTree().CreateTimer(5.0f), "timeout");  // Wait for 5 seconds
    tripleShotActive = false;  // Revert to normal single shot
    GD.Print("Triple shot ended.");
  }

  public void Damage(int amount) {
    health -= amount;

    // Clamp the health to be at least 0
    health = Mathf.Clamp(health, 0, maxHealth);

    // Update the progress bar to reflect the current health
    healthBar.Value = health;

    // Check if the enemy's health is 0 or below
    if (health <= 0) {
      // Handle death...
    }
  }

  public void SetHealth(int value) {
    this.health = value;
    healthBar.Value = health;
  }

}
