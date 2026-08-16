namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Fixed-capacity block. Rows [0, Count) stay packed and every component type
/// is stored in an independent dense column.
/// </summary>
public sealed class Chunk
{
    private readonly Entity[] _entities;
    private readonly Dictionary<int, IComponentColumn> _columns;

    internal Chunk(Archetype archetype, int capacity)
    {
        Archetype = archetype;
        Capacity = capacity;
        _entities = new Entity[capacity];
        _columns = new Dictionary<int, IComponentColumn>(
            archetype.ComponentIds.Count
        );

        foreach (int id in archetype.ComponentIds)
        {
            _columns.Add(id, ComponentRegistry.Get(id).CreateColumn(capacity));
        }
    }

    public Archetype Archetype { get; }

    public int Count { get; private set; }

    public int Capacity { get; }

    public bool HasSpace => Count < Capacity;

    public ReadOnlySpan<Entity> Entities => _entities.AsSpan(0, Count);

    public Span<T> GetSpan<T>() where T : unmanaged =>
        GetColumn<T>().AsSpan(Count);

    public ref T GetRef<T>(int row) where T : unmanaged =>
        ref GetColumn<T>().GetRef(row);

    internal int Append(Entity entity)
    {
        if (!HasSpace)
        {
            throw new InvalidOperationException("Chunk is full.");
        }

        int row = Count++;
        _entities[row] = entity;
        return row;
    }

    internal void Set<T>(int row, in T value) where T : unmanaged =>
        GetColumn<T>().Set(row, value);

    internal void CopyComponentTo(
        int id,
        int sourceRow,
        Chunk target,
        int targetRow
    )
    {
        _columns[id].CopyElementTo(
            sourceRow,
            target._columns[id],
            targetRow
        );
    }

    internal Entity? RemoveSwapBack(int row)
    {
        if ((uint)row >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        int last = Count - 1;
        Entity? moved = null;
        if (row != last)
        {
            foreach (IComponentColumn column in _columns.Values)
            {
                column.MoveElement(last, row);
            }

            _entities[row] = _entities[last];
            moved = _entities[row];
        }

        foreach (IComponentColumn column in _columns.Values)
        {
            column.ClearElement(last);
        }

        _entities[last] = default;
        --Count;
        return moved;
    }

    private ComponentColumn<T> GetColumn<T>() where T : unmanaged
    {
        int id = ComponentType<T>.Id;
        if (!_columns.TryGetValue(id, out IComponentColumn? column))
        {
            throw new InvalidOperationException(
                $"Chunk does not contain {typeof(T).Name}."
            );
        }

        return (ComponentColumn<T>)column;
    }
}
