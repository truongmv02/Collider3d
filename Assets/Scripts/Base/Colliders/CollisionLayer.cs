using System;

[Flags]
public enum CollisionLayer
{
    None = 0,
    Player = 1 << 0,
    Enemy = 1 << 1,
    Everyone = ~0
}