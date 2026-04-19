using UnityEngine;
using UnityEngine.EventSystems;

public class SampleScript : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Controller hovering over: " + gameObject.name);
        // Add your hover logic here (e.g., scale up, highlight)
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Controller stopped hovering: " + gameObject.name);
        // Reset hover effects here
    }

    public void wjenClick() => Debug.Log("button has clicked");
}
