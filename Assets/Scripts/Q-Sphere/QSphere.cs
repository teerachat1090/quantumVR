using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using QubitStat = FileManager.QubitStat;

public class QSphere : MonoBehaviour
{
    [SerializeField] GameObject stateParent = null; //storage for spawned stateVectors
    [SerializeField] GameObject stateVectorPrefab = null;
    [SerializeField] float offsetDist = 0f; //distance from sphere
    private Vector3 pointToLookAt = Vector3.zero;
    [SerializeField] GameObject centerObject = null;

    private int qubitAmount = 0;

    private List<StateVector> vectorList = new List<StateVector>();

    void Start()
    {
        if(centerObject is null) centerObject = gameObject;
        pointToLookAt = gameObject.transform.position + offsetDist*gameObject.transform.forward;
        if(stateParent is null)
        {
            stateParent = new GameObject("StateVectors");
            stateParent.transform.parent = gameObject.transform;
        }

        if(stateVectorPrefab is null) Debug.LogWarning("Warning: Prefab for state vector is missing!");
    }

    /// <summary>
    ///     ปรับเปลี่ยนโครงสร้างของ q-sphere ตามจำนวน qubits <br/>
    ///     ใช้เมื่อเริ่มสร้าง q-sphere ในตอนเริ่มโปรแกรม กับรวมถึงเมื่อจำนวน qubits เปลี่ยนไป (feature ในอนาคต)
    /// </summary>
    public void ChangeQubitAmount(int newAmount)
    {
        if(newAmount == qubitAmount)
        {
            Debug.Log("Qubit amount unchanged");
            return;
        }

        qubitAmount = newAmount;
        SetStateVector();
    }

    /// <summary>
    ///     หา combination ของ <c>n</c> เลือก <c>r</c>
    /// </summary>
    /// <param name="n">จำนวนทั้งหมด</param>
    /// <param name="r">จำนวนที่เลือก</param>
    /// <returns> จำนวนวิธีในการเลือก </returns>
    private int Comb(int n, int r)
    {
        if (r < 0 || r > n) return 0; //invalid case
        if (r == 0 || r == n) return 1; //edge case
        if (r > n / 2) r = n - r;   //nCr(n, r) = nCr(n, n-r)
        
        int result = 1;
        for (int i = 1; i <= r; i++)    result = result * (n - r + i) / i;
        
        return result;
    }

    /// <summary>
    ///     หา array ครึ่งหนึ่งของ <c>n</c> เลือก <c>i</c> เมื่อ <c>i</c> มีค่าตั้งแต่ 0-n
    /// </summary>
    /// <param name="n">จำนวนทั้งหมด</param>
    /// <returns>array ครึ่งหนึ่งของ <c>n</c> เลือก <c>i</c></returns>
    private int[] GetHalfCombArray(int n)
    {
        if(n < 1) return null;
        var arr = new int[n/2+1];

        for(int i=0; i<=n/2; i++) arr[i] = Comb(n, i);

        return arr;
    }

    /// <summary>
    ///     <b>สร้างและจัดสรรค์ statevector บน Q-Sphere</b>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         ในวงจร n qubits จะสามารถมีผลลัพธ์ได้ 2^n รูปแบบ = 2^n statevector
    ///         ซึ่งใน q-sphere จะแบ่งเป็น n ชั้น โดยจำแนกตาม Hamming Distance เมื่อเทียบกับ 00...0 (n ตัว)
    ///         กล่าวคือ แต่ละชั้นจะจำแนกตามจำนวน 1's ใน statevector นั้นๆ เช่น 001110 มี Hamming Distance = 3 
    ///     </para>
    ///     <para>
    ///         ในแต่ละชั้นของ q-sphere จึงจะมีจำนวนเท่ากับ combination ของ n เลือก r เมื่อ r = อันดับชั้น พอดี (ใน n bits เลือก r bits ให้เป็น 1's)
    ///         เช่น ถ้ามี 3 qubits แต่ละชั้นใน q-sphere จะมี {1, 3, 3, 1} statevector โดย statevector ในแต่ละชั้นจะเรียงตัวกันเป็นวงครบ
    ///         โดยเว้นระยะห่างด้วยมุมที่เท่ากัน
    ///     </para>
    ///     <para>
    ///         นอกจากนี้แล้ว ใน q-sphere (อ้างอิงจาก IBM composer) statevector ที่มี bit ตรงข้ามกัน เช่น 00110 กับ 11001 จะต้องมีตำแหน่ง
    ///         ที่อยู่ตรงข้ามกันอีกด้วย ดังนั้น เมื่อเรานำมาปรับใช้ในโปรเจคนี้ เราจึงเน้นไปที่การสร้าง statevector เพียงครึ่งเดียว (ครึ่งบน) 
    ///         แล้วจึงค่อยสร้างอีกครึ่งโดยใช้ตำแหน่งตรงข้ามแทน
    ///     </para>
    /// </remarks>
    private void SetStateVector()
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
            vector.pointToSee = pointToLookAt;
            vector.SetStateValue(num, qubitAmount);

            // lower node (opposited)
            GameObject spawnedCounter = Instantiate(stateVectorPrefab, stateParent.transform);
            spawnedCounter.name = $"Statevector <flip> {flipNum}";
            StateVector vectorCounter = spawnedCounter.GetComponent<StateVector>();
            vectorCounter.pointToSee = pointToLookAt;
            vectorCounter.SetStateValue(flipNum, qubitAmount);

            float y = (float) - (float) (trackFromEnd ? endAmount[ones] : startAmount[ones]) / maxAmount[ones] * 360.0f;
            float z = (float) ones/qubitAmount*180.0f;

            spawned.transform.eulerAngles = new Vector3(0.0f, y, z);
            vector.adjustText(gameObject.transform.rotation);
            
            spawnedCounter.transform.eulerAngles = new Vector3(0.0f, y + 180.0f, 180.0f - z);
            vectorCounter.adjustText(gameObject.transform.rotation);

            vectorList.Add(vector);
            vectorList.Add(vectorCounter);

            vector.gameObject.SetActive(true);
            vectorCounter.gameObject.SetActive(true);

            if(trackFromEnd)    endAmount[ones]--;
            else                startAmount[ones]++;
        }

        vectorList.Sort((a,b) => a.GetStateVal().CompareTo(b.GetStateVal()));

        Debug.Log("Setting finished.");
    }

    /// <summary>
    ///     อัพเดทสถานะของ statevector บน q-sphere โดยอ้างอิงจากไฟล์ json
    /// </summary>
    /// <param name="isSequence">flag เพื่อเช็คว่าต้องอ้างอิงจากไฟล์ json แบบใด (output/sequence)</param>
    /// <param name="index">เลขอันดับผลลัพธ์ในไฟล์ json แบบ sequence (ใช้เมื่อ <c>isSequence = true</c>)</param>
    public void UpdateFromJson(bool isSequence = false, int index = 0)
    {
        Debug.Log("QSphere updating...");
        var fileManager = new FileManager();

        List<QubitStat> stat;

        if(isSequence) stat = fileManager.GetStatFromJsonByIndex(false, index);
        else stat = fileManager.GetStatFromJsonData(blochSphereFlag: false);

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
