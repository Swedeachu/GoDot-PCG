using Godot;
using System;
using static Item;

public partial class Portal : RigidBody2D {

  private bool touched = false;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    GravityScale = 0;
    FreezeMode = RigidBody2D.FreezeModeEnum.Kinematic;
    ContactMonitor = true;
    MaxContactsReported = 5;
    BodyEntered += (Node body) => OnBodyEntered(body);
    ShakeAndBake.iShouldntExistList.Add(this);
    // spawnTransitionThing();
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
  }

  private void OnBodyEntered(Node body) {
    if (body is Player player) {
      if (!touched) {
        spawnTransitionThing(player);
      }
      // Destroy our self
      touched = true;
      QueueFree();
      ShakeAndBake.Instance.LevelUp();
    }
  }

  private void spawnTransitionThing(Player player) {
    GD.Print("Creating thing");

    // Load the scene and instantiate the item
    var scene = GD.Load<PackedScene>("res://capture.tscn");
    var item = (Capture)scene.Instantiate();
    GetTree().Root.CallDeferred("add_child", item);

    // Get the screen size (viewport size) using GetViewportRect().Size
    var viewportSize = GetViewport().GetVisibleRect().Size;

    // Get the Camera2D's zoom value (how zoomed in/out the camera is)
    var cameraZoom = player.GetNode<Camera2D>("Camera2D").Zoom;

    // Adjust the viewport size to account for the camera's zoom level
    Vector2 adjustedViewportSize = viewportSize / cameraZoom;

    // Scale the item to match the adjusted viewport size
    item.Scale = new Vector2(
        adjustedViewportSize.X / viewportSize.X,
        adjustedViewportSize.Y / viewportSize.Y
    );

    // Center the item by adjusting the global position
    var offset = adjustedViewportSize / 2;
    item.GlobalPosition = player.GlobalPosition - offset;
    item.offset = offset;
    item.offset.Y -= 20;
    item.player = player;

    // Set visibility layer or other properties
    item.VisibilityLayer = 12;
  }


}
