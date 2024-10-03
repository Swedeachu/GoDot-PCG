using Godot;
using System;
using static Item;

public partial class Portal : RigidBody2D {
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    GravityScale = 0;
    FreezeMode = RigidBody2D.FreezeModeEnum.Kinematic;
    ContactMonitor = true;
    MaxContactsReported = 5;
    BodyEntered += (Node body) => OnBodyEntered(body);
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
  }

  private void OnBodyEntered(Node body) {
    if (body is Player player) {
      // Destroy our self
      QueueFree();
      ShakeAndBake.Instance.LevelUp();
    }
  }

}
