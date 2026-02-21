using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    public GameObject sec4;
    public GameObject selectedCard;
    public GameObject step5;

    void Awake()
    {
        Instance = this;
    }

    // เรียกจากปุ่ม Back ใน Step5
    public void BackFromStep5()
    {
        step5.SetActive(false);
        sec4.SetActive(true);         // เปิด parent ก่อน
        selectedCard.SetActive(true); // แล้วค่อยเปิด child
    }
}