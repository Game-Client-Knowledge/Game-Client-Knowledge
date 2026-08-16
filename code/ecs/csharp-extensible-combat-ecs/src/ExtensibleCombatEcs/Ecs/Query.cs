namespace ExtensibleCombatEcs.Ecs;

/// <summary>
/// Caches matched Archetypes and refreshes only after a new Archetype appears.
/// </summary>
public sealed class QueryPlan
{
    private readonly World _world;
    private readonly QueryDescription _description;
    private readonly List<Archetype> _matches = [];
    private int _knownVersion = -1;

    internal QueryPlan(World world, QueryDescription description)
    {
        _world = world;
        _description = description;
    }

    public IReadOnlyList<Archetype> Archetypes
    {
        get
        {
            if (_knownVersion != _world.ArchetypeVersion)
            {
                _matches.Clear();
                foreach (Archetype archetype in _world.Archetypes)
                {
                    if (_description.Matches(archetype.Mask))
                    {
                        _matches.Add(archetype);
                    }
                }

                _knownVersion = _world.ArchetypeVersion;
            }

            return _matches;
        }
    }
}
