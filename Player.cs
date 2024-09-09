using Godot;
using System;

public partial class Player : CharacterBody2D {

  public const float Speed = 300.0f;

  public override void _PhysicsProcess(double delta) {
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

}
