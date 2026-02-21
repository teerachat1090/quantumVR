using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public string selectedBellState = "00";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetBellState(string state)
    {
        selectedBellState = state;
        Debug.Log("เลือก: " + state);
    }
}