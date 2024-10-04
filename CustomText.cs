using Godot;
using System;
using System.Threading.Tasks;

public partial class CustomText : Label {

  private float lifeTime = 5.0f;    // Life time in seconds

  public void SetLifeTime(float lifeTime) {
    this.lifeTime = lifeTime;
  }

  public override void _Ready() {
    // Start the async floating and fading process
    StartFloatingAndFading();
  }

  // Start the floating and fading process asynchronously
  private async void StartFloatingAndFading() {
    // After the life time has passed, free the node
    await ToSignal(GetTree().CreateTimer(lifeTime), "timeout");
    QueueFree();
  }

}
