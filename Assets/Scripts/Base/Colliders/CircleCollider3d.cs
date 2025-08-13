using UnityEngine;

public class CircleCollider3d : ColliderBase
{
    [SerializeField]
    private float radius =0.5f;

    public float Radius
    {
        set => radius = value;
        get => radius;
    }

    protected override void OnDrawGizmosSelected()
    {
        // base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        var sphereScale = transform.lossyScale;
        var scale = radius * Mathf.Max(sphereScale.x, sphereScale.y, sphereScale.z);
        var pos = transform.position ;
        Gizmos.DrawWireSphere(pos, scale);
        Gizmos.color = Color.white;
        Gizmos.matrix = Matrix4x4.identity;
    }
}