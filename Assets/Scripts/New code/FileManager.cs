using System;
using System.IO;
using UnityEngine;

public class FileManager
{
    // Folder and file name
    string dataFolder = "QuantumData", inputFolder = "QuantumInput", outputFolder = "QuantumOutput";
    private string jsonBlochinputFileNameName = "bloch_circuit_input.json", jsonQinputFileNameName = "q_circuit_input.json";
    private string jsonBlochoutputFileNameName = "bloch_circuit_output.json", jsonQoutputFileNameName = "q_circuit_output.json";
    private string jsonBlochSequenceFileName = "bloch_circuit_sequence.json", jsonQSequenceFileName = "q_circuit_sequence.json";
    private string pythonScriptFolder = "New code",pythonScriptName = "sample.py", pythonAnimateName = "quantum_sequence.py";
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

    public void GetJsonIOPath(string inputFileName, string outputFileName, out string inputPath, out string outputPath)
    {
        string dataFolderPath = Path.Combine(Application.persistentDataPath, dataFolder);
        createFolderIfNotExist(dataFolderPath);

        string inputFolderPath = Path.Combine(dataFolderPath, inputFolder);
        createFolderIfNotExist(inputFolderPath);

        string outputFolderPath = Path.Combine(dataFolderPath, outputFolder);
        createFolderIfNotExist(outputFolderPath);   

        inputPath = Path.Combine(inputFolderPath, inputFileName);
        outputPath = Path.Combine(outputFolderPath, outputFileName);

        createEmptyJsonIfNotExist(inputPath);
        createEmptyJsonIfNotExist(outputPath);
    }

    public void GetJsonSphereIOPath(bool isBlochSphere, out string inputPath, out string outputPath)
    {
        //string tempInputPath, tempOutputPath;
        GetJsonIOPath( isBlochSphere ? jsonBlochinputFileNameName : jsonQinputFileNameName, 
                     isBlochSphere ? jsonBlochoutputFileNameName : jsonQoutputFileNameName, 
                     out string tempInputPath, out string tempOutputPath);

        inputPath = tempInputPath;
        outputPath = tempOutputPath;
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
}