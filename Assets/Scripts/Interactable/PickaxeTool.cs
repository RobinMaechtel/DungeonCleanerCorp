using UnityEngine;

public class PickaxeTool : ToolBase
{
    protected override void OnPrimary(ToolContext context)
    {
        // TODO: Raycast, check IMineable, apply mining
        Debug.Log("Pickaxe Primary (mine) - TODO");
    }

    protected override void OnSecondary(ToolContext context)
    {
        // TODO: alt mine / heavy hit
        Debug.Log("Pickaxe Secondary - TODO");
    }
}
