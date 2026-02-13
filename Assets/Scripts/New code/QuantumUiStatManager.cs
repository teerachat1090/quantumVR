using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;

public class QuantumUiStatManager : MonoBehaviour
{
    [Header("UI Option")]
    [SerializeField]    private bool isBlochSphere = true;

    [Header("RequireScript")]
    [SerializeField] private ProbHistogram hist;

    [Header("Canvas")]
    [SerializeField]    private Canvas canvas = null;
    [SerializeField]    private RectTransform backgroundImage = null;

    [Header("Bloch UI")]
    [SerializeField]    private TMP_Text ket0_real_Text = null;
    [SerializeField]    private TMP_Text ket0_imag_Text = null;
    [SerializeField]    private TMP_Text ket1_real_Text = null;
    [SerializeField]    private TMP_Text ket1_imag_Text = null;

    //[Header("Background Image")]

    //more variable for Q-sphere

    // 1:2:1:2:...:1
    // space -> 1, br -> 2

    // have list of bar: (val, image)
    // update: same amount-change val, diff amount-empty and re-edit
    // **when destroy -> we need to destroy GameObject (Image)

    public bool isBlochTextMeshReady()
    {
        bool flag = (ket0_real_Text is not null) && (ket0_imag_Text is not null) && 
                    (ket1_real_Text is not null) && (ket1_imag_Text is not null);
        return flag;
    }

    private JArray getStatFromJson(string jsonOutputPath)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonOutputPath);
            JObject jsondata = JObject.Parse(jsonString);
            JArray stats = (JArray)jsondata["state"];
            return stats;
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarning($"Error: The file '{jsonOutputPath}' was not found.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error occurred: {ex.Message}");
        }

        return null;
    }

    private List<QubitStat> GetJsonBlochData(JArray stats)
    {
        Debug.Log($"Input type: {stats.Type}");
        try
        {
            List<QubitStat> statList = new List<QubitStat>();

            foreach (JObject item in stats)
            {
                QubitStat qStat = new QubitStat(
                    (int)item["value"],
                    (double)item["real_part"],
                    (double)item["imag_part"],
                    (double)item["prob"]
                );

                statList.Add(qStat);
            }

            return statList;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error occurred: {ex.Message}");
        }

        return null;
    }

    // 1 space for real, 2 spaces for imag
    private void AssignBlochValueToTextMesh(List<QubitStat> blochStat)
    {
        if(blochStat is null)
        {
            Debug.LogWarning("Warning: Error occur when process json object.");
            return;
        }
        var ket0 = blochStat.FirstOrDefault(s => s.val == 0);
        var ket1 = blochStat.FirstOrDefault(s => s.val == 1);

        double ket0_real = ket0.realPart;
        string ket0_real_str = ((ket0_real < 0) ? "- " : "") + math.abs(ket0_real).ToString("F3");
        ket0_real_Text.SetText(ket0_real_str);

        double ket0_imag = ket0.imagPart;
        string ket0_imag_str = ((ket0_imag < 0) ? "-  " : "+  ") + math.abs(ket0_imag).ToString("F3");
        ket0_imag_Text.SetText(ket0_imag_str);

        double ket1_real = ket1.realPart;
        string ket1_real_str = ((ket1_real < 0) ? "- " : "") + math.abs(ket1_real).ToString("F3");
        ket1_real_Text.SetText(ket1_real_str);

        double ket1_imag = ket1.imagPart;
        string ket1_imag_str = ((ket1_imag < 0) ? "-  " : "+  ") + math.abs(ket1_imag).ToString("F3");
        ket1_imag_Text.SetText(ket1_imag_str);
    } 

    public void ShowBlochResult(string jsonOutputPath)
    {
        if (isBlochSphere)
        {
            JArray JsonStats = getStatFromJson(jsonOutputPath);
            List<QubitStat> stat = GetJsonBlochData(JsonStats);
            if(stat is null || stat.Count == 0)
            {
                Debug.LogWarning("Error: Cannot get data from json file!");
                return;
            }
            //Debug.Log("Stat Not null");
            if (!isBlochTextMeshReady())
            {
                Debug.LogWarning("Error: Some TextMesh for Bloch Sphere is missing!");
                return;
            }

            AssignBlochValueToTextMesh(stat);

            Debug.Log("Updating Histrogram");
            hist.UpdateHist(stat);
            //adjust histogram
        }

        return;
    }

    private JArray GetStatFromJsonByIndex(string jsonSequenceOutputPath, int index)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonSequenceOutputPath);
            JObject jsondata = JObject.Parse(jsonString);
            JArray statsList = (JArray)jsondata["resultList"];
            var statSequence = statsList.FirstOrDefault(item => item.Value<int>("sequenceIndex") == index);
            return statSequence["state"] as JArray;
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarning($"Error: The file '{jsonSequenceOutputPath}' was not found.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error occurred: {ex.Message}");
        }

        return null;
    }

    public void ShowBlochResultByIndex(string jsonSequenceOutputPath, int index)
    {
        JArray jsonStat = GetStatFromJsonByIndex(jsonSequenceOutputPath, index);
        List<QubitStat> stat = GetJsonBlochData(jsonStat);
        if(stat is null || stat.Count == 0)
        {
            Debug.LogWarning("Error: Cannot get data from json file!");
            return;
        }
        //Debug.Log("Stat Not null");
        if (!isBlochTextMeshReady())
        {
            Debug.LogWarning("Error: Some TextMesh for Bloch Sphere is missing!");
            return;
        
        }
        AssignBlochValueToTextMesh(stat);
        hist.UpdateHist(stat);
    }

    public int getSequenceAmount(string jsonSequenceOutputPath)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonSequenceOutputPath);
            JObject jsondata = JObject.Parse(jsonString);
            int seqAmount = (int)jsondata["gateAmount"] + 1;
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

    public class QubitStat
    {
        public int val;
        public double realPart, imagPart;
        public double prob;

        public QubitStat(int Val, double Real, double Imag, double Prob)
        {
            val = Val;  realPart = Real;    imagPart = Imag;    prob = Prob;
        }
    }

    // 0 1
    // 00 01 10 11
    // 000 001 010 011 100 101 110 111
    // 0000 0001 0010 0011 0100 0101 0110 0111 1000 1001 1010 1011 1100 1101 1110 1111
}
