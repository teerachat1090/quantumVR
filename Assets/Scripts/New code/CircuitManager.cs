using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CircuitManager : MonoBehaviour
{
    [SerializeField] private SocketsManager socketsManager = null;
    // use component.getComponentsInChildren<thatComponent>(false, thatList);
    //      to store as List instead of array (no reallocate !) - false = not count inactive child

    private enum SphereType
    {
        BlochSphere, QSphere
    }
    [Header("Sphere setting")]
    [SerializeField] private SphereType sphereType = SphereType.BlochSphere;
    [SerializeField] private GameObject sphere = null; //check later: bloch / Q - sphere
    [SerializeField] private int qubitAmount = 1;

    [Header("Display setting")]
    [SerializeField] private QuantumUiStatManager uiManager = null; //showing result
    [SerializeField] private TMP_Text modeText = null; //display mode

    [Header("Interaction setting")]
    [SerializeField] private GameObject storage = null; //disable grabbable when in animated mode
    [SerializeField] private InteractionLayerMask interactionLayer;

    private XRGrabInteractable[] sourceGates;

    // Folder and file name
    private string pythonScriptFolder = "New code",pythonScriptName = "quantum_circuit.py", pythonAnimateName = "quantum_sequence.py";
    private string mainSciptsPath = Path.Combine(Application.dataPath, "Scripts");
    private string pythonScriptPath;
    
    // can check each one if enable
    private List<QubitCircuit> qubitCircuits;
    private bool isBlochSphere;
    private BlochSphere blochSphere = null;
    private QSphere qSphere = null;
    //private QSphere qSphere = null;
    private SequenceManager sqManager = null;

    private CircuitExecutor executor = new CircuitExecutor();
    private FileManager fileManager = new FileManager();

    public bool isItBlochSphere()
    {
        return isBlochSphere;
    }

    private void ComponentCheck()
    {
        isBlochSphere = (sphereType == SphereType.BlochSphere) ? true : false;
        if(isBlochSphere)   
        {
            blochSphere = sphere.GetComponent<BlochSphere>();
            if(blochSphere is null) Debug.LogWarning("Warning: Sphere model is missing!");
        } else
        {
            qSphere = sphere.GetComponent<QSphere>();
            if(qSphere is null)     Debug.LogWarning("Warning: Sphere model is missing!");          
        }

        if(uiManager is null)   Debug.LogWarning("Initialize Warning: UI script is missing is missing!");
        else                    Debug.Log("UI stat checking sucessful.");
        

        sqManager = GetComponent<SequenceManager>();
        if(sqManager is null) Debug.LogWarning("Warning: Sequence manager component is missing!");

        if(modeText is null) Debug.LogWarning("Warning: Text for showing mode is missing!");

        if(socketsManager is null) Debug.LogWarning("Warning: Socket manager is missing!");

        if(storage is null) Debug.LogWarning("Warning: storage object is missing! Unable to set gate grabbable state!");
        else
        {
            sourceGates = storage.GetComponentsInChildren<XRGrabInteractable>();
            if(sourceGates is null) Debug.LogWarning("Warning: Unable to get component from source gate in storage!");
        }
    }

    void Awake()
    {
        ComponentCheck();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(socketsManager is null) Debug.LogError("socketsManager is missing");

        qubitCircuits = socketsManager.InitSocketPrefabSpawn(isBlochSphere ? 1: qubitAmount);

        if(isBlochSphere is true)   socketsManager.updateCircuitByJson(null, -1, -1, true);

        if(isBlochSphere is false && qSphere is not null)
        {
            qSphere.ChangeQubitAmount(qubitAmount);
        }
    }

    private void updateBlochVectorInstant()
    {
        if(blochSphere is null) {
            Debug.LogWarning("Warning: Sphere model is missing. Unable to animated!");
            return;
        }
        
        List<string> gateList = socketsManager.GetGateAsStringList(0);
        Vector3 resultVector = executor.GetResultBlochVector(gateList);
        blochSphere.AnimateToStateDirectly(resultVector);
    }

    public List<string> GetGateAsStringList(int index)
    {
        return socketsManager.GetGateAsStringList(index);
    }

    private async Task calculateAndUpdateUi(string inputPath, string outputPath)
    {
        await Task.Run(() => executor.PrepareThenRunQiskit(pythonScriptPath, inputPath, outputPath));

        // show value
        if(uiManager is not null)   uiManager.ShowBlochResult(blochSphereFlag: true);
        else                        Debug.LogWarning("Warning: ui script is missing. Unable to show stat!");
    }

    // recalculate everytinm the circuit change
    public void updateOverallCircuit(string circuitJson)
    {
        fileManager.updateJsonInputToFile(circuitJson, isBlochSphere);
        updateBlochVectorInstant();
        
        // calculate value
        pythonScriptPath = Path.Combine(mainSciptsPath, pythonScriptFolder, pythonScriptName);

        fileManager.GetJsonSphereIOPath(isBlochSphere, out string inputPath, out string outputPath, out _);

        _ = calculateAndUpdateUi(inputPath, outputPath);
    }

    // assigned to button
    public void PrepareForAnimation()
    {
        if(sqManager is null)
        {
            Debug.LogWarning("Warning: Sequence manager component is missing!");
            return;
        }

        if(modeText is null)    Debug.LogWarning("Wrning: Text object is missing");
        else                    modeText.SetText("Sequence Mode");

        socketsManager.FreezeGateBlock(true);

        pythonScriptPath = Path.Combine(mainSciptsPath, pythonScriptFolder, pythonAnimateName);

        fileManager.GetJsonSphereIOPath(isBlochSphere, out string inputPath, out _, out string sequenceOutputPath);
                     
        _ = sqManager.prepareSequence(pythonScriptPath, inputPath, sequenceOutputPath);
    }

    // assigned to button
    public void BackToNormal()
    {
        if(modeText is null)    Debug.LogWarning("Wrning: Text object is missing");
        else                    modeText.SetText("Instant Mode");

        socketsManager.FreezeGateBlock(false);
        
        updateBlochVectorInstant();
    }

    public Vector3 GetNthGatePosInQubit(int qubit, int rank)
    {
        if (isBlochSphere)  return qubitCircuits[qubit].GetNthGatePos(rank);
        
        return Vector3.zero;
    }
}