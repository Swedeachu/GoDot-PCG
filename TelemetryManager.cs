using Godot;
using static Enemy;
using System.IO;
using System.Collections.Generic;

public partial class TelemetryManager : Node2D {

  // Telemetry fields
  private int shotsFired;
  private int damageTaken;
  private int pickupsCollected;
  private Dictionary<Enemy.EnemyType, int> killsByType = new Dictionary<Enemy.EnemyType, int>(); // Tracks kills by EnemyType
  private int deathsSuffered;
  private float distanceTraveled;
  private double timeSpent; // Time spent in the game, in seconds

  public static TelemetryManager Instance { get; private set; }

  public override void _Ready() {
    Instance = this;
  }

  // TODO : key press to save telemetry
  public override void _Process(double delta) {
    // Update time spent
    timeSpent += delta;
  }

  public void AddShotFired() { shotsFired++; }

  public void AddDamageTaken(int damage) { damageTaken += damage; }

  public void AddPickupCollected() { pickupsCollected++; }

  public void AddKill(EnemyType type) {
    if (!killsByType.ContainsKey(type)) {
      killsByType[type] = 0;
    }

    killsByType[type]++;
  }

  public void AddDeath() { deathsSuffered++; }

  public void AddDistance(float distance) { distanceTraveled += distance; }

  public void Write(string filePath) {
    using (StreamWriter writer = new StreamWriter(filePath)) {
      // Write headers
      writer.WriteLine("Shots Fired,Damage Taken,Pickups Collected,Deaths Suffered,Distance Traveled,Time Spent");

      // Write basic telemetry data
      writer.WriteLine($"{shotsFired},{damageTaken},{pickupsCollected},{deathsSuffered},{distanceTraveled},{timeSpent}");

      // Write kills by type
      writer.WriteLine("\nKills Inflicted by Enemy Type:");
      foreach (var kill in killsByType) {
        writer.WriteLine($"{kill.Key},{kill.Value}");
      }
    }

    GD.Print("Telemetry data written to: " + filePath);
  }

}
