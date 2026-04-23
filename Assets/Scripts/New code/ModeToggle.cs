using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeToggle : MonoBehaviour
{
    public void GoToInverseMode()
    {
        SceneManager.LoadScene("Chapter1_Inverse");
    }

    public void GoToNormalMode()
    {
        SceneManager.LoadScene("Chapter1");
    }
}