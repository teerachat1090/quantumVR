using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitExecutor
{
    private string dataFolder = "QuantumData", inputFolder = "QuantumInput";
    private string jsonInputFileName = "circuit_input.json";

    // output name: instantBlochOuput.json
    public IEnumerator PrepareToRunQiskit(){
        Debug.Log("-----Preparing-----");
        // check json input, python script
        // check python command

        // <start coroutine>
        // run python background
        // receive data from python

        // save rusult as dictionary
        yield break;
    }

    private static void checkJsonInput()
    {
        return;
    }
}