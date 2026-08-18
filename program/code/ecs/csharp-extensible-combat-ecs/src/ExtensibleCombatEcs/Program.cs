using ExtensibleCombatEcs.Game;

if (args.Contains("--self-test", StringComparer.Ordinal))
{
    DemoSelfTests.RunAll();
    return;
}

DemoSelfTests.RunAll();
DemoScenario scenario = DemoBootstrap.Create();
Console.WriteLine("\nExtensible combat ECS demonstration");
Console.WriteLine("Player moves up into an obstacle while AI produces intents.");

for (int frame = 0; frame < 8; ++frame)
{
    scenario.Context.Input.MoveDirection =
        frame < 5 ? new Float2(0, 1) : default;
    scenario.Context.Input.AttackRequested = true;
    scenario.Pipeline.RunFrame(scenario.Context, 0.25f);
    scenario.PrintFrame();
}
