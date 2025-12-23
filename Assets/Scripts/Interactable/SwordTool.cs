using UnityEngine;

public class SwordTool : ToolBase
{
    [Header("Hit Scan")]
    [SerializeField] private float range = 2.2f;
    [SerializeField] private float radius = 0.25f;

    [Header("Cut Force")]
    [SerializeField] private float impulse = 3.5f;

    protected override void OnPrimary(ToolContext context)
    {
        var ray = context.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.SphereCast(ray, radius, out var hit, range, context.HitMask, QueryTriggerInteraction.Ignore))
        {
            // Limb-level hit first
            var limb = hit.collider.GetComponentInParent<CuttableLimbHitbox>();
            if (limb != null)
            {
                limb.CutOff(hit.point, ray.direction, impulse);
                return;
            }

            // Or generic cuttable
            var cuttable = hit.collider.GetComponentInParent<ICuttable>();
            if (cuttable != null)
            {
                cuttable.Cut(hit.point, ray.direction);
            }
        }
    }

    protected override void OnSecondary(ToolContext context)
    {
        // Secondary could become "heavy swing" later
        // For now: same as primary but maybe bigger radius/range
        OnPrimary(context);
    }
}

public interface ICuttable
{
    void Cut(Vector3 hitPoint, Vector3 hitDirection);
}
