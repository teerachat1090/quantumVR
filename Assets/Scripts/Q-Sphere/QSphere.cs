using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using QubitStat = FileManager.QubitStat;

public class QSphere : MonoBehaviour
{
    [SerializeField] GameObject stateParent = null;

    [SerializeField] GameObject stateVectorPrefab = null;

    private int qubitAmount = 0;

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

    public void ChangeQubitAmount(int newAmount)
    {
        if(newAmount == qubitAmount)
        {
            Debug.Log("Qubit amount unchanged");
            return;
        }

        qubitAmount = newAmount;
        setStateVector();
    }

    // nCr = n!/(r! * (n-r)!)
    //
    //  n * (n-1) * ... * (n-r+1)
    //  ---------------------------
    //   r * (r-1) * ... * (1)
    private int Comb(int n, int r)
    {
        if (r < 0 || r > n) return 0; //invalid case
        if (r == 0 || r == n) return 1; //edge case
        if (r > n / 2) r = n - r;   //nCr(n, r) = nCr(n, n-r)
        
        int result = 1;
        for (int i = 1; i <= r; i++)    result = result * (n - r + i) / i;
        
        return result;
    }

    private int[] GetHalfCombArray(int n)
    {
        if(n < 1) return null;
        var arr = new int[n/2+1];

        for(int i=0; i<=n/2; i++) arr[i] = Comb(n, i);

        return arr;
    }

    private void setStateVector()
    {
        Debug.Log("Setting state vector");

        if(stateVectorPrefab is null)
        {
            Debug.LogWarning("Warning: Prefab for create state vector is missing");
            return;
        }

        // clear list if have one
        if(vectorList.Count == 0){
            foreach(StateVector vector in vectorList)   
                Destroy(vector.gameObject);
            
            vectorList.Clear();
        }

        // set array of max number (M) - combinatorics
        var maxAmount = GetHalfCombArray(qubitAmount);

        // trace from end
        int[] endAmount = maxAmount.Select(i => i-1).ToArray();

        // trace from start
        var startAmount = new int[qubitAmount/2+1]; //array of 0

        int round = 1 << qubitAmount-1;
        int bitFlip = (round  << 1) - 1 ;
        
        for(int i=0; i<round; i++)
        {
            int ones = Convert.ToString(i, 2).Count(c => c == '1');
            int num = i, flipNum = i^bitFlip;
            bool trackFromEnd = false;
            if(ones > qubitAmount/2)
            { 
                (num, flipNum) = (flipNum, num);
                trackFromEnd = true;
                ones = qubitAmount - ones;
            }

            // upper node
            GameObject spawned = Instantiate(stateVectorPrefab, stateParent.transform);
            spawned.name = $"Statevector {num}";
            StateVector vector = spawned.GetComponent<StateVector>();
            vector.SetStateValue(num, qubitAmount);

            // lower node (opposited)
            GameObject spawnedCounter = Instantiate(stateVectorPrefab, stateParent.transform);
            spawnedCounter.name = $"Statevector <flip> {flipNum}";
            StateVector vectorCounter = spawnedCounter.GetComponent<StateVector>();
            vectorCounter.SetStateValue(flipNum, qubitAmount);

            float y = (float) - (float) (trackFromEnd ? endAmount[ones] : startAmount[ones]) / maxAmount[ones] * 360.0f;
            float z = (float) ones/qubitAmount*180.0f;

            spawned.transform.eulerAngles = new Vector3(0.0f, y, z);
            vector.adjustText(gameObject.transform.rotation);
            
            spawnedCounter.transform.eulerAngles = new Vector3(0.0f, y + 180.0f, 180.0f - z);
            vectorCounter.adjustText(gameObject.transform.rotation);

            vectorList.Add(vector);
            vectorList.Add(vectorCounter);

            if(trackFromEnd)    endAmount[ones]--;
            else                startAmount[ones]++;
        }

        vectorList.Sort((a,b) => a.GetStateVal().CompareTo(b.GetStateVal()));

        Debug.Log("Setting finished.");
    }


    public void UpdateFromJson()
    {
        Debug.Log("QSphere updating...");
        var fileManager = new FileManager();

        List<QubitStat> stat = fileManager.GetJsonData(blochSphereFlag: false);

        // take data from each list and update each vector
        foreach(QubitStat q in stat)
        {
            float newProb = (float) q.prob;
            float newPhase = (float) q.phase;
            if(q.val < 0 || q.val > Math.Pow(2, qubitAmount))
            {
                Debug.LogWarning($"Warning: QubitState value error: {q.val}");
                continue;
            }
            vectorList[q.val].UpdateStateVector(newProb, newPhase);
        }

        Debug.Log("QSphere update finished.");
    }
}
