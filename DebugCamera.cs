using Godot;
using System;

public partial class DebugCamera : Camera2D {
  // Movement speed for the camera
  [Export]
  public float moveSpeed = 300f;

  // Zoom limits and sensitivity
  [Export]
  public float zoomSpeed = 0.05f;
  public float minZoom = 0.5f;
  public float maxZoom = 2.0f;

  public override void _Ready() {
    // Enable smoothing for smoother movement, if you want
    // SmoothingEnabled = true;
    // SmoothingSpeed = 10f;
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
    // Zoom in on scroll up
    if (Input.IsActionJustPressed("ui_zoom_in")) {
      Zoom += new Vector2(zoomSpeed, zoomSpeed);
    }

    // Zoom out on scroll down
    if (Input.IsActionJustPressed("ui_zoom_out")) {
      Zoom -= new Vector2(zoomSpeed, zoomSpeed);
    }

    // Clamp zoom levels
    Zoom = new Vector2(Mathf.Clamp(Zoom.X, minZoom, maxZoom), Mathf.Clamp(Zoom.Y, minZoom, maxZoom));
  }
}
