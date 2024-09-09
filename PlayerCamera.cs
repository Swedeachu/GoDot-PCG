using Godot;
using System;

public partial class PlayerCamera : Camera2D {
  // Movement speed for the camera
  [Export]
  public float moveSpeed = 300f;

  // Zoom limits and sensitivity
  [Export]
  public float zoomSpeed = 0.05f;
  public float minZoom = 1.5f;
  public float maxZoom = 0.1f;

  private Node2D followShape;

  public override void _Ready() {
    var parent = this.GetParent();
    followShape = (Node2D)parent.GetNode("CollisionShape2D");
    Position = followShape.Position;
  }

  public override void _Process(double delta) {
    HandleZoom();
    Position = followShape.Position;
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
