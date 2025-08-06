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

    public float RealRadius
    {
        get
        {
            var lossyScale = transform.lossyScale;
            return radius * Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
        }
    }

    
    private float GetMaxScale(Vector3 scale)
    {
        return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
    }
    
    protected override void OnDrawGizmosSelected()
    {
        // base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        var sphereScale = transform.lossyScale;
        var scale = radius * Mathf.Max(sphereScale.x, sphereScale.y, sphereScale.z);
        var pos = transform.position + transform.rotation * Vector3.Scale(center, sphereScale);
        Gizmos.DrawWireSphere(pos, scale);
        Gizmos.color = Color.white;
        Gizmos.matrix = Matrix4x4.identity;
    }
}