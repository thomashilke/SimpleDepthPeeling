using (Game game = new Game(800, 600, "LearnOpenTK"))
{
    game.UpdateFrequency = 1.0;
    game.VSync = OpenTK.Windowing.Common.VSyncMode.Off;
    game.Run();
}
