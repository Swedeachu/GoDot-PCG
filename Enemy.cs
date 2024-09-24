using Godot;
using System;

public partial class Enemy : CharacterBody2D {

  private Node2D target;

  // Speed and acceleration variables
  private float speed = 200f;
  private float acceleration = 7f;

  // NavigationAgent2D to handle pathfinding
  private NavigationAgent2D navigationAgent;

  public override void _Ready() {
    // Get the NavigationAgent2D from the scene tree
    navigationAgent = GetNode<NavigationAgent2D>("Navigation/NavigationAgent2D");
    var timer = GetNode<Timer>("Navigation/Timer");
    timer.Timeout += OnTimerTimeout;
    FindPlayer();
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 direction = Vector2.Zero;

    // Get the next position along the path and calculate the direction
    direction = (navigationAgent.GetNextPathPosition() - GlobalPosition).Normalized();

    // Apply velocity smoothing using linear interpolation (lerp)
    Velocity = Velocity.Lerp(direction * speed, acceleration * (float)delta);

    // Move the enemy using MoveAndSlide()
    MoveAndSlide();
  }

  private void OnTimerTimeout() {
    if (target != null) {
      // Set the target position for the NavigationAgent2D to follow
      navigationAgent.TargetPosition = target.GlobalPosition;
    } else {
      FindPlayer();
    }
  }

  private void FindPlayer() {
    // Find the "World" node first
    var worldNode = GetTree().Root.GetNode("World");

    if (worldNode != null) {
      // Search for the Player node inside the World node
      var playerNode = worldNode.GetNode("Player") as Node2D;

      if (playerNode != null) {
        target = playerNode;
        GD.Print("Player found: ", target.Name);
      } else {
        GD.PrintErr("Player node not found!");
      }
    } else {
      GD.PrintErr("World node not found!");
    }
  }


}
