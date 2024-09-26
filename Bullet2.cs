using Godot;
using System;

public partial class Bullet2 : RigidBody2D {

  public float Speed = 600f;  // Speed of the bullet
  private Vector2 _direction;
  private bool isEnemyBullet = false;

  public override void _Ready() {
    GravityScale = 0;  // Disable gravity for top-down movement

    // Physics stuff we need for movement and collision
    FreezeMode = RigidBody2D.FreezeModeEnum.Kinematic;
    ContactMonitor = true;
    MaxContactsReported = 5; // not sure if making this super big beyond like a 1 or a 5 matters

    BodyEntered += (Node body) => Collide(body); // weird af lambda for body entered collision callback
  }

  public void Initialize(Vector2 direction, bool enemy = false) {
    _direction = direction.Normalized();  // Normalize direction to ensure consistent speed
    LinearVelocity = _direction * Speed;  // Apply movement to the bullet using LinearVelocity
    this.isEnemyBullet = enemy;
    if (enemy) {
      CollisionLayer = (1 << 1); // layer 2
      CollisionMask = (1 << 1); // mask 2
    } else {
      CollisionLayer = (1 << 2); // layer 3
      CollisionMask = (1 << 0) | (1 << 2); // masks 1 and 3
    }
  }

  public override void _PhysicsProcess(double delta) {
    // Remove manual position updating; let physics engine handle it
  }

  private void Collide(Node body) {
    if (body is Bullet2 bullet) {
      // bullets of different types destroy eachother, otherwise they just ignore each other
      if(bullet.isEnemyBullet != this.isEnemyBullet) {
        QueueFree();
        bullet.QueueFree();
      }
      return;
    }

    if (!this.isEnemyBullet && body is Enemy enemy) { // We can damage enemies, if we are not enemies
      enemy.Damage(1);
    } else if (this.isEnemyBullet && body is Player player) { // we can damage player if we are an enemy bullet
      player.Damage(1);
      TelemetryManager.Instance.AddDamageTaken(1); // telemetry
    }

    // Queue the bullet for deletion
    QueueFree();
  }

}
