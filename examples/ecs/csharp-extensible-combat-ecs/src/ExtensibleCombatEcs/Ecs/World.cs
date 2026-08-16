namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Owns entity lifecycle, physical locations, Archetypes, migration, and query
/// caching. Systems should batch through QueryPlan; Get is for sparse links.
/// </summary>
public sealed class World
{
    private readonly int _chunkCapacity;
    private readonly List<EntityLocation> _locations = [];
    private readonly Stack<int> _freeIndices = [];
    private readonly Dictionary<ComponentMask, Archetype> _archetypes = [];
    private readonly Dictionary<QueryDescription, QueryPlan> _queries = [];
    private readonly Archetype _emptyArchetype;

    public World(int chunkCapacity = 128)
    {
        if (chunkCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkCapacity));
        }

        _chunkCapacity = chunkCapacity;
        _emptyArchetype = GetOrCreateArchetype(default);
    }

    public int AliveCount { get; private set; }

    internal int ArchetypeVersion { get; private set; }

    internal IEnumerable<Archetype> Archetypes => _archetypes.Values;

    public Entity CreateEntity()
    {
        int index;
        uint generation;
        if (_freeIndices.TryPop(out int reused))
        {
            index = reused;
            generation = _locations[index].Generation;
        }
        else
        {
            index = _locations.Count;
            generation = 1;
            _locations.Add(new EntityLocation { Generation = generation });
        }

        var entity = new Entity(index, generation);
        (Chunk chunk, int row) = _emptyArchetype.Allocate(entity);
        _locations[index] = new EntityLocation
        {
            Generation = generation,
            IsAlive = true,
            Archetype = _emptyArchetype,
            Chunk = chunk,
            Row = row,
        };
        ++AliveCount;
        return entity;
    }

    public bool IsAlive(Entity entity)
    {
        if ((uint)entity.Index >= (uint)_locations.Count)
        {
            return false;
        }

        EntityLocation location = _locations[entity.Index];
        return location.IsAlive && location.Generation == entity.Generation;
    }

    public bool Has<T>(Entity entity) where T : unmanaged =>
        RequireLocation(entity).Archetype!.Contains<T>();

    public ref T Get<T>(Entity entity) where T : unmanaged
    {
        EntityLocation location = RequireLocation(entity);
        if (!location.Archetype!.Contains<T>())
        {
            throw new InvalidOperationException(
                $"{entity} does not contain {typeof(T).Name}."
            );
        }

        return ref location.Chunk!.GetRef<T>(location.Row);
    }

    public bool TryGet<T>(Entity entity, out T value) where T : unmanaged
    {
        if (!IsAlive(entity))
        {
            value = default;
            return false;
        }

        EntityLocation location = _locations[entity.Index];
        if (!location.Archetype!.Contains<T>())
        {
            value = default;
            return false;
        }

        value = location.Chunk!.GetRef<T>(location.Row);
        return true;
    }

    public void Add<T>(Entity entity, in T component) where T : unmanaged
    {
        EntityLocation source = RequireLocation(entity);
        int id = ComponentType<T>.Id;
        if (source.Archetype!.Mask.Contains(id))
        {
            source.Chunk!.Set(source.Row, component);
            return;
        }

        Archetype targetArchetype =
            GetOrCreateArchetype(source.Archetype.Mask.With(id));
        (Chunk targetChunk, int targetRow) =
            targetArchetype.Allocate(entity);

        foreach (int existingId in source.Archetype.ComponentIds)
        {
            source.Chunk!.CopyComponentTo(
                existingId,
                source.Row,
                targetChunk,
                targetRow
            );
        }

        targetChunk.Set(targetRow, component);
        SetLocation(entity, targetArchetype, targetChunk, targetRow);
        UpdateMovedLocation(
            source.Chunk!.RemoveSwapBack(source.Row),
            source.Row
        );
    }

    public bool Remove<T>(Entity entity) where T : unmanaged
    {
        EntityLocation source = RequireLocation(entity);
        int id = ComponentType<T>.Id;
        if (!source.Archetype!.Mask.Contains(id))
        {
            return false;
        }

        Archetype targetArchetype =
            GetOrCreateArchetype(source.Archetype.Mask.Without(id));
        (Chunk targetChunk, int targetRow) =
            targetArchetype.Allocate(entity);

        foreach (int existingId in source.Archetype.ComponentIds)
        {
            if (existingId != id)
            {
                source.Chunk!.CopyComponentTo(
                    existingId,
                    source.Row,
                    targetChunk,
                    targetRow
                );
            }
        }

        SetLocation(entity, targetArchetype, targetChunk, targetRow);
        UpdateMovedLocation(
            source.Chunk!.RemoveSwapBack(source.Row),
            source.Row
        );
        return true;
    }

    public void Destroy(Entity entity)
    {
        EntityLocation location = RequireLocation(entity);
        UpdateMovedLocation(
            location.Chunk!.RemoveSwapBack(location.Row),
            location.Row
        );

        _locations[entity.Index] = new EntityLocation
        {
            Generation = location.Generation == uint.MaxValue
                ? 1
                : location.Generation + 1,
            IsAlive = false,
            Row = -1,
        };
        _freeIndices.Push(entity.Index);
        --AliveCount;
    }

    public QueryPlan Query(QueryDescription description)
    {
        if (!_queries.TryGetValue(description, out QueryPlan? query))
        {
            query = new QueryPlan(this, description);
            _queries.Add(description, query);
        }

        return query;
    }

    private EntityLocation RequireLocation(Entity entity)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException(
                $"{entity} is stale or destroyed."
            );
        }

        return _locations[entity.Index];
    }

    private Archetype GetOrCreateArchetype(ComponentMask mask)
    {
        if (_archetypes.TryGetValue(mask, out Archetype? archetype))
        {
            return archetype;
        }

        archetype = new Archetype(
            mask,
            mask.EnumerateComponentIds().ToArray(),
            _chunkCapacity
        );
        _archetypes.Add(mask, archetype);
        ++ArchetypeVersion;
        return archetype;
    }

    private void SetLocation(
        Entity entity,
        Archetype archetype,
        Chunk chunk,
        int row
    )
    {
        _locations[entity.Index] = new EntityLocation
        {
            Generation = entity.Generation,
            IsAlive = true,
            Archetype = archetype,
            Chunk = chunk,
            Row = row,
        };
    }

    private void UpdateMovedLocation(Entity? movedEntity, int row)
    {
        if (movedEntity is not Entity moved)
        {
            return;
        }

        EntityLocation location = _locations[moved.Index];
        location.Row = row;
        _locations[moved.Index] = location;
    }
}
