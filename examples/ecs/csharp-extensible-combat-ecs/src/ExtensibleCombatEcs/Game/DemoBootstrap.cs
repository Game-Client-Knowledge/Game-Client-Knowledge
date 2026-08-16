using ExtensibleCombatEcs.Ecs;
using ExtensibleCombatEcs.Systems;

namespace ExtensibleCombatEcs.Game;

public sealed class DemoScenario
{
    public required SimulationContext Context { get; init; }
    public required SimulationPipeline Pipeline { get; init; }
    public required Entity Player { get; init; }
    public required Entity ChaserEnemy { get; init; }
    public required Entity GuardEnemy { get; init; }
    public required Entity Teammate { get; init; }
    public required Entity Obstacle { get; init; }

    public void PrintFrame()
    {
        Console.WriteLine(
            $"\nFrame {Context.FrameNumber}, " +
            $"time={Context.WorldRules.TimeOfDay}"
        );
        PrintActor(Player);
        PrintActor(ChaserEnemy);
        PrintActor(GuardEnemy);
        PrintActor(Teammate);

        foreach (ref readonly EffectEvent effect in
                 Context.EffectEvents.Events)
        {
            Console.WriteLine(
                $"  effect={effect.EffectId}: " +
                $"{Context.Names.Get(effect.Source)} -> " +
                $"{Context.Names.Get(effect.Target)}, " +
                $"damage={effect.AppliedDamage:0.00}"
            );
        }
    }

    private void PrintActor(Entity entity)
    {
        if (!Context.World.IsAlive(entity))
        {
            Console.WriteLine($"  {Context.Names.Get(entity)}: destroyed");
            return;
        }

        Position position = Context.World.Get<Position>(entity);
        Health health = Context.World.Get<Health>(entity);
        string state = Context.World.Has<DeadTag>(entity) ? "dead" : "alive";
        Console.WriteLine(
            $"  {Context.Names.Get(entity),-14} " +
            $"pos={position.Value,-14} " +
            $"hp={health.Current,6:0.00}/{health.Maximum:0.00} " +
            $"state={state}"
        );
    }
}

public static class DemoBootstrap
{
    public static DemoScenario Create()
    {
        var world = new World(chunkCapacity: 64);
        var context = new SimulationContext(
            world,
            new CommandBuffer(),
            new InputFrame(),
            new WorldRules { TimeOfDay = TimeOfDay.Afternoon },
            new GridNavigation(),
            CreateAttackCatalog(),
            new EntityNames()
        );

        Entity player = SpawnPlayer(world, context.Names);
        Entity chaser = SpawnChaser(world, context.Names, player);
        Entity guard = SpawnGuard(world, context.Names, player);
        Entity teammate = SpawnTeammate(
            world,
            context.Names,
            player,
            chaser
        );
        Entity obstacle = SpawnObstacle(
            world,
            context.Names,
            context.Navigation
        );

        SpawnBuff(
            world,
            context.Names,
            player,
            BuffKind.Haste,
            0.25f,
            2,
            2.0f,
            "Player Haste"
        );
        SpawnBuff(
            world,
            context.Names,
            player,
            BuffKind.AttackUp,
            0.20f,
            1,
            3.0f,
            "Player Attack Up"
        );
        SpawnBuff(
            world,
            context.Names,
            chaser,
            BuffKind.PhysicalResistance,
            0.10f,
            2,
            4.0f,
            "Chaser Armor"
        );

        context.Input.AttackTarget = chaser;
        return new DemoScenario
        {
            Context = context,
            Pipeline = SimulationPipeline.CreateDefault(context),
            Player = player,
            ChaserEnemy = chaser,
            GuardEnemy = guard,
            Teammate = teammate,
            Obstacle = obstacle,
        };
    }

    private static AttackCatalog CreateAttackCatalog()
    {
        var catalog = new AttackCatalog();
        catalog.Add(new AttackDefinition
        {
            Id = 1,
            Name = "Player Sword",
            DamageType = DamageType.Physical,
            BaseDamage = 12.0f,
            Range = 2.5f,
            CooldownSeconds = 0.50f,
            EffectId = 101,
        });
        catalog.Add(new AttackDefinition
        {
            Id = 2,
            Name = "Enemy Claw",
            DamageType = DamageType.Physical,
            BaseDamage = 8.0f,
            Range = 1.6f,
            CooldownSeconds = 0.75f,
            EffectId = 201,
        });
        catalog.Add(new AttackDefinition
        {
            Id = 3,
            Name = "Teammate Strike",
            DamageType = DamageType.Fire,
            BaseDamage = 7.0f,
            Range = 2.5f,
            CooldownSeconds = 0.60f,
            EffectId = 301,
        });
        return catalog;
    }

    private static Entity SpawnPlayer(World world, EntityNames names)
    {
        Entity entity = world.CreateEntity();
        AddActorCore(world, entity, new Float2(1, 1), 1, 100, 3, 5, 1);
        world.Add(entity, new PlayerTag());
        world.Add(entity, new PlayerControlled());
        names.Set(entity, "Player");
        return entity;
    }

    private static Entity SpawnChaser(
        World world,
        EntityNames names,
        Entity player
    )
    {
        Entity entity = world.CreateEntity();
        AddActorCore(world, entity, new Float2(3, 1), 2, 70, 1.5f, 3, 2);
        world.Add(entity, new EnemyTag());
        world.Add(entity, new ChaseBehavior
        {
            Target = player,
            StopDistance = 1.2f,
            AttackDistance = 1.6f,
        });
        names.Set(entity, "Chaser Enemy");
        return entity;
    }

    private static Entity SpawnGuard(
        World world,
        EntityNames names,
        Entity player
    )
    {
        var home = new Float2(7, 7);
        Entity entity = world.CreateEntity();
        AddActorCore(world, entity, home, 2, 90, 1.2f, 4, 2);
        world.Add(entity, new EnemyTag());
        world.Add(entity, new GuardBehavior
        {
            Target = player,
            Home = home,
            GuardRadius = 3.0f,
            StopDistance = 1.2f,
            AttackDistance = 1.6f,
        });
        names.Set(entity, "Guard Enemy");
        return entity;
    }

    private static Entity SpawnTeammate(
        World world,
        EntityNames names,
        Entity player,
        Entity target
    )
    {
        Entity entity = world.CreateEntity();
        AddActorCore(world, entity, new Float2(1, 0), 1, 80, 2.5f, 2, 3);
        world.Add(entity, new TeammateTag());
        world.Add(entity, new FollowBehavior
        {
            Leader = player,
            DesiredDistance = 1.0f,
        });
        world.Add(entity, new AssistAttackBehavior
        {
            Target = target,
            AttackDistance = 2.5f,
        });
        names.Set(entity, "Teammate");
        return entity;
    }

    private static Entity SpawnObstacle(
        World world,
        EntityNames names,
        GridNavigation navigation
    )
    {
        Entity entity = world.CreateEntity();
        world.Add(entity, new WorldObjectTag());
        world.Add(entity, new Position(1, 2));
        world.Add(entity, new Obstacle());
        navigation.BlockCell(1, 2);
        names.Set(entity, "Stone Object");
        return entity;
    }

    private static void SpawnBuff(
        World world,
        EntityNames names,
        Entity owner,
        BuffKind kind,
        float magnitude,
        int stacks,
        float duration,
        string name
    )
    {
        Entity entity = world.CreateEntity();
        world.Add(entity, new BuffEffect
        {
            Owner = owner,
            Kind = kind,
            Magnitude = magnitude,
            StackCount = stacks,
            RemainingSeconds = duration,
        });
        names.Set(entity, name);
    }

    private static void AddActorCore(
        World world,
        Entity entity,
        Float2 position,
        int teamId,
        float health,
        float moveSpeed,
        float attackPower,
        int attackId
    )
    {
        world.Add(entity, new Position(position.X, position.Y));
        world.Add(entity, new Faction { TeamId = teamId });
        world.Add(entity, new Health
        {
            Current = health,
            Maximum = health,
        });
        world.Add(entity, new GroundMover());
        world.Add(entity, new MoveIntent());
        world.Add(entity, new BaseMoveStats { Speed = moveSpeed });
        world.Add(entity, MovementModifiers.Identity);
        world.Add(entity, new ResolvedMoveSpeed());
        world.Add(entity, new AttackIntent());
        world.Add(entity, new CombatStats { AttackPower = attackPower });
        world.Add(entity, CombatModifiers.Identity);
        world.Add(entity, new CombatLoadout
        {
            PrimaryAttackId = attackId,
        });
        world.Add(entity, new AttackCooldown());
        world.Add(entity, new Resistances
        {
            Physical = 0.05f,
        });
        world.Add(entity, new ResistanceModifiers());
    }
}
