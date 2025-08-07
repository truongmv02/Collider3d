using UnityEngine;

public class BoxCollider3d : ColliderBase
{
    [SerializeField]
    protected Vector3 size = Vector3.one;

    public Vector3 Size
    {
        set => size = value;
        get => size;
    }

    public Vector3 HalfSizeReal => size * 0.5f;
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.DrawWireCube(center, size);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }
    
}