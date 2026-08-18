using ExtensibleCombatEcs.Ecs;

namespace ExtensibleCombatEcs.Game;

/// <summary>Dependency-free smoke tests for a fresh clone.</summary>
public static class DemoSelfTests
{
    public static void RunAll()
    {
        TestGeneration();
        TestMigrationAndQuery();
        TestSwapRemove();
        TestGameplay();
        Console.WriteLine("All ECS demo self-tests passed.");
    }

    private static void TestGeneration()
    {
        var world = new World(4);
        Entity oldEntity = world.CreateEntity();
        world.Add(oldEntity, new Position(1, 2));
        world.Destroy(oldEntity);
        Entity current = world.CreateEntity();

        Assert(oldEntity.Index == current.Index, "slot should be reused");
        Assert(
            oldEntity.Generation != current.Generation,
            "generation should advance"
        );
        Assert(!world.IsAlive(oldEntity), "stale handle should be invalid");
    }

    private static void TestMigrationAndQuery()
    {
        var world = new World(4);
        Entity first = world.CreateEntity();
        Entity second = world.CreateEntity();
        world.Add(first, new Position(1, 0));
        world.Add(first, new Health { Current = 10, Maximum = 10 });
        world.Add(second, new Position(2, 0));
        world.Add(second, new Health { Current = 20, Maximum = 20 });

        QueryPlan query = world.Query(
            QueryDescription.Empty.With<Position>().With<Health>()
        );
        Assert(Count(query) == 2, "both entities should match");
        world.Remove<Health>(first);
        Assert(Count(query) == 1, "query should refresh after migration");
        Assert(
            MathF.Abs(world.Get<Position>(second).Value.X - 2) < 0.0001f,
            "component values should survive migration"
        );
    }

    private static void TestSwapRemove()
    {
        var world = new World(4);
        Entity first = world.CreateEntity();
        Entity second = world.CreateEntity();
        world.Add(first, new Position(10, 0));
        world.Add(second, new Position(20, 0));
        world.Destroy(first);
        Assert(
            MathF.Abs(world.Get<Position>(second).Value.X - 20) < 0.0001f,
            "moved entity location should be repaired"
        );
    }

    private static void TestGameplay()
    {
        DemoScenario scenario = DemoBootstrap.Create();
        World world = scenario.Context.World;
        float initialHealth = world.Get<Health>(
            scenario.ChaserEnemy
        ).Current;
        scenario.Context.Input.MoveDirection = new Float2(0, 1);
        scenario.Context.Input.AttackRequested = true;
        scenario.Pipeline.RunFrame(scenario.Context, 0.25f);

        Assert(
            world.Get<Health>(scenario.ChaserEnemy).Current < initialHealth,
            "player and teammate should damage the chaser"
        );
        Assert(
            scenario.Context.EffectEvents.Count > 0,
            "damage should produce effects"
        );
        Assert(
            Float2.DistanceSquared(
                world.Get<Position>(scenario.Player).Value,
                new Float2(1, 1)
            ) < 0.0001f,
            "obstacle should block the player"
        );

        for (int frame = 1; frame < 7; ++frame)
        {
            scenario.Pipeline.RunFrame(scenario.Context, 0.25f);
        }

        Assert(
            world.Has<DeadTag>(scenario.ChaserEnemy),
            "lethal damage should become DeadTag"
        );
    }

    private static int Count(QueryPlan query)
    {
        int count = 0;
        foreach (Archetype archetype in query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                count += chunk.Count;
            }
        }

        return count;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Self-test failed: {message}"
            );
        }
    }
}
