using ExtensibleCombatEcs.Ecs;
using ExtensibleCombatEcs.Game;

namespace ExtensibleCombatEcs.Systems;

public sealed class CooldownSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public CooldownSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<AttackCooldown>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(CooldownSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                Span<AttackCooldown> cooldowns =
                    chunk.GetSpan<AttackCooldown>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    cooldowns[row].RemainingSeconds = MathF.Max(
                        0.0f,
                        cooldowns[row].RemainingSeconds -
                        context.DeltaTime
                    );
                }
            }
        }
    }
}

/// <summary>
/// Validates intent, emits DamageEvent, and never directly mutates the target.
/// </summary>
public sealed class AttackSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public AttackSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<Position>()
                .With<Faction>()
                .With<CombatStats>()
                .With<CombatModifiers>()
                .With<CombatLoadout>()
                .With<AttackCooldown>()
                .With<AttackIntent>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(AttackSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                ReadOnlySpan<Entity> entities = chunk.Entities;
                Span<Position> positions = chunk.GetSpan<Position>();
                Span<Faction> factions = chunk.GetSpan<Faction>();
                Span<CombatStats> stats = chunk.GetSpan<CombatStats>();
                Span<CombatModifiers> modifiers =
                    chunk.GetSpan<CombatModifiers>();
                Span<CombatLoadout> loadouts =
                    chunk.GetSpan<CombatLoadout>();
                Span<AttackCooldown> cooldowns =
                    chunk.GetSpan<AttackCooldown>();
                Span<AttackIntent> intents =
                    chunk.GetSpan<AttackIntent>();

                for (int row = 0; row < chunk.Count; ++row)
                {
                    AttackIntent intent = intents[row];
                    intents[row] = default;
                    if (
                        intent.IsRequested == 0 ||
                        cooldowns[row].RemainingSeconds > 0.0f ||
                        !context.World.IsAlive(intent.Target) ||
                        !context.World.TryGet(
                            intent.Target,
                            out Position targetPosition
                        ) ||
                        !context.World.TryGet(
                            intent.Target,
                            out Faction targetFaction
                        ) ||
                        !context.World.Has<Health>(intent.Target) ||
                        context.World.Has<DeadTag>(intent.Target)
                    )
                    {
                        continue;
                    }

                    if (factions[row].TeamId == targetFaction.TeamId)
                    {
                        continue;
                    }

                    AttackDefinition definition =
                        context.Attacks.Get(loadouts[row].PrimaryAttackId);
                    if (
                        Float2.DistanceSquared(
                            positions[row].Value,
                            targetPosition.Value
                        ) > definition.Range * definition.Range
                    )
                    {
                        continue;
                    }

                    context.DamageEvents.Add(new DamageEvent
                    {
                        Source = entities[row],
                        Target = intent.Target,
                        DamageType = definition.DamageType,
                        RawDamage = DamagePipeline.CalculateOutgoing(
                            definition,
                            stats[row],
                            modifiers[row]
                        ),
                        EffectId = definition.EffectId,
                    });
                    cooldowns[row].RemainingSeconds =
                        definition.CooldownSeconds;
                }
            }
        }
    }
}

/// <summary>
/// Applies target-side resistance. High-volume versions can sort events by
/// target Chunk to improve locality.
/// </summary>
public sealed class DamageApplySystem : ISimulationSystem
{
    public string Name => nameof(DamageApplySystem);

    public void Update(SimulationContext context)
    {
        foreach (ref readonly DamageEvent damage in
                 context.DamageEvents.Events)
        {
            if (
                !context.World.IsAlive(damage.Target) ||
                !context.World.Has<Health>(damage.Target) ||
                !context.World.TryGet(
                    damage.Target,
                    out Resistances resistances
                ) ||
                !context.World.TryGet(
                    damage.Target,
                    out ResistanceModifiers modifiers
                )
            )
            {
                continue;
            }

            float applied = DamagePipeline.ApplyResistance(
                damage.RawDamage,
                damage.DamageType,
                resistances,
                modifiers
            );
            ref Health health = ref context.World.Get<Health>(damage.Target);
            health.Current = MathF.Max(0.0f, health.Current - applied);
            context.EffectEvents.Add(new EffectEvent
            {
                Source = damage.Source,
                Target = damage.Target,
                EffectId = damage.EffectId,
                AppliedDamage = applied,
            });
        }
    }
}

/// <summary>Adds DeadTag through CommandBuffer after stable iteration.</summary>
public sealed class DeathSystem : ISimulationSystem
{
    private readonly QueryPlan _query;

    public DeathSystem(World world)
    {
        _query = world.Query(
            QueryDescription.Empty
                .With<Health>()
                .Without<DeadTag>()
        );
    }

    public string Name => nameof(DeathSystem);

    public void Update(SimulationContext context)
    {
        foreach (Archetype archetype in _query.Archetypes)
        {
            foreach (Chunk chunk in archetype.Chunks)
            {
                ReadOnlySpan<Entity> entities = chunk.Entities;
                Span<Health> health = chunk.GetSpan<Health>();
                for (int row = 0; row < chunk.Count; ++row)
                {
                    if (health[row].Current <= 0.0f)
                    {
                        context.Commands.Add(entities[row], new DeadTag());
                    }
                }
            }
        }
    }
}
