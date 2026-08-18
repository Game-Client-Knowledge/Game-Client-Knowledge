using ExtensibleCombatEcs.Ecs;

namespace ExtensibleCombatEcs.Game;

public struct Float2
{
    public Float2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X;
    public float Y;

    public readonly float LengthSquared => X * X + Y * Y;

    public readonly Float2 NormalizedOrZero()
    {
        if (LengthSquared <= 0.000001f)
        {
            return default;
        }

        float inverseLength = 1.0f / MathF.Sqrt(LengthSquared);
        return new Float2(X * inverseLength, Y * inverseLength);
    }

    public static float DistanceSquared(Float2 left, Float2 right) =>
        (left - right).LengthSquared;

    public static Float2 operator +(Float2 left, Float2 right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static Float2 operator -(Float2 left, Float2 right) =>
        new(left.X - right.X, left.Y - right.Y);

    public static Float2 operator *(Float2 value, float scalar) =>
        new(value.X * scalar, value.Y * scalar);

    public override readonly string ToString() => $"({X:0.00}, {Y:0.00})";
}

public struct Position
{
    public Position(float x, float y) => Value = new Float2(x, y);

    public Float2 Value;
}

public struct PlayerTag { }
public struct EnemyTag { }
public struct TeammateTag { }
public struct WorldObjectTag { }
public struct PlayerControlled { }
public struct GroundMover { }
public struct Obstacle { }
public struct DeadTag { }

/// <summary>
/// Input and AI produce intents; execution systems consume them.
/// </summary>
public struct MoveIntent
{
    public Float2 Direction;
    public byte IsRequested;
}

public struct AttackIntent
{
    public Entity Target;
    public byte IsRequested;
}

public struct BaseMoveStats
{
    public float Speed;
}

public struct MovementModifiers
{
    public float SpeedMultiplier;
    public float AdditiveSpeed;
    public byte CanMove;

    public static MovementModifiers Identity => new()
    {
        SpeedMultiplier = 1.0f,
        CanMove = 1,
    };
}

public struct ResolvedMoveSpeed
{
    public float Value;
    public byte CanMove;
}

public struct Faction
{
    public int TeamId;
}

public struct Health
{
    public float Current;
    public float Maximum;
}

public struct CombatStats
{
    public float AttackPower;
}

public struct CombatModifiers
{
    public float AttackMultiplier;
    public float FlatAttackBonus;

    public static CombatModifiers Identity => new()
    {
        AttackMultiplier = 1.0f,
    };
}

public enum DamageType : byte
{
    Physical,
    Fire,
    Ice,
}

public struct Resistances
{
    public float Physical;
    public float Fire;
    public float Ice;

    public readonly float Get(DamageType type) => type switch
    {
        DamageType.Physical => Physical,
        DamageType.Fire => Fire,
        DamageType.Ice => Ice,
        _ => 0.0f,
    };
}

public struct ResistanceModifiers
{
    public float Physical;
    public float Fire;
    public float Ice;

    public readonly float Get(DamageType type) => type switch
    {
        DamageType.Physical => Physical,
        DamageType.Fire => Fire,
        DamageType.Ice => Ice,
        _ => 0.0f,
    };
}

public struct CombatLoadout
{
    public int PrimaryAttackId;
}

public struct AttackCooldown
{
    public float RemainingSeconds;
}

public struct ChaseBehavior
{
    public Entity Target;
    public float StopDistance;
    public float AttackDistance;
}

public struct GuardBehavior
{
    public Entity Target;
    public Float2 Home;
    public float GuardRadius;
    public float StopDistance;
    public float AttackDistance;
}

public struct FollowBehavior
{
    public Entity Leader;
    public float DesiredDistance;
}

public struct AssistAttackBehavior
{
    public Entity Target;
    public float AttackDistance;
}

public enum BuffKind : byte
{
    Haste,
    Slow,
    AttackUp,
    PhysicalResistance,
    Rooted,
}

/// <summary>
/// Buff is an entity with independent lifetime instead of a managed object
/// embedded inside every actor.
/// </summary>
public struct BuffEffect
{
    public Entity Owner;
    public BuffKind Kind;
    public float Magnitude;
    public int StackCount;
    public float RemainingSeconds;
}

public struct DamageEvent
{
    public Entity Source;
    public Entity Target;
    public DamageType DamageType;
    public float RawDamage;
    public int EffectId;
}

public struct EffectEvent
{
    public Entity Source;
    public Entity Target;
    public int EffectId;
    public float AppliedDamage;
}
