using System.Collections;
using UnityEngine;

public abstract class ToolBase : MonoBehaviour, ITool
{
    [Header("Timing")]
    [SerializeField] protected float cooldown = 0.25f;

    [Header("Debug Swing Anim")]
    [SerializeField] protected Transform animRoot; // set to this transform or a child
    [SerializeField] protected float swingDuration = 0.12f;
    [SerializeField] protected Vector3 swingEuler = new Vector3(-65f, 25f, 0f);

    protected float lastUseTime = -999f;
    protected Coroutine swingCo;
    protected Quaternion baseLocalRot;

    public virtual void Equip(ToolContext context)
    {
        if (animRoot == null) animRoot = transform;
        baseLocalRot = animRoot.localRotation;
    }

    public virtual void Unequip(ToolContext context)
    {
        if (swingCo != null) StopCoroutine(swingCo);
    }

    public void Primary(ToolContext context)
    {
        if (!CanUse()) return;
        lastUseTime = Time.time;

        PlaySwing();
        OnPrimary(context);
    }

    public void Secondary(ToolContext context)
    {
        if (!CanUse()) return;
        lastUseTime = Time.time;

        PlaySwing();
        OnSecondary(context);
    }

    protected bool CanUse() => Time.time >= lastUseTime + cooldown;

    protected void PlaySwing()
    {
        if (animRoot == null) return;
        if (swingCo != null) StopCoroutine(swingCo);
        swingCo = StartCoroutine(SwingRoutine());
    }

    private IEnumerator SwingRoutine()
    {
        float t = 0f;
        while (t < swingDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.SmoothStep(0f, 1f, t / swingDuration);
            animRoot.localRotation = baseLocalRot * Quaternion.Euler(Vector3.Lerp(Vector3.zero, swingEuler, a));
            yield return null;
        }

        // return quickly
        float back = 0f;
        while (back < swingDuration)
        {
            back += Time.deltaTime;
            float a = Mathf.SmoothStep(0f, 1f, back / swingDuration);
            animRoot.localRotation = Quaternion.Slerp(animRoot.localRotation, baseLocalRot, a);
            yield return null;
        }

        animRoot.localRotation = baseLocalRot;
        swingCo = null;
    }

    protected abstract void OnPrimary(ToolContext context);
    protected abstract void OnSecondary(ToolContext context);
}
