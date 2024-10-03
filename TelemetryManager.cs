using Godot;
using static Enemy;
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

  public void Restart() {
    shotsFired = 0;
    damageTaken = 0;
    pickupsCollected = 0;
    deathsSuffered = 0;
    distanceTraveled = 0;
    timeSpent = 0;
    killsByType.Clear();
  }

  public int GetTotalKills() {
    int count = 0;
    foreach (var pair in killsByType) {
      count += pair.Value;
    }
    return count;
  }

  public override void _Process(double delta) {
    // Update time spent
    timeSpent += delta;
    // I guess for now press space to log?
    if (Input.IsActionJustPressed("space")) {
      Write();
    }
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

  public void Write() {
    // Construct the file name and path
    string fileName = "telemetry_data_stage1.csv";
    string filePath = "user://" + fileName;

    // Use StringBuilder to build the CSV content
    System.Text.StringBuilder csvContent = new System.Text.StringBuilder();

    // Write the headers
    csvContent.AppendLine("Shots Fired,Damage Taken,Pickups Collected,Deaths Suffered,Distance Traveled,Time Spent");

    // Write the basic telemetry data
    csvContent.AppendLine($"{shotsFired},{damageTaken},{pickupsCollected},{deathsSuffered},{distanceTraveled},{timeSpent}");

    // Add a separator for the kill data
    csvContent.AppendLine("\nKills Inflicted by Enemy Type:");

    // Write kills by enemy type
    foreach (var kill in killsByType) {
      csvContent.AppendLine($"{kill.Key},{kill.Value}");
    }

    // Use Godot's FileAccess to open the file for writing
    using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
    if (file != null) {
      // Write the CSV content to the file
      file.StoreString(csvContent.ToString());
      file.Close();

      // Print confirmation
      string realPath = ProjectSettings.GlobalizePath(filePath);
      GD.Print("Telemetry data written to: " + realPath);
    } else {
      GD.PrintErr("Failed to write telemetry data to file.");
    }
  }

}
