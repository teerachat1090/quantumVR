using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using QubitStat = FileManager.QubitStat;

public class QuantumUiStatManager : MonoBehaviour
{
    [Header("UI Option")]
    [SerializeField]    public bool isBlochSphere = true;

    [Header("RequireScript")]
    [SerializeField] private ProbHistogram hist;

    [Header("Canvas")]
    [SerializeField]    private Canvas canvas = null;
    [SerializeField]    private RectTransform backgroundImage = null;

    [Header("Statevector result UI")]
    [SerializeField]    private StateVectorDisplay stateVectorDisplay = null;

    private FileManager fileManager = new FileManager();

    public int seqIndex = -1;

    //[Header("Background Image")]

    //more variable for Q-sphere

    // 1:2:1:2:...:1
    // space -> 1, br -> 2

    // have list of bar: (val, image)
    // update: same amount-change val, diff amount-empty and re-edit
    // **when destroy -> we need to destroy GameObject (Image)
    
    public void DisplayResult(bool blochSphereFlag)
    {
        
        List<QubitStat> stat = fileManager.GetStatFromJsonData(blochSphereFlag);
        if(stat is null || stat.Count == 0)
        {
            Debug.LogWarning("Error: Cannot get data from json file!");
            return;
        }

        Debug.Log("Updating Histrogram");
        hist.UpdateHist(stat);
        //adjust histogram
        
        return;
    }

    public void CloseStateVector()
    {
        stateVectorDisplay.gameObject.SetActive(false);
    }

    public void DisplayStateVectorByValue(int value)
    {
        if (stateVectorDisplay == null)
        {
            Debug.LogWarning("Warning: State vector display is missing.");
            return;
        }

        //get stat of statevector by its value
        QubitStat stat = fileManager.GetStatFromJsonByValue(isBlochSphere, value, seqIndex);
        stateVectorDisplay.AssignInfomation(stat.realPart, stat.imagPart, stat.val, stat.prob, stat.phase);
        stateVectorDisplay.gameObject.SetActive(true);
    }

    public void ShowResultByIndex(bool blochSphereFlag, int index)
    {
        List<QubitStat> stat = fileManager.GetStatFromJsonByIndex(blochSphereFlag, index);

        if(stat is null || stat.Count == 0)
        {
            Debug.LogWarning("Error: Cannot get data from json file!");
            return;
        }
        seqIndex = index;
        hist.UpdateHist(stat);
    }

    public int getSequenceAmount(string jsonSequenceOutputPath)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonSequenceOutputPath);
            JObject jsondata = JObject.Parse(jsonString);
            int seqAmount = (int)jsondata["stepAmount"] + 1;
            return seqAmount;
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarning($"Error: The file '{jsonSequenceOutputPath}' was not found.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error occurred: {ex.Message}");
        }
        return 0;
    }
}
