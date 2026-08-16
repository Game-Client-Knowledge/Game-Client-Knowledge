using ExtensibleCombatEcs.Game;

namespace ExtensibleCombatEcs.Systems;

/// <summary>
/// Explicit teaching pipeline. A production scheduler can replace this with a
/// read/write dependency DAG without changing gameplay components.
/// </summary>
public sealed class SimulationPipeline
{
    private readonly List<ISimulationSystem> _systems = [];

    public SimulationPipeline Add(ISimulationSystem system)
    {
        _systems.Add(system);
        return this;
    }

    public void RunFrame(SimulationContext context, float deltaTime)
    {
        context.BeginFrame(deltaTime);
        foreach (ISimulationSystem system in _systems)
        {
            system.Update(context);
        }

        context.Commands.Playback(context.World);
    }

    public static SimulationPipeline CreateDefault(SimulationContext context)
    {
        return new SimulationPipeline()
            .Add(new CooldownSystem(context.World))
            .Add(new PlayerInputSystem(context.World))
            .Add(new ChaseAiSystem(context.World))
            .Add(new GuardAiSystem(context.World))
            .Add(new FollowAiSystem(context.World))
            .Add(new AssistAttackAiSystem(context.World))
            .Add(new BuffAggregationSystem(context.World))
            .Add(new MovementStatsSystem(context.World))
            .Add(new GroundMovementSystem(context.World))
            .Add(new AttackSystem(context.World))
            .Add(new DamageApplySystem())
            .Add(new DeathSystem(context.World));
    }
}
