using ExtensibleCombatEcs.Ecs;
using ExtensibleCombatEcs.Game;

namespace ExtensibleCombatEcs.Systems;

/// <summary>Adapts player input into shared movement/attack intents.</summary>
public sealed class PlayerInputSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public PlayerInputSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<PlayerControlled>()
                .With<MoveIntent>()
                .With<AttackIntent>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(PlayerInputSystem);

    public void Update(SimulationContext context)
    {
        Float2 direction = context.Input.MoveDirection.NormalizedOrZero();
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<MoveIntent> moves = chunk.GetSpan<MoveIntent>();
                Span<AttackIntent> attacks = chunk.GetSpan<AttackIntent>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    moves[row] = new MoveIntent
                    {
                        Direction = direction,
                        IsRequested = direction.LengthSquared > 0
                            ? (byte)1
                            : (byte)0,
                    };
                    attacks[row] = new AttackIntent
                    {
                        Target = context.Input.AttackTarget,
                        IsRequested = context.Input.AttackRequested
                            ? (byte)1
                            : (byte)0,
                    };
                }
            }
        }
    }
}

/// <summary>Generates pursuit and attack intents for chaser enemies.</summary>
public sealed class ChaseAiSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public ChaseAiSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<Position>()
                .With<ChaseBehavior>()
                .With<MoveIntent>()
                .With<AttackIntent>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(ChaseAiSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<Position> positions = chunk.GetSpan<Position>();
                Span<ChaseBehavior> behaviors =
                    chunk.GetSpan<ChaseBehavior>();
                Span<MoveIntent> moves = chunk.GetSpan<MoveIntent>();
                Span<AttackIntent> attacks = chunk.GetSpan<AttackIntent>();

                for (int row = 0; row < chunk.Count; ++row)
                {
                    ChaseBehavior behavior = behaviors[row];
                    moves[row] = default;
                    attacks[row] = default;
                    if (!context.World.TryGet(
                        behavior.Target,
                        out Position target
                    ))
                    {
                        continue;
                    }

                    Float2 offset = target.Value - positions[row].Value;
                    float distanceSquared = offset.LengthSquared;
                    if (
                        distanceSquared >
                        behavior.StopDistance * behavior.StopDistance
                    )
                    {
                        moves[row] = new MoveIntent
                        {
                            Direction = offset.NormalizedOrZero(),
                            IsRequested = 1,
                        };
                    }

                    if (
                        distanceSquared <=
                        behavior.AttackDistance * behavior.AttackDistance
                    )
                    {
                        attacks[row] = new AttackIntent
                        {
                            Target = behavior.Target,
                            IsRequested = 1,
                        };
                    }
                }
            }
        }
    }
}

/// <summary>
/// Pursues only inside a guard area and otherwise returns home.
/// </summary>
public sealed class GuardAiSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public GuardAiSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<Position>()
                .With<GuardBehavior>()
                .With<MoveIntent>()
                .With<AttackIntent>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(GuardAiSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<Position> positions = chunk.GetSpan<Position>();
                Span<GuardBehavior> behaviors =
                    chunk.GetSpan<GuardBehavior>();
                Span<MoveIntent> moves = chunk.GetSpan<MoveIntent>();
                Span<AttackIntent> attacks = chunk.GetSpan<AttackIntent>();

                for (int row = 0; row < chunk.Count; ++row)
                {
                    GuardBehavior behavior = behaviors[row];
                    moves[row] = default;
                    attacks[row] = default;
                    bool found = context.World.TryGet(
                        behavior.Target,
                        out Position target
                    );
                    bool targetInsideArea =
                        found &&
                        Float2.DistanceSquared(
                            target.Value,
                            behavior.Home
                        ) <= behavior.GuardRadius * behavior.GuardRadius;

                    Float2 destination = targetInsideArea
                        ? target.Value
                        : behavior.Home;
                    Float2 offset = destination - positions[row].Value;
                    float distanceSquared = offset.LengthSquared;
                    if (
                        distanceSquared >
                        behavior.StopDistance * behavior.StopDistance
                    )
                    {
                        moves[row] = new MoveIntent
                        {
                            Direction = offset.NormalizedOrZero(),
                            IsRequested = 1,
                        };
                    }

                    if (
                        targetInsideArea &&
                        distanceSquared <=
                        behavior.AttackDistance * behavior.AttackDistance
                    )
                    {
                        attacks[row] = new AttackIntent
                        {
                            Target = behavior.Target,
                            IsRequested = 1,
                        };
                    }
                }
            }
        }
    }
}

/// <summary>Maintains distance to a leader without owning movement code.</summary>
public sealed class FollowAiSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public FollowAiSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<Position>()
                .With<FollowBehavior>()
                .With<MoveIntent>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(FollowAiSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<Position> positions = chunk.GetSpan<Position>();
                Span<FollowBehavior> behaviors =
                    chunk.GetSpan<FollowBehavior>();
                Span<MoveIntent> moves = chunk.GetSpan<MoveIntent>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    moves[row] = default;
                    FollowBehavior behavior = behaviors[row];
                    if (!context.World.TryGet(
                        behavior.Leader,
                        out Position leader
                    ))
                    {
                        continue;
                    }

                    Float2 offset = leader.Value - positions[row].Value;
                    if (
                        offset.LengthSquared >
                        behavior.DesiredDistance *
                        behavior.DesiredDistance
                    )
                    {
                        moves[row] = new MoveIntent
                        {
                            Direction = offset.NormalizedOrZero(),
                            IsRequested = 1,
                        };
                    }
                }
            }
        }
    }
}

/// <summary>
/// Produces teammate attack intent separately from teammate following.
/// </summary>
public sealed class AssistAttackAiSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public AssistAttackAiSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<Position>()
                .With<AssistAttackBehavior>()
                .With<AttackIntent>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(AssistAttackAiSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<Position> positions = chunk.GetSpan<Position>();
                Span<AssistAttackBehavior> behaviors =
                    chunk.GetSpan<AssistAttackBehavior>();
                Span<AttackIntent> attacks = chunk.GetSpan<AttackIntent>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    attacks[row] = default;
                    AssistAttackBehavior behavior = behaviors[row];
                    if (
                        context.World.TryGet(
                            behavior.Target,
                            out Position target
                        ) &&
                        Float2.DistanceSquared(
                            positions[row].Value,
                            target.Value
                        ) <= behavior.AttackDistance *
                        behavior.AttackDistance
                    )
                    {
                        attacks[row] = new AttackIntent
                        {
                            Target = behavior.Target,
                            IsRequested = 1,
                        };
                    }
                }
            }
        }
    }
}
