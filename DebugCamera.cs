using Godot;
using System;

public partial class DebugCamera : Camera2D {
  // Movement speed for the camera
  [Export]
  public float moveSpeed = 300f;

  // Zoom limits and sensitivity
  [Export]
  public float zoomSpeed = 0.05f;
  public float minZoom = 1.5f;
  public float maxZoom = 0.1f;

  public override void _Ready() {
    Position = new Vector2(400, 400);
  }

  public override void _Process(double delta) {
    HandleMovement((float)delta);
    HandleZoom();
  }

  private void HandleMovement(float delta) {
    Vector2 movement = Vector2.Zero;

    // WASD Movement
    if (Input.IsActionPressed("move_up")) {
      movement.Y -= 1;
    }
    if (Input.IsActionPressed("move_down")) {
      movement.Y += 1;
    }
    if (Input.IsActionPressed("move_left")) {
      movement.X -= 1;
    }
    if (Input.IsActionPressed("move_right")) {
      movement.X += 1;
    }

    // Normalize the movement vector so diagonal movement is not faster
    movement = movement.Normalized();

    // Apply movement based on move speed and delta time
    Position += movement * moveSpeed * delta;
  }

  private void HandleZoom() {
    // Zoom in (make the zoom value smaller)
    if (Input.IsActionJustPressed("ui_zoom_in")) {
      Zoom += new Vector2(zoomSpeed, zoomSpeed);
    }

    // Zoom out (make the zoom value larger)
    if (Input.IsActionJustPressed("ui_zoom_out")) {
      Zoom -= new Vector2(zoomSpeed, zoomSpeed);
    }

    // Clamp zoom levels to avoid zooming too far in or out
    float clampedZoom = Mathf.Clamp(Zoom.X, maxZoom, minZoom);  // Ensure zoom stays between minZoom and maxZoom
    Zoom = new Vector2(clampedZoom, clampedZoom);

    // Map hotkey to zoom out to the max instantly (for map view)
    if (Input.IsActionJustPressed("map")) {
      if (Zoom.X <= maxZoom) {
        Zoom = new Vector2(minZoom, minZoom);  
      } else {
        Zoom = new Vector2(maxZoom, maxZoom);  
      }
    }
  }

}
