using UnityEngine;

public class BackToSelectedCard : MonoBehaviour
{
    public GameObject step5;
    public GameObject sec4;
    public GameObject selectedCard;

    public void OnBack()
    {
        step5.SetActive(false);
        sec4.SetActive(true);
        selectedCard.SetActive(true);

        Debug.Log("Sec4 active: " + sec4.activeSelf);
        Debug.Log("SelectedCard active: " + selectedCard.activeSelf);
    }
}