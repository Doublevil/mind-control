# StateWatcher

The `StateWatcher` abstract class allows you to easily track values in real-time. This is a great fit if you're trying to display up-to-date values from a game on a user interface like a WPF or Blazor application for example.

The way it works is that it periodically updates its `LatestState` property automatically, which works well in data binding scenarios, but also has events that you can subscribe to, in order to directly receive each and every state update if you need to.

To use it, you have define a class that inherits it, and implement the `ReadState` method.

Here is an example:

```csharp
public record MyGameState(int HealthPoints, string PlayerName);

public class MyGameWatcher : StateWatcher<MyGameState>
{
    private ProcessMemory _myGame;
    
    public TestStateWatcher()
        : base(30) // This makes it refresh 30 times per second
    {
        // We build a new ProcessMemory instance here, but we could also
        // have an existing instance passed as a constructor parameter.
        _myGame = ProcessMemory.OpenProcessByName("mygame.exe");
    }
    
    protected override MyGameState ReadState()
    {
        // This method will be automatically called 30 times per second.
        // It will update the LatestState property of this instance. 
        int hp = _myGame.ReadInt("mygame.exe+1F16C,24,8");
        string name = _myGame.ReadString("mygame.exe+1F16C,24,1A,8");            
        return new MyGameState(hp, name);
    }
}
```

And then this is how you would use the `MyGameWatcher` class:

```csharp
var watcher = new MyGameWatcher();

// Set this up if you need to react each time your state is updated.
// Alternatively, you can just use watcher.LatestState whenever you need it.
watcher.StateUpdated += (_, args) => Console.WriteLine(
    $"{args.State.PlayerName} has {args.State.HealthPoints} health points.");

// Don't forget to start the automatic updates
watcher.Start();
```
