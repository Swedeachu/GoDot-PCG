using Godot;
using System.Collections.Generic;
using static Item;

public partial class Item : RigidBody2D {

  public enum ItemType {
    Heal,
    Speed,
    Damage,
  }

  public ItemType itemType { get; set; }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    GravityScale = 0;
    FreezeMode = RigidBody2D.FreezeModeEnum.Kinematic;
    ContactMonitor = true;
    MaxContactsReported = 5;
    BodyEntered += (Node body) => OnBodyEntered(body);
    ShakeAndBake.iShouldntExistList.Add(this);
  }

  public void SetType(ItemType type) {
    itemType = type;

    // Get the TextureRect node
    var textureRect = GetNode<TextureRect>("TextureRect");

    // Load textures based on the ItemType
    if (type == ItemType.Heal) {
      // Load the heart texture
      textureRect.Texture = (Texture2D)GD.Load("res://heart.png");
    } else if (type == ItemType.Speed) {
      // Load the speed texture
      textureRect.Texture = (Texture2D)GD.Load("res://speed.png");
    } else if (type == ItemType.Damage) {
      // Load the fire texture
      textureRect.Texture = (Texture2D)GD.Load("res://fire.png");
    }
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
  }

  private void OnBodyEntered(Node body) {
    if (body is Player player) {
      // Apply the item effect to the player
      player.ApplyItemEffect(itemType);

      // Destroy the item after it's collected
      QueueFree();
      ShakeAndBake.iShouldntExistList.Remove(this);

      TelemetryManager.Instance.AddPickupCollected(); // telemetry
    }
  }

}

public class ItemWaveDescriptor {
  public Dictionary<ItemType, int> ItemCounts { get; set; }

  public ItemWaveDescriptor() {
    ItemCounts = new Dictionary<ItemType, int>();
  }
}
