using Godot;
using System;

public partial class Bullet2 : RigidBody2D {

  public float Speed = 600f;  // Speed of the bullet
  private Vector2 _direction;

  public override void _Ready() {
    // Disable gravity
    GravityScale = 0;  // Disable gravity for top-down movement

    // Physics stuff we need for movement and collision
    FreezeMode = RigidBody2D.FreezeModeEnum.Kinematic;
    ContactMonitor = true;
    MaxContactsReported = 100; // not sure if making this super big beyond like a 1 or a 5 matters but 100 to gurantee the callback

    BodyEntered += (Node body) => collide(body); // weird af lambda for body entered collision callback
  }

  public void Initialize(Vector2 direction) {
    _direction = direction.Normalized();  // Normalize direction to ensure consistent speed
    LinearVelocity = _direction * Speed;  // Apply movement to the bullet using LinearVelocity
  }

  public override void _PhysicsProcess(double delta) {
    // Remove manual position updating; let physics engine handle it
  }

  private void collide(Node body) {
    GD.Print("Bullet2D collided with " + body.Name);
    QueueFree();
  }

}
