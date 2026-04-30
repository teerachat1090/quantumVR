using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Newtonsoft.Json.Linq;
using TMPro;
using SphereType = CircuitManager.SphereType;
using Unity.Mathematics;

public class SequenceManager : MonoBehaviour
{
    private CircuitManager headManager = null;

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
    private QSphere qSphere = null;
    private int seqIndex = 0, seqAmount = 0;
    private Vector3 currBlochVector = Vector3.up;
    private List<QuantumGate> blochGateList = new List<QuantumGate>();
    private List<int> columnList = new List<int>();
    private GameObject marker = null;
    private float markerYScale = 0.0f; 
    private FileManager fileManager = new FileManager();
    private SocketsManager socketsManager = null;

    private void componentCheck()
    {
        headManager = GetComponent<CircuitManager>();
        if(headManager is null)  Debug.LogWarning("Warning: Manager script is missing!");
        else socketsManager = headManager.GetSocketsManager();

        if(modeButton == null)
            Debug.LogWarning("Warning: mode button is missing!" + 
            "This will make mode button accessable during processing which error can occured."); 
        else
        {
            press = modeButton.GetComponentInChildren<XRGrabInteractable>();
            if(press is null)
                Debug.LogWarning("Warning: XRGrabInteractable on mode button is missing!" +
                "This will make mode button accessable during processing which error can occured.");
        }
            

        if(prevButton == null || nextButton == null)
            Debug.LogWarning("Warning: Button for next and/or previous sequence is missing!");
        else
            toggleAnimateButton(false);


        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;

        string str = (sphere == null) ? "missing" : "present";
        Debug.Log($"sphere object is "+str);
        if(sphere == null)
        {
            Debug.LogWarning("Warning: There's no sphere object.");
        }
        else
        {
            if(isBlochSphere)   
            {
                blochSphere = sphere.GetComponent<BlochSphere>();
                if(blochSphere is null) Debug.LogWarning("Warning: Sphere is missing BlochSphere component!");
            }
            else    
            {
                qSphere = sphere.GetComponent<QSphere>();
                if(qSphere is null) Debug.LogWarning("Warning: Sphere is missing QSphere component!");
            }
        }
        
        if(uiManager is null)   Debug.LogWarning("Initialize Warning: UI script is missing is missing!");
        else                    Debug.Log("UI stat checking sucessful.");
        

        if(markerPrefab == null) Debug.LogWarning("Warning: Marker is missing!");
        else
        {
            marker = Instantiate(markerPrefab, socketsManager.transform.position, quaternion.identity);
            marker.transform.Rotate(new Vector3(60.0f, 90.0f, 0.0f));
            marker.SetActive(false);
            markerYScale = marker.transform.localScale.y;
            Debug.Log("Marker deployed!");
        }
    }

    void Start()
    {
        componentCheck();
    }

    public void ReScaleMarker(float columnSize)
    {
        Vector3 markerScale = marker.transform.localScale;
        marker.transform.localScale = new Vector3(markerScale.x,markerYScale*columnSize,markerScale.z);
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
        
        uiManager.ShowResultByIndex(isBlochSphere, seqIndex);
        seqAmount = uiManager.getSequenceAmount(outputPath);

        if(isBlochSphere)   {
            blochSphere.AnimateToStateDirectly(currBlochVector);
            blochGateList = socketsManager.GetGateListByRow(0);
        }
        else
        {
            qSphere.UpdateFromJson(isSequence: true);
        }

        columnList = fileManager.GetColumnListFromJson(isBlochSphere);
        ReScaleMarker(socketsManager.GetColumnSize());
        press.enabled = true;
        toggleAnimateButton(true);
        Debug.Log("Initialize finished!");
    }

    public void backtoNormal()
    {
        toggleAnimateButton(false);
        uiManager.ShowResultByIndex(isBlochSphere, seqAmount-1);
        uiManager.seqIndex = -1;
        marker.SetActive(false);
        Debug.Log("Reset to Normal mode");
    }

    private void updateBlochVector(bool isInverse)
    {
        if(seqIndex == 0)
        {
            currBlochVector = Vector3.up;
            return;
        }
        CircuitExecutor executor = new CircuitExecutor();
        QuantumGate targetGate = blochGateList[(seqIndex - 1) + (isInverse ? 1: 0)];
        Debug.Log($"At {seqIndex} do gate: {targetGate}");
        currBlochVector = executor.DoRotate(currBlochVector, targetGate, isInverse);
    }

    private void UpdateMarker()
    {
        if(seqIndex == 0) 
        {
            marker.SetActive(false);
            return;
        }

        int targetColumn = columnList[seqIndex-1];
        float columnPos = socketsManager.GetColumnPosition(targetColumn);
        Vector3 markerPos = marker.transform.position;
        marker.transform.position = new Vector3(markerPos.x, markerPos.y, columnPos);
        marker.SetActive(true);
    }

    public void setPrevSeqence()
    {
        if(seqIndex == 0) return;
        seqIndex--;
        uiManager.ShowResultByIndex(isBlochSphere, seqIndex);

        if(isBlochSphere) {
            updateBlochVector(true);
            blochSphere.AnimateToStateDirectly(currBlochVector);
        }
        else
        {
            qSphere.UpdateFromJson(true, seqIndex);
        }

        UpdateMarker();
    }

    public void setNextSeqence()
    {
        if(seqIndex == seqAmount - 1) return;
        seqIndex++;
        uiManager.ShowResultByIndex(isBlochSphere, seqIndex);
        
        if(isBlochSphere) {
            updateBlochVector(false);
            blochSphere.AnimateToStateDirectly(currBlochVector);
        }
        else
        {
            //update Q-sphere by index
            qSphere.UpdateFromJson(true, seqIndex);
        }

        UpdateMarker();
    }
}
