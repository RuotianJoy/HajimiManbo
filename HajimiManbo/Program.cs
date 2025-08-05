using System;

try
{
    using var game = new HajimiManbo.Game1();
    game.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Game crashed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.ReadKey();
}
