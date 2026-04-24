using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GateTooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;

    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (tooltipPanel == null)
        {
            Debug.LogWarning("tooltipPanel not assigned on: " + gameObject.name);
            return;
        }
        tooltipPanel.SetActive(true);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (grabInteractable == null) return;
        grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        grabInteractable.hoverExited.RemoveListener(OnHoverExited);
    }
}