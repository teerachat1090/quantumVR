/// <summary>
/// ข้อมูล metrics ที่ส่งจาก GraphManager ไปยัง MetricsPanel
/// </summary>
public struct MetricsData
{
    public int    hops;          // จำนวน hop (-1 = partitioned)
    public int    links;         // จำนวน link ทั้งหมด
    public int    fidelity;      // Total Fidelity (%)
    public int    distKm;        // ระยะทางรวม (km)
    public string fault;         // Fault Tolerance ("ต่ำ" / "กลาง" / "สูง" / "ดี")
    public float  qber;          // Quantum Bit Error Rate (0–0.5)
    public float  entangleRate;  // Entanglement Rate (ebit/s)
    public string penaltyTag;    // ข้อความแสดง penalty เช่น "-8 degrade, heavy"

    /// <summary>ไม่มีเส้นทาง Alice → Bob</summary>
    public bool partitioned => hops == -1;
}
