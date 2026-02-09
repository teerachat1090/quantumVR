using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;

public class QuantumUiStatManager : MonoBehaviour
{
    [Header("UI Option")]
    [SerializeField]    private bool isBlochSphere = true;

    [Header("Bloch UI")]
    [SerializeField]    private TMP_Text ket0_real_Text = null;
    [SerializeField]    private TMP_Text ket0_imag_Text = null;
    [SerializeField]    private TMP_Text ket1_real_Text = null;
    [SerializeField]    private TMP_Text ket1_imag_Text = null;

    //more variable for Q-sphere

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool isBlochTextMeshReady()
    {
        bool flag = (ket0_real_Text is not null) && (ket0_imag_Text is not null) && 
                    (ket1_real_Text is not null) && (ket1_imag_Text is not null);
        return flag;
    }

    private BlochQubitStat GetJsonData(string jsonOutputPath)
    {
        try
        {
            string jsonString = File.ReadAllText(jsonOutputPath);
            JObject jsondata = JObject.Parse(jsonString);

            var blochStat = new BlochQubitStat(
                (double)jsondata["0_real"],(double)jsondata["0_imag"],(double)jsondata["0_prob"],
                (double)jsondata["1_real"],(double)jsondata["1_imag"],(double)jsondata["1_prob"]);

            return blochStat;
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

    // 1 space for real, 2 spaces for imag
    private void AssignValueToTextMesh(BlochQubitStat blochStat)
    {
        double ket0_real = blochStat.ket0_real;
        string ket0_real_str = (ket0_real < 0) ? "- " : "" ;
        ket0_real_Text.text = ket0_real_str + blochStat.ket0_real.ToString("F3");

        double ket0_imag = blochStat.ket0_imag;
        string ket0_imag_str = (ket0_imag < 0) ? "-  " : "+  " ;
        ket0_imag_Text.text = ket0_imag_str + blochStat.ket0_imag.ToString("F3");

        double ket1_real = blochStat.ket1_real;
        string ket1_real_str = (ket1_real < 0) ? "- " : "" ;
        ket1_real_Text.text = ket1_real_str + blochStat.ket1_real.ToString("F3");

        double ket1_imag = blochStat.ket1_imag;
        string ket1_imag_str = (ket1_imag < 0) ? "-  " : "+  " ;
        ket1_imag_Text.text = ket1_imag_str + blochStat.ket1_imag.ToString("F3");
    } 

    public void ShowBlochResult(string jsonOutputPath)
    {
        if (isBlochSphere)
        {
            BlochQubitStat stat = GetJsonData(jsonOutputPath);
            if(stat is null)
            {
                Debug.LogWarning("Error: Cannot get data from json file!");
                return;
            }

            if (!isBlochTextMeshReady())
            {
                Debug.LogWarning("Error: Some TextMesh for Bloch Sphere is missing!");
                return;
            }

            AssignValueToTextMesh(stat);
        }
    }

    public class BlochQubitStat
    {
        public double ket0_real {get;  set;} 
        public double ket0_imag{ get; set;} 
        public double ket0_prob { get; set;}
        public double ket1_real { get; set;}
        public double ket1_imag { get; set;}
        public double ket1_prob{ get; set;}

        public BlochQubitStat(  double _ket0_real, double _ket0_imag, double _ket0_prob, 
                                double _ket1_real, double _ket1_imag, double _ket1_prob)
        {
            ket0_real = _ket0_real;     ket0_imag = _ket0_imag;     ket1_prob = _ket0_prob;
            ket1_real = _ket1_real;     ket1_imag = _ket1_imag;     ket1_prob = _ket1_prob;
        }
    }
}
