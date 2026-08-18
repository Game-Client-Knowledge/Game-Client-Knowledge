namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Owns all Chunks for one exact component combination.
/// </summary>
public sealed class Archetype
{
    private readonly List<Chunk> _chunks = [];

    internal Archetype(
        ComponentMask mask,
        IReadOnlyList<int> componentIds,
        int chunkCapacity
    )
    {
        Mask = mask;
        ComponentIds = componentIds;
        ChunkCapacity = chunkCapacity;
    }

    public ComponentMask Mask { get; }

    public IReadOnlyList<int> ComponentIds { get; }

    public IReadOnlyList<Chunk> Chunks => _chunks;

    public int ChunkCapacity { get; }

    public bool Contains<T>() where T : unmanaged =>
        Mask.Contains(ComponentType<T>.Id);

    internal (Chunk Chunk, int Row) Allocate(Entity entity)
    {
        Chunk? chunk = null;
        foreach (Chunk candidate in _chunks)
        {
            if (candidate.HasSpace)
            {
                chunk = candidate;
                break;
            }
        }

        if (chunk is null)
        {
            chunk = new Chunk(this, ChunkCapacity);
            _chunks.Add(chunk);
        }

        return (chunk, chunk.Append(entity));
    }
}
