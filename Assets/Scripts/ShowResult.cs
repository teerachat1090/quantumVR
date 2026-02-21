using UnityEngine;

public class ShowResult : MonoBehaviour
{
    public GameObject card00;
    public GameObject card01;
    public GameObject card10;
    public GameObject card11;

    void OnEnable()
    {
        // GameManager อาจยังไม่พร้อม ใช้ coroutine รอ 1 frame
        StartCoroutine(ShowNextFrame());
    }

    System.Collections.IEnumerator ShowNextFrame()
    {
        yield return null; // รอ 1 frame ให้ Awake ทุกอย่างเสร็จก่อน
        ShowSelectedCard();
    }

    void ShowSelectedCard()
    {
        if (card00 != null) card00.SetActive(false);
        if (card01 != null) card01.SetActive(false);
        if (card10 != null) card10.SetActive(false);
        if (card11 != null) card11.SetActive(false);

        // ตรวจ null ก่อนใช้
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance เป็น null!");
            return;
        }

        string selected = GameManager.Instance.selectedBellState;
        Debug.Log("แสดงการ์ด: " + selected);

        if (selected == "00" && card00 != null) card00.SetActive(true);
        else if (selected == "01" && card01 != null) card01.SetActive(true);
        else if (selected == "10" && card10 != null) card10.SetActive(true);
        else if (selected == "11" && card11 != null) card11.SetActive(true);
        else Debug.LogWarning("ไม่พบการ์ดสำหรับ state: '" + selected + "'");
    }
}