namespace ExtensibleCombatEcs.Ecs;

internal interface IComponentColumn
{
    void CopyElementTo(
        int sourceRow,
        IComponentColumn target,
        int targetRow
    );

    void MoveElement(int sourceRow, int targetRow);

    void ClearElement(int row);
}

/// <summary>
/// One dense Structure-of-Arrays column for a component type.
/// </summary>
internal sealed class ComponentColumn<T> : IComponentColumn
    where T : unmanaged
{
    private readonly T[] _items;

    public ComponentColumn(int componentId, int capacity)
    {
        ComponentId = componentId;
        _items = new T[capacity];
    }

    public int ComponentId { get; }

    public Span<T> AsSpan(int count) => _items.AsSpan(0, count);

    public ref T GetRef(int row) => ref _items[row];

    public void Set(int row, in T value) => _items[row] = value;

    public void CopyElementTo(
        int sourceRow,
        IComponentColumn target,
        int targetRow
    )
    {
        ((ComponentColumn<T>)target)._items[targetRow] = _items[sourceRow];
    }

    public void MoveElement(int sourceRow, int targetRow) =>
        _items[targetRow] = _items[sourceRow];

    public void ClearElement(int row) => _items[row] = default;
}
