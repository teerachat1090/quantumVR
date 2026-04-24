using UnityEngine;
using TMPro;

public class CBitUi : MonoBehaviour
{
    [SerializeField] TMP_Text bitName;
    [SerializeField] TMP_Text bitValue;

    private int bitIndex = 0;
    private int value = 0;
    
    private string IntToUnderscript(int val)
    {
        if(val == 0) return "\u2080";

        string output = "";    
        string str = val.ToString();
        foreach (char c in str)
        {
            int code = 2080 + (c - '0');
            output += $"\\u{code}";
        }
        
        return output;
    }

    public void SetBitIndex(int index)
    {
        if(bitName == null)
        {
            Debug.LogWarning("Warning: GameObject \"bitName\" is missing.");
            return;
        }

        bitName.text = "b" + IntToUnderscript(index);
    }

    public void SetBitValue(int val)
    {
        if(val != 1 && val != 0)
        {
            Debug.LogWarning($"Warning: Value is invalid ({val})");
            return;
        }

        bitValue.text = (val == 0) ? "0": "1";
    }
}
