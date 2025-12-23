using UnityEngine;

public class CuttableLimbHitbox : MonoBehaviour
{
    public DismemberLimb limb;

    public void CutOff(Vector3 hitPoint, Vector3 direction, float impulse)
    {
        // This is the bone that starts the limb chain (e.g. upper arm / thigh / head)

        Transform limbBoneRoot = limb.joint.transform;
        Rigidbody limbRootRb = limbBoneRoot.GetComponent<Rigidbody>();

        // 1) Create a new root object for the detached part
        GameObject limbRootGO = new GameObject(limb.limbName + "_ItemRoot");
        limbRootGO.transform.position = limbBoneRoot.position;
        limbRootGO.transform.rotation = limbBoneRoot.rotation;

        // 2) Reparent the limb bone hierarchy under the new root
        if (limb.detachHierarchy)
        {
            limbBoneRoot.SetParent(limbRootGO.transform, true);
        }

        // 3) Reparent the visual SkinnedMeshRenderers and fix their settings
        if (limb.limbRenderers != null)
        {
            foreach (var smr in limb.limbRenderers)
            {
                if (smr == null) continue;

                // Make the renderer a child of the new item root
                smr.transform.SetParent(limbRootGO.transform, true);

                // IMPORTANT: make sure the SMR uses the limb root bone now
                smr.rootBone = limbBoneRoot;

                // Helpful while debugging: prevent over-aggressive culling
                smr.updateWhenOffscreen = true;

                // Optional: force a simple, safe bounding box around the limb
                // (tweak size if cubes are larger/smaller)
                var bounds = smr.localBounds;
                bounds.center = Vector3.zero;
                bounds.extents = Vector3.one * 0.5f; // half-unit cube
                smr.localBounds = bounds;
            }
        }

        // 4) Break the joint
        Destroy(limb.joint);
        limb.joint = null;

        // 5) Let physics handle the detached limb
        if (limbRootRb != null)
        {
            limbRootRb.isKinematic = false;
            limbRootRb.detectCollisions = true;

            if (limb.impulseStrength > 0f)
            {
                limbRootRb.AddExplosionForce(
                    limb.impulseStrength,
                    limbBoneRoot.position + Vector3.up * 0.1f,
                    1f,
                    0.1f,
                    ForceMode.Impulse);
            }
        }

        // 6) Attach item script to the new root so we can treat it as loot later
        var item = limbRootGO.AddComponent<CorpsePartItem>();
        item.partId = string.IsNullOrEmpty(limb.partId) ? limb.limbName.ToLowerInvariant() : limb.partId;
        item.displayName = limb.displayName;
        item.creatureTypeId = "Orc"; // or from a field on RagdollDismemberDebug if you want it generic
        item.partType = limb.partType;
        item.baseSellValue = limb.baseSellValue;
        item.weightKg = limb.weightKg;
        item.isFlesh = true; // skeletons etc. can later set this to false

        Debug.Log($"RagdollDismemberDebug: Cut limb '{limb.limbName}' and spawned item root '{limbRootGO.name}'.");
    }
}
