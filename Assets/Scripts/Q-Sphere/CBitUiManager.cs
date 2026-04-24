using System.Collections.Generic;
using UnityEngine;
using QubitStat = FileManager.QubitStat;

public class CBitUiManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject cBitUiPrefab = null;
    [SerializeField] private RectTransform backgroundImage = null;
    [SerializeField] private GameObject cBitParent = null;

    private int cBitNum = 0;
    private List<int> valList = new List<int>();
    private List<CBitUi> cBitUiList = new List<CBitUi>();

    public void CreateCBitUi(int num)
    {
        if(cBitNum != 0) return;

        // 1:2:1:2:1:2:...:1
        cBitNum = num;
        float imageWidth = backgroundImage.rect.width;
        float unitRatio = imageWidth/(3*cBitNum + 1);
        float lengthCount = imageWidth/2 - unitRatio*2;
        float currentY = backgroundImage.localPosition.y;

        for(int i=0; i<cBitNum; i++)
        {
            GameObject spawned = Instantiate(cBitUiPrefab, cBitParent.transform);

            var cBitUi = spawned.GetComponent<CBitUi>();
            var cBitUiRect = spawned.GetComponent<RectTransform>();
            if(cBitUi == null || cBitUiRect == null)
            {
                Debug.LogWarning("Warning: Prefab not has require component.");
                Destroy(spawned);
                return;
            }

            cBitUiRect.localPosition = new Vector2(lengthCount, currentY);
            cBitUi.SetBitIndex(i);
            
            cBitUiList.Add(cBitUi);

            lengthCount -= unitRatio*3;
        }
    }

    private void UpdateValtoBitUi(List<int> cbitlist)
    {
        for(int i=0; i<cBitUiList.Count; i++)
        {
            cBitUiList[i].SetBitValue(cbitlist[i]);
        }
    }

    public void UpdateUi(List<int> cbitlist)
    {
        int statCount = cbitlist.Count;
        bool rebuildFlag = cBitUiList.Count != statCount;

        if(rebuildFlag)
        {
            CreateCBitUi(statCount);
        }
        
        UpdateValtoBitUi(cbitlist);
    }

    void Start()
    {
        if(cBitParent == null)
        {
            cBitParent = new GameObject("CBitsUi");
            cBitParent.transform.SetParent(transform, false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
