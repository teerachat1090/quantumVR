using UnityEngine;

public class ButtonBellState : MonoBehaviour
{
    // เรียกจากปุ่ม 00, 01, 10, 11
    public void SelectState00()
    {
        GameManager.Instance.SetBellState("00");
    }
    
    public void SelectState01()
    {
        GameManager.Instance.SetBellState("01");
    }
    
    public void SelectState10()
    {
        GameManager.Instance.SetBellState("10");
    }
    
    public void SelectState11()
    {
        GameManager.Instance.SetBellState("11");
    }
}