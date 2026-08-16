namespace ExtensibleCombatEcs.Ecs;

internal interface IComponentDescriptor
{
    int Id { get; }

    IComponentColumn CreateColumn(int capacity);
}

internal sealed class ComponentDescriptor<T> : IComponentDescriptor
    where T : unmanaged
{
    public ComponentDescriptor(int id) => Id = id;

    public int Id { get; }

    public IComponentColumn CreateColumn(int capacity) =>
        new ComponentColumn<T>(Id, capacity);
}

/// <summary>
/// Assigns compact process-local IDs. These IDs are not stable save/network
/// schema identifiers.
/// </summary>
internal static class ComponentRegistry
{
    private const int MaximumTypes = 64;
    private static readonly object Gate = new();
    private static readonly List<IComponentDescriptor> Descriptors = [];

    public static int Register<T>() where T : unmanaged
    {
        lock (Gate)
        {
            if (Descriptors.Count >= MaximumTypes)
            {
                throw new InvalidOperationException(
                    $"At most {MaximumTypes} component types are supported."
                );
            }

            int id = Descriptors.Count;
            Descriptors.Add(new ComponentDescriptor<T>(id));
            return id;
        }
    }

    public static IComponentDescriptor Get(int id)
    {
        lock (Gate)
        {
            return Descriptors[id];
        }
    }
}

public static class ComponentType<T> where T : unmanaged
{
    public static int Id { get; } = ComponentRegistry.Register<T>();
}
