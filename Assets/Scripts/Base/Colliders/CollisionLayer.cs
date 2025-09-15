using System;

[Flags]
public enum CollisionLayer
{
    None = 0,
    Player = 1 << 0,
    Enemy = 1 << 1,
    PlayerBullet = 1 << 2,
    Wall = 1 << 3,
    Everyone = ~0
}