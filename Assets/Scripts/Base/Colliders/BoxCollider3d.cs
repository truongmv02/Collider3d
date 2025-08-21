using System.Runtime.CompilerServices;
using UnityEngine;

public class BoxCollider3d : ColliderBase
{
    [SerializeField]
    protected Vector3 size = Vector3.one;

    public Vector3 Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => size = value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => size;
    }

    
    public Vector3 HalfSizeReal => size * 0.5f;
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }
    
}