using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using QubitStat = QuantumUiStatManager.QubitStat;

public class ProbHistogram : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField]    private Canvas canvas = null;
    [SerializeField]    private RectTransform backgroundImage = null;
    [SerializeField]    private GameObject histPrefab = null;


    private GameObject histParent;
    private string histParentName = "Histograms";

    // child name in prefab
    private List<GameObject> histList = new List<GameObject>();
    private List<double> probStateList = new List<double>();


    void Start()
    {
        histParent = new GameObject(histParentName);
        histParent.transform.SetParent(canvas.transform, false);
    }

    public void ClearHistogram()
    {
        if(histList.Count == 0) return;

        foreach(GameObject obj in histList) Destroy(obj);
        histList.Clear();
    }

    public void RebuildHistogram(int numState)
    {
        if(histList.Count > 0) ClearHistogram();

        // 1:2:1:2:1:2:...:1
        float imageWidth = backgroundImage.rect.width;
        float unitRatio = imageWidth/(3*numState + 1);
        float lengthCount = -imageWidth/2 + unitRatio*2;
        
        for(int i=0; i<numState; i++)
        {
            GameObject sample = Instantiate(histPrefab, histParent.transform);
            histList.Add(sample);
            
            EditingHistChild editor = sample.GetComponent<EditingHistChild>();
            if(editor is null)
            {
                Debug.LogWarning("Warning: editor script is missing from histogram prefab!");
                continue;
            }
            editor.setHist(lengthCount, unitRatio*2);
            editor.setState(i);
            
            lengthCount += unitRatio*3;
        }
        
        return;
    } 

    public void UpdateProbList(List<QubitStat> stats)
    {
        // update prob list
        probStateList.Clear();
        foreach(QubitStat qubitStat in stats)
        {
            probStateList.Add(qubitStat.prob);
        }
    }

    private void UpdateProbToHist(int numState)
    {
        for(int i=0; i<numState; i++)
        {
            GameObject hist = histList[i];
            EditingHistChild editor = hist.GetComponent<EditingHistChild>();
            if(editor is null)
            {
                Debug.LogWarning("Warning: editor script is missing from histogram prefab!");
                continue;
            }

            editor.setProb(probStateList[i]);
        }
    }

    public void UpdateHist(List<QubitStat> stats)
    {
        if(histPrefab is null)
        {
            Debug.LogWarning("Warning: Prefab for histogram is missing!");
            return;
        }

        // count number of stat
        int statCount = stats.Count;
        bool rebuildFlag = probStateList.Count != statCount;

        UpdateProbList(stats);

        if(rebuildFlag)
        {
            RebuildHistogram(statCount);
        }
        
        UpdateProbToHist(statCount);
        // assign value to hist
        
    }
}
