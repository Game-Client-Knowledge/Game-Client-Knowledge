namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Entity is a small value handle, not a gameplay object.
/// Index locates a slot; Generation rejects stale handles after slot reuse.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    public Entity(int index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    public int Index { get; }

    public uint Generation { get; }

    public bool Equals(Entity other) =>
        Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) =>
        obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    public override string ToString() => $"Entity({Index}:{Generation})";

    public static bool operator ==(Entity left, Entity right) =>
        left.Equals(right);

    public static bool operator !=(Entity left, Entity right) =>
        !left.Equals(right);
}

internal struct EntityLocation
{
    public uint Generation;
    public bool IsAlive;
    public Archetype? Archetype;
    public Chunk? Chunk;
    public int Row;
}
