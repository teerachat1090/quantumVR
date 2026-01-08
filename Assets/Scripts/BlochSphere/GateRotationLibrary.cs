using UnityEngine;
using System.Globalization;
using System.Collections.Generic;

[System.Serializable]
public class QuantumGateInfo
{
    public Vector3 axis;
    public float angle;
    public string label;

    // like __init__ in python
    public QuantumGateInfo(Vector3 Axis, float Angle, string Label)
    {
        axis = Axis;
        angle = Angle;
        label = Label;
    }
}

// NOTE: Because RX, RY, RZ need angle input -> hard to implement, Maybe we shouldn't use it.
public static class GateRotationLibrary
{
    // Due to gameObject placement:
    //  +X  ->   Unity X (right)
    //  +Y  ->   Unity Z (forward)
    //  +Z  ->   Unity Y (up)

    public static readonly Dictionary<string, QuantumGateInfo> gateInfoStorage;

    static GateRotationLibrary() // like __init__ in python
    {
        gateInfoStorage = new Dictionary<string, QuantumGateInfo>   // {key, value}
        {
            { "H",  new QuantumGateInfo((Vector3.right + Vector3.up).normalized,     180.0f,     "H = Rn(π), n=(X+Z)/√2") }, 
            { "X",  new QuantumGateInfo( Vector3.right,     180.0f,     "X = RX(π)")},
            { "Y",  new QuantumGateInfo( Vector3.forward,   180.0f,     "Y = RY(π)")},
            { "Z",  new QuantumGateInfo( Vector3.up,        180.0f,     "Z = RZ(π)")},
            { "I",  new QuantumGateInfo( Vector3.up,        0.0f,       "I = Identity")},
            { "T",  new QuantumGateInfo( Vector3.up,        45.0f,      "T = RZ(π/4)")},
            { "S",  new QuantumGateInfo( Vector3.up,        90.0f,      "S = RZ(π/2)")},
            { "TT", new QuantumGateInfo( Vector3.up,        -45.0f,     "TT = -RZ(π/4)")},
            { "ST", new QuantumGateInfo( Vector3.up,        -90.0f,     "ST = RZ(-π/2)")},
            { "SQRTX",  new QuantumGateInfo( Vector3.right,  90.0f,     "√X = RX(π/2)")},
            { "SQRTXT", new QuantumGateInfo( Vector3.right,  -90.0f,    "√X† = RX(-π/2)")},
        };
    }


    public static bool TryGetRotation(string gateRaw, out Vector3 axis, out float angleDeg, out string label)
    {
        axis = Vector3.up;
        angleDeg = 0f;
        label = "";

        Debug.Log($"Raw string: {gateRaw}");
        if (string.IsNullOrWhiteSpace(gateRaw))
            return false;

        string gateName = gateRaw.Trim().ToUpperInvariant().Replace(" ", "");

        // ---- Parameterized rotations: RX(90), RY(45), RZ(180) ----
        if (gateName.StartsWith("RX"))
        {
            axis = Vector3.right; // ✅ ไม่ติดลบ
            angleDeg = ParseAngleDegrees(gateName, defaultDeg: 180f);
            label = $"RX({angleDeg:F0}°)";
            return true;
        }

        if (gateName.StartsWith("RY"))
        {
            axis = Vector3.forward; // ✅ ไม่ติดลบ
            angleDeg = ParseAngleDegrees(gateName, defaultDeg: 180f);
            label = $"RY({angleDeg:F0}°)";
            return true;
        }

        if (gateName.StartsWith("RZ"))
        {
            axis = Vector3.up;
            angleDeg = ParseAngleDegrees(gateName, defaultDeg: 180f);
            label = $"RZ({angleDeg:F0}°)";
            return true;
        }

        // ---- Common gates ----
        if(gateInfoStorage.TryGetValue(gateName, out QuantumGateInfo infoResult))
        {
            axis = infoResult.axis;
            angleDeg = infoResult.angle;
            label = infoResult.label;
            return true;
        }

        return false;
    }

    // string to decaimal (try to support every cases)
    // necessary ?
    private static float ParseAngleDegrees(string g, float defaultDeg)
    {
        int l = g.IndexOf('(');
        int r = g.IndexOf(')');

        string number = null;

        // try to get string between brackets
        if (l >= 0 && r > l) number = g.Substring(l + 1, r - l - 1);
        else number = g.Length > 2 ? g.Substring(2) : null;

        if (string.IsNullOrWhiteSpace(number))
            return defaultDeg;

        // NumberStyles.Float -> support space, start-end with 0, exponential (x.xxE+yy)
        // CultureInfo.InvariantCulture -> use "." for decimal and "," for thousand
        if (float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float deg))
            return deg;

        return defaultDeg;
    }
}
