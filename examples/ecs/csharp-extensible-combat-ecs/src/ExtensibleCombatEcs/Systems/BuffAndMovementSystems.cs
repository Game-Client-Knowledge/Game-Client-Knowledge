using ExtensibleCombatEcs.Ecs;
using ExtensibleCombatEcs.Game;

namespace ExtensibleCombatEcs.Systems;

/// <summary>
/// Converts independent Buff entities into compact aggregate actor components.
/// Trigger-style buffs should use dedicated components/systems.
/// </summary>
public sealed class BuffAggregationSystem : ISimulationSystem
{
    private readonly QueryPlan _actors;
    private readonly QueryPlan _buffs;

    public BuffAggregationSystem(World world)
    {
        _actors = world.Query(
            QueryDescription.Empty
                .With<MovementModifiers>()
                .With<CombatModifiers>()
                .With<ResistanceModifiers>()
                .Without<DeadTag>()
        );
        _buffs = world.Query(QueryDescription.Empty.With<BuffEffect>());
    }

    public string Name => nameof(BuffAggregationSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _actors.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<MovementModifiers> movement =
                    chunk.GetSpan<MovementModifiers>();
                Span<CombatModifiers> combat =
                    chunk.GetSpan<CombatModifiers>();
                Span<ResistanceModifiers> resistance =
                    chunk.GetSpan<ResistanceModifiers>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    movement[row] = MovementModifiers.Identity;
                    combat[row] = CombatModifiers.Identity;
                    resistance[row] = default;
                }
            }
        }

        foreach (Archetype archetype in _buffs.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<BuffEffect> buffs = chunk.GetSpan<BuffEffect>();
                ReadOnlySpan<Entity> entities = chunk.Entities;
                for (int row = 0; row < chunk.Count; ++row)
                {
                    ref BuffEffect buff = ref buffs[row];
                    buff.RemainingSeconds -= context.DeltaTime;
                    if (buff.RemainingSeconds <= 0.0f)
                    {
                        context.Commands.Destroy(entities[row]);
                        continue;
                    }

                    if (
                        !context.World.IsAlive(buff.Owner) ||
                        !context.World.Has<MovementModifiers>(buff.Owner) ||
                        !context.World.Has<CombatModifiers>(buff.Owner) ||
                        !context.World.Has<ResistanceModifiers>(buff.Owner)
                    )
                    {
                        context.Commands.Destroy(entities[row]);
                        continue;
                    }

                    int stacks = Math.Max(1, buff.StackCount);
                    float magnitude = buff.Magnitude * stacks;
                    ref MovementModifiers movement =
                        ref context.World.Get<MovementModifiers>(buff.Owner);
                    ref CombatModifiers combat =
                        ref context.World.Get<CombatModifiers>(buff.Owner);
                    ref ResistanceModifiers resistance =
                        ref context.World.Get<ResistanceModifiers>(buff.Owner);

                    switch (buff.Kind)
                    {
                        case BuffKind.Haste:
                            movement.SpeedMultiplier *= 1.0f + magnitude;
                            break;
                        case BuffKind.Slow:
                            movement.SpeedMultiplier *=
                                MathF.Max(0.0f, 1.0f - magnitude);
                            break;
                        case BuffKind.AttackUp:
                            combat.AttackMultiplier *= 1.0f + magnitude;
                            break;
                        case BuffKind.PhysicalResistance:
                            resistance.Physical += magnitude;
                            break;
                        case BuffKind.Rooted:
                            movement.CanMove = 0;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(buff.Kind)
                            );
                    }
                }
            }
        }
    }
}

/// <summary>Resolves base stats, Buffs, and world time into hot data.</summary>
public sealed class MovementStatsSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public MovementStatsSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<BaseMoveStats>()
                .With<MovementModifiers>()
                .With<ResolvedMoveSpeed>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(MovementStatsSystem);

    public void Update(SimulationContext context)
    {
        float worldMultiplier = context.WorldRules.MovementMultiplier;
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<BaseMoveStats> baseStats =
                    chunk.GetSpan<BaseMoveStats>();
                Span<MovementModifiers> modifiers =
                    chunk.GetSpan<MovementModifiers>();
                Span<ResolvedMoveSpeed> resolved =
                    chunk.GetSpan<ResolvedMoveSpeed>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    resolved[row] = new ResolvedMoveSpeed
                    {
                        Value = MathF.Max(
                            0.0f,
                            (
                                baseStats[row].Speed +
                                modifiers[row].AdditiveSpeed
                            ) *
                            modifiers[row].SpeedMultiplier *
                            worldMultiplier
                        ),
                        CanMove = modifiers[row].CanMove,
                    };
                }
            }
        }
    }
}

/// <summary>
/// Executes all ground movement in dense loops. Navigation is replaceable.
/// </summary>
public sealed class GroundMovementSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public GroundMovementSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<GroundMover>()
                .With<Position>()
                .With<MoveIntent>()
                .With<ResolvedMoveSpeed>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(GroundMovementSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<Position> positions = chunk.GetSpan<Position>();
                Span<MoveIntent> intents = chunk.GetSpan<MoveIntent>();
                Span<ResolvedMoveSpeed> speeds =
                    chunk.GetSpan<ResolvedMoveSpeed>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    MoveIntent intent = intents[row];
                    ResolvedMoveSpeed speed = speeds[row];
                    intents[row] = default;
                    if (
                        intent.IsRequested == 0 ||
                        speed.CanMove == 0 ||
                        speed.Value <= 0.0f
                    )
                    {
                        continue;
                    }

                    Float2 candidate =
                        positions[row].Value +
                        intent.Direction.NormalizedOrZero() *
                        speed.Value *
                        context.DeltaTime;
                    if (context.Navigation.CanOccupy(candidate))
                    {
                        positions[row].Value = candidate;
                    }
                }
            }
        }
    }
}
