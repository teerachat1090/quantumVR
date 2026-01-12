using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class ChanegeColorWhenHover : MonoBehaviour
{
    public Color hoverColor = Color.green;
    public Color normalColor = Color.white;

    private XRRayInteractor rayInteractor;
    private XRInteractorLineVisual lineVisual;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if(grabInteractable == null)
        {
            Debug.LogWarning("XRGrabInteractable component is missing!");
            return;
        }
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable()
    {
        if(grabInteractable == null)
        {
            Debug.LogWarning("XRGrabInteractable component is missing!");
            return;
        }
        grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        grabInteractable.hoverExited.RemoveListener(OnHoverExited);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        rayInteractor = args.interactorObject as XRRayInteractor;
        if(rayInteractor == null) {
            Debug.LogWarning("rayInteractor is missing");
            return;
        }
        lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
        if(lineVisual == null)
        {
            Debug.LogWarning("lineVisual is missing");
            return;
        }

        lineVisual.validColorGradient = CreateGradient(hoverColor);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (lineVisual == null) return;

        lineVisual.validColorGradient = CreateGradient(normalColor);
    }

    private Gradient CreateGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { //all in "color" from 0.0 to 1.0
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f) 
            },
            new GradientAlphaKey[] { //all in alpha 1.0 from 0.0 to 1.0
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f) 
            }
        );
        return gradient;
    }
}
