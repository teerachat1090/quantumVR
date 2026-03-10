using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

public class FileManager
{
    // Folder and file name
    string dataFolder = "QuantumData", inputFolder = "QuantumInput", outputFolder = "QuantumOutput";
    private string jsonBlochinputFileNameName = "bloch_circuit_input.json", jsonQinputFileNameName = "q_circuit_input.json";
    private string jsonBlochoutputFileNameName = "bloch_circuit_output.json", jsonQoutputFileNameName = "q_circuit_output.json";
    private string jsonBlochSequenceFileName = "bloch_circuit_sequence.json", jsonQSequenceFileName = "q_circuit_sequence.json";
    private string pythonScriptFolder = "New code",pythonScriptName = "quantum_circuit.py", pythonAnimateName = "quantum_sequence.py";
    private string mainSciptsPath = Path.Combine(Application.dataPath, "Scripts");
    private string pythonScriptPath;

    public void createEmptyJsonIfNotExist(string jsonPath)
    {
        if (!File.Exists(jsonPath))     File.WriteAllText(jsonPath, "{}");
    }

    public void createFolderIfNotExist(string folderPath)
    {
        if(!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    public void GetJsonIOPath(string inputFileName, string outputFileName, string sequenceFileName, 
                                out string inputPath, out string outputPath, out string sequencePath)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        createFolderIfNotExist(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        createFolderIfNotExist(inputFolderPath);

        string outputFolderPath = Path.Combine(dataFolderPath, outputFolder);
        createFolderIfNotExist(outputFolderPath);   

        inputPath = Path.Combine(inputFolderPath, inputFileName);
        outputPath = Path.Combine(outputFolderPath, outputFileName);
        sequencePath = Path.Combine(outputFolderPath, sequenceFileName);

        createEmptyJsonIfNotExist(inputPath);
        createEmptyJsonIfNotExist(outputPath);
        createEmptyJsonIfNotExist(sequenceFileName);
    }

    public void GetJsonSphereIOPath(bool isBlochSphere, out string inputPath, out string outputPath, out string sequencePath)
    {
        //string tempInputPath, tempOutputPath;
        GetJsonIOPath( isBlochSphere ? jsonBlochinputFileNameName : jsonQinputFileNameName, 
                     isBlochSphere ? jsonBlochoutputFileNameName : jsonQoutputFileNameName,
                     isBlochSphere ?  jsonBlochSequenceFileName : jsonQSequenceFileName,
                     out string tempInputPath, out string tempOutputPath, out string tempSequencePath);

        inputPath = tempInputPath;
        outputPath = tempOutputPath;
        sequencePath = tempSequencePath;
    }

    // TODO: add list input for checking any file
    public void updateJsonInputToFile(string jsonExport, bool isBlochSphere)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        createFolderIfNotExist(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        createFolderIfNotExist(inputFolderPath);
        
        string wantedJsonFile = isBlochSphere ? jsonBlochinputFileNameName : jsonQinputFileNameName;
        string inputPath = Path.Combine(inputFolderPath, wantedJsonFile);

        File.WriteAllText(inputPath, jsonExport);
        //Debug.Log($"-----UPDATE-----\nUpdate json input: {inputPath}\n Result:{jsonExport}");
    }

    //---------------------------------Json quantum file operation---------------------------------------------

    // Get JArray of state from result json file
    public JArray GetQuantumStatFromJson(string jsonOutputPath)
    {
        Debug.Log($"Json file path: {jsonOutputPath}");

        try
        {
            string jsonString = File.ReadAllText(jsonOutputPath);
            JObject jsondata = JObject.Parse(jsonString);
            JArray stats = (JArray)jsondata["state"];

            if(stats is null)
            {
                Debug.LogWarning("Json file has no attribute name: state");
                return null;
            }
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

    public void sample()
    {
        //get state -> result / by index

        //get data
    }

    private List<QubitStat> GetDataFromStat(JArray stats)
    {
        if(stats is null)
        {
            Debug.LogWarning($"Warning: JArray input is null.");
            return null;
        }

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
                    (double)item["prob"],
                    (double)item["phase"]
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

    public List<QubitStat> GetJsonData(bool blochSphereFlag)
    {
        GetJsonSphereIOPath(blochSphereFlag, out _, out string jsonOutputPath, out string _);

        JArray stats = GetQuantumStatFromJson(jsonOutputPath);

        return GetDataFromStat(stats);
    }

    // read json sequence file
    public List<QubitStat> GetStatFromJsonByIndex(bool blochSphereFlag, int index)
    {
        GetJsonSphereIOPath(blochSphereFlag, out _, out _, out string jsonSequenceOutputPath);
        try
        {
            // read as search by index
            string jsonString = File.ReadAllText(jsonSequenceOutputPath);
            JObject jsondata = JObject.Parse(jsonString);
            JArray statsList = (JArray)jsondata["resultList"];
            var statSequence = statsList.FirstOrDefault(item => item.Value<int>("sequenceIndex") == index);

            // get data from stat
            JArray stats = (JArray) statSequence["state"];
            return  GetDataFromStat(stats);
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

    public class QubitStat
    {
        public int val;
        public double realPart, imagPart;
        public double prob;
        public double phase;

        public QubitStat(int Val, double Real, double Imag, double Prob, double Phase)
        {
            val = Val;  realPart = Real;    imagPart = Imag;    prob = Prob;  phase = Phase;
        }
    }
}