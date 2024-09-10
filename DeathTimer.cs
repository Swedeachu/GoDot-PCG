using Godot;
using System;

public partial class DeathTimer : Timer {

  public override void _Ready() {
    // Connect the timeout signal to a method that queues the bullet for deletion
    Timeout += () => OnTimeout(); // wtf is this syntax and operator overload abuse lmao
  }

  private void OnTimeout() {
    GetParent().QueueFree();  // Get the bullet node (parent) and free it
  }

}
