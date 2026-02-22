using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Newtonsoft.Json.Linq;
using TMPro;

public class SequenceManager : MonoBehaviour
{
    private CircuitManager headManager = null;

    private enum SphereType
    {
        BlochSphere, QSphere
    }
    [Header("Sphere setting")]
    [SerializeField]    private SphereType sphereType = SphereType.BlochSphere;
    [SerializeField]    private GameObject sphere = null; //check later: bloch / Q - sphere

    [Header("Display setting")]
    [SerializeField] private GameObject markerPrefab = null;
    [SerializeField] private QuantumUiStatManager uiManager = null;

    [Header("Button attachment")]
    [SerializeField]    private GameObject prevButton = null;
    [SerializeField]    private GameObject nextButton = null;

    [SerializeField]    private GameObject modeButton = null;

    private XRGrabInteractable press = null;
    private bool isBlochSphere;
    private BlochSphere blochSphere = null;
    private int seqIndex = 0, seqAmount = 0;
    private string sequencefile = null;
    private Vector3 currVector = Vector3.up;
    private List<string> gateList = new List<string>();
    private GameObject marker = null;

    private void componentCheck()
    {
        headManager = GetComponent<CircuitManager>();
        if(headManager is null)  Debug.LogWarning("Warning: Manager script is missing!");

        if(modeButton is null)
            Debug.LogWarning("Warning: mode button is missing!" + 
            "This will make mode button accessable during processing which error can occured."); 
        else
        {
            press = modeButton.GetComponentInChildren<XRGrabInteractable>();
            if(press is null)
                Debug.LogWarning("Warning: XRGrabInteractable on mode button is missing!" +
                "This will make mode button accessable during processing which error can occured.");
        }
            

        if(prevButton is null || nextButton is null)
            Debug.LogWarning("Warning: Button for next and/or previous sequence is missing!");
        else
            toggleAnimateButton(false);


        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;
        if(isBlochSphere)   
        {
            blochSphere = sphere.GetComponent<BlochSphere>();
            if(blochSphere is null) Debug.LogWarning("Warning: Sphere model is missing!");
        }
        else    
        {
            //try to get Q-sphere
        }
        
        
        if(uiManager is null)   Debug.LogWarning("Initialize Warning: UI script is missing is missing!");
        else                    Debug.Log("UI stat checking sucessful.");
        

        if(markerPrefab is null) Debug.LogWarning("Warning: Marker is missing!");
        else
        {
            marker = Instantiate(markerPrefab);
            marker.transform.Rotate(new Vector3(60.0f, 90.0f, 0.0f));
            marker.SetActive(false);
            Debug.Log("Marker deployed!");
        }
    }

    void Start()
    {
        componentCheck();
    }

    private void toggleAnimateButton(bool isShow)
    {
        prevButton.SetActive(isShow);
        nextButton.SetActive(isShow); 
    }

    public async Task prepareSequence(string pythonScriptPath, string inputPath, string outputPath)
    {
        press.enabled = false;
        seqIndex = 0;

        CircuitExecutor executor = new CircuitExecutor();

        // set ui to |1>, vector to 1
        
        //await for script
        await Task.Run(() => executor.PrepareThenRunQiskit(pythonScriptPath, inputPath, outputPath));
        
        sequencefile = outputPath;
        uiManager.ShowBlochResultByIndex(outputPath, seqIndex);
        seqAmount = uiManager.getSequenceAmount(outputPath);
        blochSphere.AnimateToStateDirectly(currVector);

        // REMINDER: only for bloch sphere (index = 0)
        gateList = headManager.GetGateAsStringList(0);
        int i=0;
        foreach(string gate in gateList)
        {
            Debug.Log($"{i}: {gate}");
            i++;
        }

        press.enabled = true;
        toggleAnimateButton(true);
        Debug.Log("Initialize finished!");
    }

    public void backtoNormal()
    {
        toggleAnimateButton(false);
        uiManager.ShowBlochResultByIndex(sequencefile, seqAmount-1);
        marker.SetActive(false);
        Debug.Log("Reset to Normal mode");
    }

    private void updateVector(bool isInverse)
    {
        if(seqIndex == 0)
        {
            currVector = Vector3.up;
            return;
        }
        CircuitExecutor executor = new CircuitExecutor();
        string targetGate = gateList[(seqIndex - 1) + (isInverse ? 1: 0)];
        Debug.Log($"At {seqIndex} do gate: {targetGate}");
        currVector = executor.DoRotate(currVector, targetGate, isInverse);
    }

    private void UpdateMarker()
    {
        if(seqIndex == 0) 
        {
            marker.SetActive(false);
            return;
        }

        Vector3 targetPosition = headManager.GetNthGatePosInQubit(0, seqIndex);

        marker.transform.position = targetPosition;
        marker.SetActive(true);
    }

    public void setPrevSeqence()
    {
        if(seqIndex == 0) return;
        seqIndex--;
        uiManager.ShowBlochResultByIndex(sequencefile, seqIndex);
        updateVector(true);
        blochSphere.AnimateToStateDirectly(currVector);
        UpdateMarker();
    }

    public void setNextSeqence()
    {
        if(seqIndex == seqAmount - 1) return;
        seqIndex++;
        uiManager.ShowBlochResultByIndex(sequencefile, seqIndex);
        updateVector(false);
        blochSphere.AnimateToStateDirectly(currVector);
        UpdateMarker();
    }
}
