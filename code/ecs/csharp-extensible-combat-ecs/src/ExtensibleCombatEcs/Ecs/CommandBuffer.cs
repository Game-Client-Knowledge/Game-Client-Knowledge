namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Defers structural changes until all stable query loops finish.
/// The teaching version boxes commands; production code should use typed,
/// preallocated command streams.
/// </summary>
public sealed class CommandBuffer
{
    private readonly List<ICommand> _commands = [];

    public void Add<T>(Entity entity, in T value) where T : unmanaged =>
        _commands.Add(new AddCommand<T>(entity, value));

    public void Remove<T>(Entity entity) where T : unmanaged =>
        _commands.Add(new RemoveCommand<T>(entity));

    public void Destroy(Entity entity) =>
        _commands.Add(new DestroyCommand(entity));

    public void Playback(World world)
    {
        foreach (ICommand command in _commands)
        {
            command.Apply(world);
        }

        _commands.Clear();
    }

    private interface ICommand
    {
        void Apply(World world);
    }

    private readonly struct AddCommand<T> : ICommand where T : unmanaged
    {
        private readonly Entity _entity;
        private readonly T _value;

        public AddCommand(Entity entity, in T value)
        {
            _entity = entity;
            _value = value;
        }

        public void Apply(World world)
        {
            if (world.IsAlive(_entity))
            {
                world.Add(_entity, _value);
            }
        }
    }

    private readonly struct RemoveCommand<T> : ICommand where T : unmanaged
    {
        private readonly Entity _entity;

        public RemoveCommand(Entity entity) => _entity = entity;

        public void Apply(World world)
        {
            if (world.IsAlive(_entity))
            {
                world.Remove<T>(_entity);
            }
        }
    }

    private readonly struct DestroyCommand : ICommand
    {
        private readonly Entity _entity;

        public DestroyCommand(Entity entity) => _entity = entity;

        public void Apply(World world)
        {
            if (world.IsAlive(_entity))
            {
                world.Destroy(_entity);
            }
        }
    }
}
