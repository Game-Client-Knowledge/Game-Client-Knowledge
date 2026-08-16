using System.Numerics;

namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Compact component signature. The teaching implementation supports 64 types
/// so query matching is reduced to bitwise operations.
/// </summary>
public readonly struct ComponentMask : IEquatable<ComponentMask>
{
    public ComponentMask(ulong bits) => Bits = bits;

    public ulong Bits { get; }

    public ComponentMask With(int id) => new(Bits | (1UL << id));

    public ComponentMask Without(int id) => new(Bits & ~(1UL << id));

    public bool Contains(int id) => (Bits & (1UL << id)) != 0;

    public bool ContainsAll(ComponentMask required) =>
        (Bits & required.Bits) == required.Bits;

    public bool Intersects(ComponentMask other) =>
        (Bits & other.Bits) != 0;

    public IEnumerable<int> EnumerateComponentIds()
    {
        ulong remaining = Bits;
        while (remaining != 0)
        {
            int id = BitOperations.TrailingZeroCount(remaining);
            yield return id;
            remaining &= remaining - 1;
        }
    }

    public bool Equals(ComponentMask other) => Bits == other.Bits;

    public override bool Equals(object? obj) =>
        obj is ComponentMask other && Equals(other);

    public override int GetHashCode() => Bits.GetHashCode();
}

public readonly struct QueryDescription : IEquatable<QueryDescription>
{
    public QueryDescription(ComponentMask required, ComponentMask excluded)
    {
        Required = required;
        Excluded = excluded;
    }

    public ComponentMask Required { get; }

    public ComponentMask Excluded { get; }

    public static QueryDescription Empty => new(default, default);

    public QueryDescription With<T>() where T : unmanaged =>
        new(Required.With(ComponentType<T>.Id), Excluded);

    public QueryDescription Without<T>() where T : unmanaged =>
        new(Required, Excluded.With(ComponentType<T>.Id));

    public bool Matches(ComponentMask mask) =>
        mask.ContainsAll(Required) && !mask.Intersects(Excluded);

    public bool Equals(QueryDescription other) =>
        Required.Equals(other.Required) && Excluded.Equals(other.Excluded);

    public override bool Equals(object? obj) =>
        obj is QueryDescription other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Required, Excluded);
}
