using Godot;
using System;

public partial class Capture : Node2D {

  public Texture2D Texture { get; set; }

  public Vector2 offset = Vector2.Zero;

  public Player player;

  // Speed for the wipe effect (adjust this for faster/slower wipe)
  public float wipeSpeed = 2000f;

  // Timer to track when to delete the node
  private float elapsedTime = 0f;

  // Time in seconds before the node deletes itself
  private float lifeSpan = 3f; // 3 seconds

  public override void _Ready() {
    // Retrieve the Image from the Viewport texture
    var image = GetViewport().GetTexture().GetImage(); // we might need to await until render signal is sent

    // Create a new ImageTexture and load the Image data into it
    var imageTexture = ImageTexture.CreateFromImage(image);

    // Now apply this texture to the TextureRect
    var textureRect = GetNode<TextureRect>("TextureRect");
    textureRect.Texture = imageTexture;

    this.Texture = imageTexture;
  }

  public override void _PhysicsProcess(double delta) {
    // Move the texture left over time to create the wipe effect
    offset.X += (float)(wipeSpeed * delta); // Gradually increase the offset's x value to move left

    // Adjust the global position to create the wipe effect
    GlobalPosition = player.GlobalPosition - offset;

    // Track elapsed time
    elapsedTime += (float)delta;

    // Delete the node after the lifespan is over
    if (elapsedTime >= lifeSpan) {
      QueueFree(); // Removes the node from the scene
    }
  }
}
