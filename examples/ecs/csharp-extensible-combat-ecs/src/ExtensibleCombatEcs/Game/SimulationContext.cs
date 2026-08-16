using ExtensibleCombatEcs.Ecs;

namespace ExtensibleCombatEcs.Game;

public sealed class SimulationContext
{
    public SimulationContext(
        World world,
        CommandBuffer commands,
        InputFrame input,
        WorldRules worldRules,
        GridNavigation navigation,
        AttackCatalog attacks,
        EntityNames names
    )
    {
        World = world;
        Commands = commands;
        Input = input;
        WorldRules = worldRules;
        Navigation = navigation;
        Attacks = attacks;
        Names = names;
    }

    public World World { get; }
    public CommandBuffer Commands { get; }
    public InputFrame Input { get; }
    public WorldRules WorldRules { get; }
    public GridNavigation Navigation { get; }
    public AttackCatalog Attacks { get; }
    public EntityNames Names { get; }
    public FrameEventBuffer<DamageEvent> DamageEvents { get; } = new();
    public FrameEventBuffer<EffectEvent> EffectEvents { get; } = new();
    public float DeltaTime { get; private set; }
    public int FrameNumber { get; private set; }

    public void BeginFrame(float deltaTime)
    {
        if (deltaTime <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        DeltaTime = deltaTime;
        ++FrameNumber;
        DamageEvents.Clear();
        EffectEvents.Clear();
    }
}

public interface ISimulationSystem
{
    string Name { get; }

    void Update(SimulationContext context);
}
