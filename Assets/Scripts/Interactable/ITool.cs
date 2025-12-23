using UnityEngine;

public interface ITool
{
    void Equip(ToolContext context);
    void Unequip(ToolContext context);

    void Primary(ToolContext context);
    void Secondary(ToolContext context);
}

public readonly struct ToolContext
{
    public readonly Transform Owner;
    public readonly Camera Camera;
    public readonly Transform ToolSocket;
    public readonly LayerMask HitMask;

    public ToolContext(Transform owner, Camera camera, Transform toolSocket, LayerMask hitMask)
    {
        Owner = owner;
        Camera = camera;
        ToolSocket = toolSocket;
        HitMask = hitMask;
    }
}
