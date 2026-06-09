using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NodeClickHandler : MonoBehaviour
{
    public int nodeIndex;

    void Start()
    {
        var interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelected);
        }
        else
        {
            Debug.LogWarning("No XRSimpleInteractable on " + gameObject.name);
        }
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        Debug.Log("XR Selected node: " + nodeIndex);
        GraphManager.Instance?.OnNodeClicked(nodeIndex);
    }
}