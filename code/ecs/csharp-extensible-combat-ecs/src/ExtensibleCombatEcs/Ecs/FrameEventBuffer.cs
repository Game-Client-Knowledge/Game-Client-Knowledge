namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Reusable dense frame-local event stream. T is unmanaged, so events do not
/// allocate one managed object each.
/// </summary>
public sealed class FrameEventBuffer<T> where T : unmanaged
{
    private T[] _items;

    public FrameEventBuffer(int initialCapacity = 64)
    {
        _items = new T[initialCapacity > 0
            ? initialCapacity
            : throw new ArgumentOutOfRangeException(nameof(initialCapacity))];
    }

    public int Count { get; private set; }

    public ReadOnlySpan<T> Events => _items.AsSpan(0, Count);

    public void Add(in T item)
    {
        if (Count == _items.Length)
        {
            Array.Resize(ref _items, checked(_items.Length * 2));
        }

        _items[Count++] = item;
    }

    public void Clear() => Count = 0;
}
