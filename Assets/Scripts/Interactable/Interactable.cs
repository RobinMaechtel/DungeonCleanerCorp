using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField]
    [Tooltip("What should the player see as the default interaction label, e.g. 'Use PC'")]
    private string promptText = "Interact";

    public virtual string GetPromptText()
    {
        return promptText;
    }

    /// <summary>
    /// Called when the player presses the interact key while this is the current target.
    /// </summary>
    public abstract void Interact(Interactor interactor);

    /// <summary>
    /// Called when player starts looking at this object.
    /// </summary>
    public virtual void OnFocusGained(Interactor interactor) { }

    /// <summary>
    /// Called when player stops looking at this object.
    /// </summary>
    public virtual void OnFocusLost(Interactor interactor) { }
}
