using Godot;
using System;

public partial class Player : CharacterBody2D {

  public const float Speed = 300.0f;

  public PackedScene BulletScene;  // Preload the Bullet scene here

  public override void _Ready() {
    // Load the bullet scene
    BulletScene = GD.Load<PackedScene>("res://bullet2.tscn");
  }

  public override void _PhysicsProcess(double delta) {
    Movement(delta);
    RotateTowardsMouse();

    // Check for shooting
    if (Input.IsActionJustPressed("shoot")) {
      Shoot();
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

    // Instance the bullet
    var bullet = (Bullet2)BulletScene.Instantiate();

    // Set bullet's position to the player's position
    bullet.Position = GlobalPosition;

    // Set the direction of the bullet towards the mouse
    Vector2 mousePosition = GetGlobalMousePosition();
    Vector2 direction = mousePosition - GlobalPosition;
    bullet.Initialize(direction);  // Pass direction to the bullet

    // Add the bullet to the scene
    GetParent().AddChild(bullet);
  }

}
