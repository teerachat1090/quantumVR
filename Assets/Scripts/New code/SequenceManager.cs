using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Newtonsoft.Json.Linq;

public class SequenceManager : MonoBehaviour
{
    private CircuitManager headManager = null;

    [SerializeField] private GameObject canvas = null;

    private enum SphereType
    {
        BlochSphere, QSphere
    }
    [SerializeField]    private SphereType sphereType = SphereType.BlochSphere;
    [SerializeField]    private GameObject sphere = null; //check later: bloch / Q - sphere

    [SerializeField]    private GameObject prevButton = null;
    [SerializeField]    private GameObject nextButton = null;

    [SerializeField]    private GameObject modeButton = null;

    private XRGrabInteractable press = null;
    private bool isBlochSphere;
    private BlochSphere blochSphere = null;
    private QuantumUiStatManager uiManager = null;
    private int seqIndex = 0, seqAmount = 0;
    private string sequencefile = null;
    private Vector3 currVector = Vector3.up;
    private List<string> gateList = new List<string>();

    void Start()
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
        
        
        if(canvas is null) Debug.LogWarning("Warning: UI canvas is missing!");
        else
        {
            uiManager = canvas.GetComponent<QuantumUiStatManager>();
            if(uiManager is null) Debug.LogWarning("Initialize Warning: UI script is missing is missing!");
            else Debug.Log("UI stat checking sucessful.");
        }
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

        //only for bloch sphere
        gateList = headManager.GetGateList();
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

    public void setPrevSeqence()
    {
        if(seqIndex == 0) return;
        seqIndex--;
        uiManager.ShowBlochResultByIndex(sequencefile, seqIndex);
        updateVector(true);
        blochSphere.AnimateToStateDirectly(currVector);
    }

    public void setNextSeqence()
    {
        if(seqIndex == seqAmount - 1) return;
        seqIndex++;
        uiManager.ShowBlochResultByIndex(sequencefile, seqIndex);
        updateVector(false);
        blochSphere.AnimateToStateDirectly(currVector);
    }
}
