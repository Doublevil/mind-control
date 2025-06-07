using MindControl.Samples.SlimeRancherDemo;

Console.Clear();
Console.Title = "Slime Rancher Demo MindControl Console App";
var slimeRancher = new SlimeRancherDemo();

Console.WriteLine("This application is a demo for the MindControl library.");
Console.WriteLine("Start the free Slime Rancher demo to see and edit the game state.");
Console.WriteLine("Slime Rancher belongs to Monomi Park. They are not responsible for this software.");

// Main loop: read the game state every 100ms and display it in the console.
while (true)
{
    var gameState = slimeRancher.GetGameState();
    Console.SetCursorPosition(0, 4);
    switch (gameState.State)
    {
        case GameRunState.NotRunning:
            Console.WriteLine("The Slime Rancher demo is not running.".PadRight(Console.WindowWidth));
            ClearConsoleRows();
            break;
        case GameRunState.InMenu:
            Console.WriteLine("The Slime Rancher demo is running, but in a menu.".PadRight(Console.WindowWidth));
            ClearConsoleRows();
            break;
        case GameRunState.InGame:
            DisplayPlayerState(gameState.Player.GetValueOrDefault());
            break;
    };

    await Task.Delay(TimeSpan.FromMilliseconds(100));
}

// Display the player state in the console.
void DisplayPlayerState(PlayerState playerState)
{
    // We use PadRight to clear older text when the player state changes, because we are writing over the same lines
    // without ever clearing the console, to prevent flickering.
    
    Console.WriteLine("Slime Rancher Demo Player State:".PadRight(Console.WindowWidth - 1));
    string health = $"[H]ealth:  {playerState.CurrentHealth}/{playerState.MaxHealth}";
    Console.WriteLine(health.PadRight(Console.WindowWidth - 1));
    string stamina = $"[S]tamina: {playerState.CurrentStamina}/{playerState.MaxStamina}";
    Console.WriteLine(stamina.PadRight(Console.WindowWidth - 1));
    string coins = $"[C]oins:   {playerState.CoinCount}";
    Console.WriteLine(coins.PadRight(Console.WindowWidth - 1));
    Console.WriteLine();
    Console.WriteLine("Press H/S/C to modify the player state, or F to toggle infinite stamina.");
    HandleUserInput(playerState);
}

// Handle user input to modify the game state.
void HandleUserInput(PlayerState playerState)
{
    // Check if a key has been pressed
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(intercept: true).Key;
        if (key == ConsoleKey.Escape)
        {
            // Exit if the Escape key is pressed
            Environment.Exit(0);
        }
            
        switch (key)
        {
            case ConsoleKey.H:
            {
                // Decrease health
                var newPlayerState = playerState with { CurrentHealth = playerState.CurrentHealth - 10 };
                slimeRancher.SetPlayerState(newPlayerState);
                break;
            }
            case ConsoleKey.S:
            {
                // Decrease stamina
                var newPlayerState = playerState with { CurrentStamina = playerState.CurrentStamina - 10 };
                slimeRancher.SetPlayerState(newPlayerState);
                break;
            }
            case ConsoleKey.C:
            {
                // Increase coin count
                var newPlayerState = playerState with { CoinCount = playerState.CoinCount + 100 };
                slimeRancher.SetPlayerState(newPlayerState);
                break;
            }
            case ConsoleKey.F:
            {
                // Toggle infinite stamina
                slimeRancher.ToggleInfiniteStamina();
                break;
            }
        }
    }
}

// Clears the console rows below the current cursor position, up to a maximum of 10 rows.
// We use this to prevent old player state information from being displayed when the state changes.
void ClearConsoleRows()
{
    for (int i = Console.CursorTop - 1; i < 10; i++)
    {
        Console.SetCursorPosition(0, i);
        Console.Write(new string(' ', Console.WindowWidth));
    }
}
