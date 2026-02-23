using System;
using System.Collections.Generic;
using UnityEngine;

public class QSphere : MonoBehaviour
{
    [SerializeField] GameObject stateParent = null;

    [SerializeField] GameObject stateVectorPrefab = null;

    [SerializeField] int QubitAmount = 5;

    private List<StateVector> vectorList = new List<StateVector>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(stateParent is null)
        {
            stateParent = new GameObject("StateVectors");
            stateParent.transform.parent = gameObject.transform;
        }

        if(stateVectorPrefab is null) Debug.LogWarning("Warning: Prefab for state vector is missing!");
    }

    private void setStateVector()
    {
        // use qubit amount
    }
}
