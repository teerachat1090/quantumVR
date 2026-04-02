using System.Collections.Generic;
using UnityEngine;

public class TopologyBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject nodePrefab;
    public GameObject linkPrefab;

    [Header("Parents")]
    public Transform nodeParent;
    public Transform linkParent;

    [Header("Materials")]
    public Material matEnd;
    public Material matHub;
    public Material matRep;
    public Material matFail;
    public Material matSelected;
    public Material matLink;
    public Material matLinkFail;
    public Material matLinkJam;
    public Material matLinkHeavy;

    // Runtime lists
    [HideInInspector] public List<GameObject> nodes = new();
    [HideInInspector] public List<(int a, int b)> links = new();
    [HideInInspector] public List<LineRenderer> linkRenderers = new();
    [HideInInspector] public List<NodeData> nodeDataList = new();

    public enum NodeType { End, Hub, Repeater }

    public class NodeData
    {
        public int index;
        public NodeType type;
        public string label;
        public Vector3 position;
    }

    // ─── Build ───────────────────────────────────────────
    public void Build(string topo, int n, float spacing)
    {
        Clear();
        switch (topo)
        {
            case "linear": BuildLinear(n, spacing); break;
            case "star":   BuildStar(n, spacing);   break;
            case "mesh":   BuildMesh(n, spacing);   break;
            case "tree":   BuildTree(n, spacing);   break;
            case "ring":   BuildRing(n, spacing);   break;
        }
        SpawnObjects();
    }

    void Clear()
    {
        foreach (var g in nodes) Destroy(g);
        foreach (var g in linkRenderers)
            if (g) Destroy(g.gameObject);
        nodes.Clear();
        links.Clear();
        linkRenderers.Clear();
        nodeDataList.Clear();
    }

    // ─── Layout Functions ─────────────────────────────────

    void BuildLinear(int n, float spacing)
    {
        float startX = -(n - 1) * spacing / 2f;
        for (int i = 0; i < n; i++)
        {
            nodeDataList.Add(new NodeData
            {
                index = i,
                type  = (i == 0 || i == n - 1) ? NodeType.End : NodeType.Repeater,
                label = i == 0 ? "Alice" : i == n - 1 ? "Bob" : "R" + i,
                position = new Vector3(startX + i * spacing, 0, 0)
            });
        }
        for (int i = 0; i < n - 1; i++) links.Add((i, i + 1));
    }

    void BuildStar(int n, float spacing)
    {
        // Hub ตรงกลาง
        nodeDataList.Add(new NodeData
        {
            index = 0, type = NodeType.Hub,
            label = "Hub", position = Vector3.zero
        });

        int leaves = Mathf.Min(n - 1, 8);
        float r = spacing * 1.2f;
        for (int i = 0; i < leaves; i++)
        {
            float angle = -Mathf.PI / 2 + 2 * Mathf.PI * i / leaves;
            nodeDataList.Add(new NodeData
            {
                index = i + 1,
                type  = (i < 2) ? NodeType.End : NodeType.Repeater,
                label = i == 0 ? "Alice" : i == 1 ? "Bob" : "N" + (i + 1),
                position = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r)
            });
            links.Add((0, i + 1));
        }
    }

    void BuildMesh(int n, float spacing)
    {
        int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
        int rows = Mathf.CeilToInt((float)n / cols);
        float ox = -(cols - 1) * spacing / 2f;
        float oz = -(rows - 1) * spacing / 2f;

        for (int i = 0; i < n; i++)
        {
            int c = i % cols, r = i / cols;
            nodeDataList.Add(new NodeData
            {
                index = i,
                type  = (i == 0 || i == n - 1) ? NodeType.End : NodeType.Repeater,
                label = i == 0 ? "Alice" : i == n - 1 ? "Bob" : "N" + i,
                position = new Vector3(ox + c * spacing, 0, oz + r * spacing)
            });
        }

        // เชื่อมแนวนอน + แนวตั้ง
        for (int i = 0; i < n; i++)
        {
            int c = i % cols, r = i / cols;
            if (c + 1 < cols && i + 1 < n)       links.Add((i, i + 1));
            if (r + 1 < rows && i + cols < n)     links.Add((i, i + cols));
        }
    }

    void BuildTree(int n, float spacing)
    {
        for (int i = 0; i < n; i++)
        {
            int depth = Mathf.FloorToInt(Mathf.Log(i + 1, 2));
            int posInRow = i - ((1 << depth) - 1);
            int rowCount = Mathf.Min(1 << depth, n - ((1 << depth) - 1));
            float xOffset = (rowCount - 1) * spacing / 2f;

            nodeDataList.Add(new NodeData
            {
                index = i,
                type  = (i == 0) ? NodeType.Hub
                       : (i == n - 1) ? NodeType.End
                       : NodeType.Repeater,
                label = i == 0 ? "Root" : i == n - 1 ? "Bob" : "N" + i,
                position = new Vector3(
                    posInRow * spacing - xOffset,
                    0,
                    depth * spacing)
            });

            if (i > 0) links.Add(((i - 1) / 2, i)); // parent → child
        }
    }

    void BuildRing(int n, float spacing)
    {
        float r = spacing * n / (2 * Mathf.PI);
        r = Mathf.Clamp(r, spacing, spacing * 2.5f);

        for (int i = 0; i < n; i++)
        {
            float angle = -Mathf.PI / 2 + 2 * Mathf.PI * i / n;
            int half = Mathf.CeilToInt(n / 2f);
            nodeDataList.Add(new NodeData
            {
                index = i,
                type  = (i == 0 || i == half) ? NodeType.End : NodeType.Repeater,
                label = i == 0 ? "Alice" : i == half ? "Bob" : "R" + i,
                position = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r)
            });
        }
        for (int i = 0; i < n; i++) links.Add((i, (i + 1) % n));
    }

    // ─── Spawn GameObjects ────────────────────────────────

    void SpawnObjects()
    {
        // Nodes
        foreach (var data in nodeDataList)
        {
            var pos = data.position + Vector3.up * 1.5f; // ยกขึ้น 1.5 หน่วย
            var go = Instantiate(nodePrefab, pos, Quaternion.identity, nodeParent);
            go.name = data.label;

            // ตั้งขนาดตาม type
            float scale = data.type == NodeType.Hub ? 1.4f
                        : data.type == NodeType.End  ? 1.1f : 0.85f;
            go.transform.localScale = Vector3.one * scale;

            // ตั้ง Material
            var mat = data.type == NodeType.Hub ? matHub
                    : data.type == NodeType.End  ? matEnd : matRep;
            go.GetComponent<Renderer>().material = mat;

            // เก็บ index ไว้ใน NodeClickHandler (Step 4)
           // var handler = go.GetComponent<NodeClickHandler>();
           // if (handler) handler.nodeIndex = data.index;

            nodes.Add(go);
        }

        // Links
        foreach (var (a, b) in links)
        {
            var go = Instantiate(linkPrefab, Vector3.zero, Quaternion.identity, linkParent);
            var lr = go.GetComponent<LineRenderer>();
            lr.SetPosition(0, nodeDataList[a].position + Vector3.up * 1.5f);
            lr.SetPosition(1, nodeDataList[b].position + Vector3.up * 1.5f);
            lr.material = matLink;
            linkRenderers.Add(lr);
        }
    }

    // ─── Update Link Colors (ถูกเรียกจาก GraphManager) ──
    public void RefreshLinkColors(int failNode, int selNode,
                                   bool simFail, bool simJam, bool simHeavy)
    {
        for (int i = 0; i < links.Count; i++)
        {
            var (a, b) = links[i];
            bool isFail  = simFail  && (a == failNode || b == failNode);
            bool isJam   = simJam   && i % 3 == 1;
            bool isHeavy = simHeavy && i % 2 == 0;
            bool isSel   = selNode >= 0 && (a == selNode || b == selNode);

            var col = isFail  ? new Color(0.88f, 0.44f, 0.31f)
                    : isJam   ? new Color(0.83f, 0.65f, 0.13f)
                    : isHeavy ? new Color(0.23f, 0.62f, 0.40f)
                    : isSel   ? new Color(0.42f, 0.39f, 0.83f)
                    :           new Color(0.61f, 0.58f, 0.88f);

            linkRenderers[i].material.color = col;
            linkRenderers[i].startWidth = (isSel || isHeavy) ? 0.12f : 0.06f;
            linkRenderers[i].endWidth   = (isSel || isHeavy) ? 0.12f : 0.06f;
        }
    }

    // ─── Update Node Colors ───────────────────────────────
    public void RefreshNodeColors(int failNode, int selNode)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var data = nodeDataList[i];
            bool isFail = i == failNode;
            bool isSel  = i == selNode;

            var mat = isFail ? matFail
                    : isSel  ? matSelected
                    : data.type == NodeType.Hub ? matHub
                    : data.type == NodeType.End  ? matEnd : matRep;

            nodes[i].GetComponent<Renderer>().material = mat;
        }
    }
}
