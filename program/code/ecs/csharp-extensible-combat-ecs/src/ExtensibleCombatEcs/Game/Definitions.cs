using ExtensibleCombatEcs.Ecs;

namespace ExtensibleCombatEcs.Game;

public sealed class AttackDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required DamageType DamageType { get; init; }
    public required float BaseDamage { get; init; }
    public required float Range { get; init; }
    public required float CooldownSeconds { get; init; }
    public required int EffectId { get; init; }
}

/// <summary>
/// Large immutable definitions stay outside hot component rows. Entities keep
/// compact integer IDs.
/// </summary>
public sealed class AttackCatalog
{
    private readonly Dictionary<int, AttackDefinition> _definitions = [];

    public void Add(AttackDefinition definition) =>
        _definitions.Add(definition.Id, definition);

    public AttackDefinition Get(int id) =>
        _definitions.TryGetValue(id, out AttackDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown attack {id}.");
}

public enum TimeOfDay : byte
{
    Morning,
    Afternoon,
    Night,
}

public sealed class WorldRules
{
    public TimeOfDay TimeOfDay { get; set; }

    public float MovementMultiplier => TimeOfDay switch
    {
        TimeOfDay.Morning => 1.0f,
        TimeOfDay.Afternoon => 0.95f,
        TimeOfDay.Night => 0.85f,
        _ => 1.0f,
    };
}

public sealed class InputFrame
{
    public Float2 MoveDirection { get; set; }
    public bool AttackRequested { get; set; }
    public Entity AttackTarget { get; set; }
}

public sealed class GridNavigation
{
    private readonly HashSet<(int X, int Y)> _blockedCells = [];

    public void BlockCell(int x, int y) => _blockedCells.Add((x, y));

    public bool CanOccupy(Float2 position) =>
        !_blockedCells.Contains((
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y)
        ));
}

public sealed class EntityNames
{
    private readonly Dictionary<int, string> _names = [];

    public void Set(Entity entity, string name) =>
        _names[entity.Index] = name;

    public string Get(Entity entity) =>
        _names.TryGetValue(entity.Index, out string? name)
            ? name
            : entity.ToString();
}

public static class DamagePipeline
{
    public static float CalculateOutgoing(
        AttackDefinition definition,
        in CombatStats stats,
        in CombatModifiers modifiers
    ) =>
        MathF.Max(
            0.0f,
            (
                definition.BaseDamage +
                stats.AttackPower +
                modifiers.FlatAttackBonus
            ) * modifiers.AttackMultiplier
        );

    public static float ApplyResistance(
        float rawDamage,
        DamageType type,
        in Resistances baseResistances,
        in ResistanceModifiers modifiers
    )
    {
        float resistance = Math.Clamp(
            baseResistances.Get(type) + modifiers.Get(type),
            -0.75f,
            0.90f
        );
        return rawDamage * (1.0f - resistance);
    }
}
