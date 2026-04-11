using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TopologyBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject nodePrefab;
    public GameObject linkPrefab;

    [Header("Parents")]
    public Transform nodeParent;
    public Transform linkParent;

    [Header("Graph Transform")]
    public Vector3 graphPosition   = new Vector3(0, 1.5f, 5f);
    public Vector3 graphRotation   = new Vector3(0, 0, 0);
    public Vector3 linearRotation  = new Vector3(0, 0, 0);
    public Vector3 treePosition    = new Vector3(0, 1.5f, 5f);
    
    [Header("Link Label Background")]
    public Color  linkLabelBGColor  = new Color(1f, 1f, 1f, 0.55f); // ขาวโปร่งแสง
    public float  linkLabelBGPadX   = 0.18f;
    public float  linkLabelBGPadY   = 0.10f;
    public int    linkLabelBGRadius = 12;   // corner radius (pixels บน texture)

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

    [Header("Noise Link Colors")]
    public Color noiseLinkColorHigh = new Color(0.11f, 0.62f, 0.46f); // เขียว fidelity >= 80
    public Color noiseLinkColorMid  = new Color(0.73f, 0.46f, 0.09f); // เหลืองส้ม fidelity >= 60
    public Color noiseLinkColorLow  = new Color(0.85f, 0.35f, 0.19f); // แดงส้ม fidelity < 60

    // สร้าง Material พื้นหลังมุมโค้งครั้งเดียว
    private Material _bgMat;
    Material GetBGMaterial()
    {
        if (_bgMat != null) return _bgMat;

        int  tw = 128, th = 64, r = linkLabelBGRadius;
        var  tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        var  px  = new Color32[tw * th];

        for (int y = 0; y < th; y++)
        for (int x = 0; x < tw; x++)
        {
            px[y * tw + x] = InsideRoundRect(x, y, tw, th, r)
                ? new Color32(255, 255, 255, 255)
                : new Color32(0,   0,   0,   0);
        }

        tex.SetPixels32(px);
        tex.Apply();

        _bgMat = new Material(Shader.Find("Sprites/Default"));
        _bgMat.mainTexture = tex;
        _bgMat.color       = linkLabelBGColor;
        return _bgMat;
    }

    bool InsideRoundRect(int x, int y, int w, int h, int r)
    {
        // corners
        int x0 = r, x1 = w - r - 1;
        int y0 = r, y1 = h - r - 1;

        // ถ้าอยู่ในแถบตรงกลาง → ใน
        if (x >= x0 && x <= x1) return true;
        if (y >= y0 && y <= y1) return true;

        // มุมทั้ง 4
        int cx = (x < x0) ? x0 : x1;
        int cy = (y < y0) ? y0 : y1;
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= (float)r * r;
    }


    [Header("Label Settings")]
    public float labelOffsetY   = 0f;
    public float labelFontSize  = 1.2f;
    public Color labelColorEnd  = new Color(0.42f, 0.39f, 0.83f);
    public Color labelColorHub  = new Color(0.83f, 0.65f, 0.13f);
    public Color labelColorRep  = new Color(0.54f, 0.54f, 0.54f);
    public Color labelColorFail = new Color(0.88f, 0.44f, 0.31f);
    public Color labelColorSel  = new Color(0.18f, 0.16f, 0.29f);

    [Header("Link Label Settings")]
    [Tooltip("ขนาดตัวอักษร Distance (km) กลางเส้น")]
    public float linkLabelFontSizeDist = 1.4f;   // ← แยกออกจากกัน ปรับได้ใน Inspector

    [Tooltip("ขนาดตัวอักษร Fidelity (%) กลางเส้น")]
    public float linkLabelFontSizeFid  = 1.4f;   // ← แยกออกจากกัน ปรับได้ใน Inspector

    public float linkLabelOffsetDist =  0.20f;   // Distance label — เหนือเส้น
    public float linkLabelOffsetFid  = -0.20f;   // Fidelity label — ใต้เส้น
    public Color linkLabelColorDist  = new Color(0.267f, 0.114f, 0.455f); // #441D74
    public Color linkLabelColorFid   = new Color(0.267f, 0.114f, 0.455f); // #441D74

    // Runtime lists
    [HideInInspector] public List<GameObject>     nodes         = new();
    [HideInInspector] public List<(int a, int b)> links         = new();
    [HideInInspector] public List<LineRenderer>   linkRenderers = new();
    [HideInInspector] public List<NodeData>       nodeDataList  = new();
    [HideInInspector] public List<TextMeshPro>    nodeLabels    = new();

    // Link labels — 1 ต่อ link, 2 แถว (Dist / Fid)
    [HideInInspector] public List<TextMeshPro> linkLabelsDist = new();
    [HideInInspector] public List<TextMeshPro> linkLabelsFid  = new();

    private string currentTopo = "linear";

    // แนบ BG quad ให้ label GO
    void AttachLabelBG(GameObject labelGO, float widthScale, float heightScale)
    {
        var bgGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(bgGO.GetComponent<MeshCollider>());         // ไม่ต้องการ collider
        bgGO.name = "BG";
        bgGO.transform.SetParent(labelGO.transform, false);
        bgGO.transform.localPosition = new Vector3(0f, 0f, 0.02f); // หลัง TMP
        bgGO.transform.localRotation = Quaternion.identity;
        bgGO.transform.localScale    = new Vector3(widthScale, heightScale, 1f);

        bgGO.GetComponent<MeshRenderer>().material = GetBGMaterial();
}

    public enum NodeType { End, Hub, Repeater }

    public class NodeData
    {
        public int      index;
        public NodeType type;
        public string   label;
        public Vector3  position;
    }

    // ─── Build ───────────────────────────────────────────
    public void Build(string topo, int n, float spacing)
    {
        currentTopo = topo;
        Vector3 rot = (topo == "linear") ? linearRotation : graphRotation;
        nodeParent.position = (topo == "tree") ? treePosition : graphPosition;
        nodeParent.rotation = Quaternion.Euler(rot);

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
        foreach (var g   in nodes)          Destroy(g);
        foreach (var lr  in linkRenderers)  if (lr)  Destroy(lr.gameObject);
        foreach (var lbl in nodeLabels)     if (lbl) Destroy(lbl.gameObject);
        foreach (var lbl in linkLabelsDist) if (lbl) Destroy(lbl.gameObject);
        foreach (var lbl in linkLabelsFid)  if (lbl) Destroy(lbl.gameObject);

        
        FlowManager.Instance?.Clear();
        nodes.Clear();
        links.Clear();
        linkRenderers.Clear();
        nodeDataList.Clear();
        nodeLabels.Clear();
        linkLabelsDist.Clear();
        linkLabelsFid.Clear();
    }

    // ─── Layout Functions ─────────────────────────────────

    void BuildLinear(int n, float spacing)
    {
        float startX = -(n - 1) * spacing / 2f;
        for (int i = 0; i < n; i++)
        {
            nodeDataList.Add(new NodeData
            {
                index    = i,
                type     = (i == 0 || i == n - 1) ? NodeType.End : NodeType.Repeater,
                label    = i == 0 ? "Alice" : i == n - 1 ? "Bob" : "R" + i,
                position = new Vector3(0, 0, startX + i * spacing)
            });
        }
        for (int i = 0; i < n - 1; i++) links.Add((i, i + 1));
    }

    void BuildStar(int n, float spacing)
    {
        nodeDataList.Add(new NodeData
        {
            index = 0, type = NodeType.Hub,
            label = "Hub", position = Vector3.zero
        });

        int leaves   = Mathf.Min(n - 1, 8);
        float r      = spacing * 1.2f;
        int repCount = 0;
        for (int i = 0; i < leaves; i++)
        {
            float angle = -Mathf.PI / 2 + 2 * Mathf.PI * i / leaves;
            nodeDataList.Add(new NodeData
            {
                index    = i + 1,
                type     = (i < 2) ? NodeType.End : NodeType.Repeater,
                label    = i == 0 ? "Alice" : i == 1 ? "Bob" : "R" + (++repCount),
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

        int repCountM = 0;
        for (int i = 0; i < n; i++)
        {
            int c = i % cols, r = i / cols;
            nodeDataList.Add(new NodeData
            {
                index    = i,
                type     = (i == 0 || i == n - 1) ? NodeType.End : NodeType.Repeater,
                label    = i == 0 ? "Alice" : i == n - 1 ? "Bob" : "R" + (++repCountM),
                position = new Vector3(ox + c * spacing, 0, oz + r * spacing)
            });
        }

        for (int i = 0; i < n; i++)
        {
            int c = i % cols, r = i / cols;
            if (c + 1 < cols && i + 1 < n)   links.Add((i, i + 1));
            if (r + 1 < rows && i + cols < n) links.Add((i, i + cols));
        }
    }

    void BuildTree(int n, float spacing)
    {
        int repCount = 0;
        for (int i = 0; i < n; i++)
        {
            int depth    = Mathf.FloorToInt(Mathf.Log(i + 1, 2));
            int posInRow = i - ((1 << depth) - 1);
            int rowCount = Mathf.Min(1 << depth, n - ((1 << depth) - 1));
            float xOffset = (rowCount - 1) * spacing / 2f;

            bool isHub = i == 0;
            bool isEnd = i == 1 || i == n - 1;
            string lbl = i == 0     ? "Root"
                       : i == 1     ? "Alice"
                       : i == n - 1 ? "Bob"
                       : "R" + (++repCount);

            nodeDataList.Add(new NodeData
            {
                index    = i,
                type     = isHub ? NodeType.Hub : isEnd ? NodeType.End : NodeType.Repeater,
                label    = lbl,
                position = new Vector3(posInRow * spacing - xOffset, 0, depth * spacing)
            });

            if (i > 0) links.Add(((i - 1) / 2, i));
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
                index    = i,
                type     = (i == 0 || i == half) ? NodeType.End : NodeType.Repeater,
                label    = i == 0 ? "Alice" : i == half ? "Bob" : "R" + i,
                position = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r)
            });
        }
        for (int i = 0; i < n; i++) links.Add((i, (i + 1) % n));
    }

    // ─── Spawn GameObjects ────────────────────────────────

    void SpawnObjects()
    {
        bool showLabel = GraphManager.Instance != null && GraphManager.Instance.ovLabel;
        bool showDist  = GraphManager.Instance != null && GraphManager.Instance.ovDist;
        bool showFid   = GraphManager.Instance != null && GraphManager.Instance.ovFid;
        bool isLinear  = currentTopo == "linear";

        float distKmPerLink = GraphManager.Instance != null ? GraphManager.Instance.distKm   : 150f;
        float fidelityPct   = GraphManager.Instance != null ? GraphManager.Instance.fidelity : 90f;

        // ── Nodes ─────────────────────────────────────────────────────────────
        foreach (var data in nodeDataList)
        {
            var go = Instantiate(nodePrefab, nodeParent);
            go.transform.localPosition = data.position;
            go.name = data.label;

            float scale = data.type == NodeType.Hub ? 1.4f
                        : data.type == NodeType.End  ? 1.1f : 0.85f;
            go.transform.localScale = Vector3.one * scale;

            var mat = data.type == NodeType.Hub ? matHub
                    : data.type == NodeType.End  ? matEnd : matRep;
            go.GetComponent<Renderer>().material = mat;

            var handler = go.GetComponent<NodeClickHandler>();
            if (handler) handler.nodeIndex = data.index;

            nodes.Add(go);

            // Node label
            var labelGO = new GameObject("Label_" + data.label);
            labelGO.transform.SetParent(nodeParent, true);

            if (isLinear)
            {
                labelGO.transform.localPosition = data.position + new Vector3(-0.75f, labelOffsetY, 0);
                labelGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else
            {
                labelGO.transform.localPosition = data.position + new Vector3(0, -1f, -labelOffsetY);
                labelGO.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            }
            labelGO.transform.localScale = Vector3.one;

            var tmp = labelGO.AddComponent<TextMeshPro>();
            tmp.text      = data.label;
            tmp.fontSize  = labelFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = data.type == NodeType.Hub ? labelColorHub
                          : data.type == NodeType.End  ? labelColorEnd : labelColorRep;
            tmp.gameObject.SetActive(showLabel);
            nodeLabels.Add(tmp);
        }

        // ── Links + Link Labels ────────────────────────────────────────────────
        for (int i = 0; i < links.Count; i++)
        {
            var (a, b) = links[i];

            // LineRenderer
            var go = Instantiate(linkPrefab, linkParent);
            var lr = go.GetComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.SetPosition(0, nodeDataList[a].position);
            lr.SetPosition(1, nodeDataList[b].position);
            lr.material = new Material(matLink);
            linkRenderers.Add(lr);

            var flow = go.AddComponent<LinkFlowEffect>();
            flow.SetReversed(IsLinkReversed(currentTopo, a, b));
            FlowManager.Instance?.Register(flow);

            // กึ่งกลางเส้น (local space ของ nodeParent)
            Vector3 mid = (nodeDataList[a].position + nodeDataList[b].position) * 0.5f;

            // ── Distance Label ────────────────────────────────────────────────
            var distGO = new GameObject("LinkDist_" + i);
            distGO.transform.SetParent(nodeParent, false);
            distGO.transform.localScale = Vector3.one;

            // ทุก topology ใช้ offset Y เหมือนกัน เพื่อให้ label ลอยเหนือเส้นและมองเห็นได้
            if (isLinear)
            {
                distGO.transform.localPosition = mid + new Vector3(0, linkLabelOffsetDist, 0);
                distGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else
            {
                distGO.transform.localPosition = mid + new Vector3(0, -0.25f,-0.15f);
                distGO.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            }

            var distTMP       = distGO.AddComponent<TextMeshPro>();
            distTMP.text      = distKmPerLink + " km";
            distTMP.fontSize  = linkLabelFontSizeDist;   // ← ใช้ field แยก
            distTMP.alignment = TextAlignmentOptions.Center;
            distTMP.color     = linkLabelColorDist;
            distTMP.fontStyle = FontStyles.Bold;
            distTMP.gameObject.SetActive(showDist);
            linkLabelsDist.Add(distTMP);

            AttachLabelBG(distGO, linkLabelFontSizeDist * 0.35f + linkLabelBGPadX,
                       linkLabelFontSizeDist * 0.1f + linkLabelBGPadY);

            // ── Fidelity Label ────────────────────────────────────────────────
            var fidGO = new GameObject("LinkFid_" + i);
            fidGO.transform.SetParent(nodeParent, false);
            fidGO.transform.localScale = Vector3.one;

            // ทุก topology ใช้ offset Y เหมือนกัน เพื่อให้ label ลอยใต้เส้นและมองเห็นได้
            if (isLinear)
            {
                fidGO.transform.localPosition = mid + new Vector3(0, linkLabelOffsetFid, 0);
                fidGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            else
            {
                fidGO.transform.localPosition = mid + new Vector3(0, -0.25f, 0.15f);
                fidGO.transform.localRotation = Quaternion.Euler(-90, 0, 0);  
            }

            var fidTMP       = fidGO.AddComponent<TextMeshPro>();
            fidTMP.text      = "F: " + fidelityPct.ToString("F0") + "%";
            fidTMP.fontSize  = linkLabelFontSizeFid;     // ← ใช้ field แยก
            fidTMP.alignment = TextAlignmentOptions.Center;
            fidTMP.color     = linkLabelColorFid;
            fidTMP.fontStyle = FontStyles.Bold;
            fidTMP.gameObject.SetActive(showFid);
            linkLabelsFid.Add(fidTMP);

            AttachLabelBG(fidGO, linkLabelFontSizeFid * 0.35f + linkLabelBGPadX, 
                                 linkLabelFontSizeFid * 0.1f + linkLabelBGPadY);
        }
    }

    // ─── Flow Direction ──────────────────────────────────
    bool IsLinkReversed(string topo, int a, int b)
    {
        int n = GraphManager.Instance != null ? GraphManager.Instance.nodeCount : 5;

        switch (topo)
        {
            case "linear":
            case "mesh":
                return false; // Alice(0)→Bob(n-1) index ต่ำ→สูง ถูกแล้ว

            case "star":
                // link(0,1) = Hub-Alice → ควรวิ่ง Alice→Hub = reverse
                if (a == 0 && b == 1) return true;
                return false; // Hub→Bob, Hub→Leaf ปกติ

            case "tree":
                // link(0,1) = Root-Alice → ควรวิ่ง Alice→Root = reverse
                if (a == 0 && b == 1) return true;
                return false; // Root→child ปกติ

            case "ring":
                // CW path: Alice(0)→R1(1)→R2(2)→Bob(half)
                //   links: (0,1),(1,2),(2,3),...,(half-1,half)  → a→b = ไม่ reverse
                // CCW path: Alice(0)→R3(n-1)→R4(n-2)→Bob(half)
                //   links: (half,half+1),...,(n-1,0)  → ควรวิ่ง 0→n-1→...→half
                //   link(n-1,0): a=n-1,b=0 ต้องวิ่ง 0→n-1 = b→a = reverse
                //   link(half,half+1): a=half,b=half+1 ต้องวิ่ง half+1→half = b→a = reverse
                {
                    int half = Mathf.CeilToInt(n / 2f);
                    bool isCCW = (a >= half) || (b == 0 && a > 0);
                    return isCCW; // CCW reverse ให้วิ่งออกจาก Alice
                }

            default:
                return false;
        }
    }

    // ─── Refresh Labels (Node) ────────────────────────────
    public void RefreshLabels(int failNode, int selNode)
    {
        bool show = GraphManager.Instance != null && GraphManager.Instance.ovLabel;

        for (int i = 0; i < nodeLabels.Count; i++)
        {
            if (nodeLabels[i] == null) continue;
            nodeLabels[i].gameObject.SetActive(show);
            if (!show) continue;

            bool isFail = i == failNode;
            bool isSel  = i == selNode;

            nodeLabels[i].color = isFail ? labelColorFail
                                : isSel  ? labelColorSel
                                : nodeDataList[i].type == NodeType.Hub ? labelColorHub
                                : nodeDataList[i].type == NodeType.End  ? labelColorEnd
                                : labelColorRep;
        }
    }

    // ─── Refresh Link Labels (Distance + Fidelity) ────────
    // เรียกจาก GraphManager.Refresh() ทุกครั้งที่ distKm / fidelity / ovDist / ovFid เปลี่ยน
    // รวมถึงอัปเดต fontSize ด้วย เพื่อรองรับการปรับค่าใน Inspector ขณะ runtime
    public void RefreshLinkLabels()
    {
        if (GraphManager.Instance == null) return;

        bool  showDist      = GraphManager.Instance.ovDist;
        bool  showFid       = GraphManager.Instance.ovFid;
        float distKmPerLink = GraphManager.Instance.distKm;
        float fidelityPct   = GraphManager.Instance.fidelity;

        for (int i = 0; i < linkLabelsDist.Count; i++)
        {
            if (linkLabelsDist[i] == null) continue;
            linkLabelsDist[i].text     = distKmPerLink + " km";
            linkLabelsDist[i].fontSize = linkLabelFontSizeDist;   // ← sync font size runtime
            linkLabelsDist[i].color    = linkLabelColorDist;
            linkLabelsDist[i].gameObject.SetActive(showDist);
        }

        for (int i = 0; i < linkLabelsFid.Count; i++)
        {
            if (linkLabelsFid[i] == null) continue;
            linkLabelsFid[i].text     = "F: " + fidelityPct.ToString("F0") + "%";
            linkLabelsFid[i].fontSize = linkLabelFontSizeFid;     // ← sync font size runtime
            linkLabelsFid[i].color    = linkLabelColorFid;
            linkLabelsFid[i].gameObject.SetActive(showFid);
        }
    }

    // ─── Update Link Colors ───────────────────────────────
    public void RefreshLinkColors(int failNode, int selNode,
                                  bool simFail, bool simJam, bool simHeavy,
                                  bool simDegrade, System.Collections.Generic.HashSet<int> degradedLinks,
                                  System.Collections.Generic.HashSet<int> cascadeFailedNodes)
    {
        float[] linkFids = GraphManager.Instance != null
                         ? GraphManager.Instance.linkFidelities
                         : null;

        for (int i = 0; i < links.Count; i++)
        {
            var (a, b) = links[i];
            bool isFail    = simFail  && (a == failNode || b == failNode);
            bool isCascade = cascadeFailedNodes != null &&
                             (cascadeFailedNodes.Contains(a) || cascadeFailedNodes.Contains(b));
            bool isHeavy   = simHeavy   && i % 2 == 0;
            bool isDegrade = simDegrade && degradedLinks != null && degradedLinks.Contains(i);
            bool isSel     = selNode >= 0 && (a == selNode || b == selNode);

            // Noise — เปลี่ยนสี link ตาม fidelity จริงของแต่ละ link
            bool isNoise = simJam && linkFids != null && linkFids.Length > i;

            Material mat;
            if (isFail || isCascade)
                mat = matLinkFail;
            else if (isDegrade)
                mat = matLinkFail;
            else if (isHeavy)
                mat = matLinkHeavy;
            else if (isNoise)
            {
                // สร้าง material instance และเปลี่ยนสีตาม fidelity
                mat = new Material(matLink);
                float f   = linkFids[i];
                Color col = f >= 80 ? noiseLinkColorHigh
                          : f >= 60 ? noiseLinkColorMid
                          :           noiseLinkColorLow;
                col.a = 0.05f;
                mat.color = col;
            }
            else
                mat = matLink;

            linkRenderers[i].material   = mat;
            linkRenderers[i].startWidth = (isSel || isHeavy) ? 0.12f : 0.06f;
            linkRenderers[i].endWidth   = (isSel || isHeavy) ? 0.12f : 0.06f;
        }
    }

    // ─── Update Node Colors ───────────────────────────────
    public void RefreshNodeColors(int failNode, int selNode,
                                  System.Collections.Generic.HashSet<int> cascadeFailedNodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var data       = nodeDataList[i];
            bool isFail    = i == failNode;
            bool isCascade = cascadeFailedNodes != null && cascadeFailedNodes.Contains(i);
            bool isSel     = i == selNode;

            var mat = (isFail || isCascade) ? matFail
                    : isSel                 ? matSelected
                    : data.type == NodeType.Hub ? matHub
                    : data.type == NodeType.End  ? matEnd : matRep;

            nodes[i].GetComponent<Renderer>().material = mat;
        }
    }
}