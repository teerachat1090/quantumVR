using UnityEngine;
using System.Globalization;

public static class GateRotationLibrary
{
    // |0⟩ = +Z (ยอดบน)  -> Unity up
    // |1⟩ = -Z (ยอดล่าง)
    // X axis = Vector3.right   (1, 0, 0)
    // Y axis = Vector3.forward (0, 0, 1)
    // Z axis = Vector3.up      (0, 1, 0)

    public static bool TryGetRotation(string gateRaw, out Vector3 axis, out float angleDeg, out string label)
    {
        axis = Vector3.up;
        angleDeg = 0f;
        label = "";

        if (string.IsNullOrWhiteSpace(gateRaw))
            return false;

        string g = gateRaw.Trim().ToUpperInvariant().Replace(" ", "");

        // ---- Parameterized rotations: RX(90), RY(45), RZ(180) ----
        if (g.StartsWith("RX"))
        {
            axis = Vector3.right; // ✅ ไม่ติดลบ
            angleDeg = ParseAngleDegrees(g, defaultDeg: 180f);
            label = $"RX({angleDeg:F0}°)";
            return true;
        }

        if (g.StartsWith("RY"))
        {
            axis = Vector3.forward; // ✅ ไม่ติดลบ
            angleDeg = ParseAngleDegrees(g, defaultDeg: 180f);
            label = $"RY({angleDeg:F0}°)";
            return true;
        }

        if (g.StartsWith("RZ"))
        {
            axis = Vector3.up;
            angleDeg = ParseAngleDegrees(g, defaultDeg: 180f);
            label = $"RZ({angleDeg:F0}°)";
            return true;
        }

        // ---- Common gates ----
        switch (g)
        {
            case "X":
                axis = Vector3.right;
                angleDeg = 180f;
                label = "X = RX(π)";
                return true;

            case "Y":
                axis = Vector3.forward;
                angleDeg = 180f;
                label = "Y = RY(π)";
                return true;

            case "Z":
                axis = Vector3.up;
                angleDeg = 180f;
                label = "Z = RZ(π)";
                return true;
            
            case "I":
            axis = Vector3.up;
            angleDeg = 0f;
            label = "I = Identity";
            return true;

            case "sqrtX":
            axis = Vector3.right;
            angleDeg = 90f;
            label = "√X = RX(π/2)";
            return true;

            case "sqrtXt":
            axis = Vector3.right;
            angleDeg = -90f;
            label = "√X† = RX(-π/2)";
            return true;

            case "S":
                axis = Vector3.up;
                angleDeg = 90f;
                label = "S = RZ(π/2)";
                return true;

            case "St":
                axis = Vector3.up;
                angleDeg = -90f;
                label = "St = RZ(-π/2)";
                return true;

            case "T":
                axis = Vector3.up;
                angleDeg = 45f;
                label = "T = RZ(π/4)";
                return true;

            case "Tt":
                axis = Vector3.up;
                angleDeg = -45f;
                label = "Tt = -RZ(π/4)";
                return true;            

            case "H":
                // ✅ 180° around (X+Z)/sqrt(2)  (โดย Z ของคุณคือ up)
                axis = (Vector3.right + Vector3.up).normalized;
                angleDeg = 180f;
                label = "H = Rn(π), n=(X+Z)/√2";
                return true;
        }

        return false;
    }

    private static float ParseAngleDegrees(string g, float defaultDeg)
    {
        int l = g.IndexOf('(');
        int r = g.IndexOf(')');

        string number = null;

        if (l >= 0 && r > l) number = g.Substring(l + 1, r - l - 1);
        else number = g.Length > 2 ? g.Substring(2) : null;

        if (string.IsNullOrWhiteSpace(number))
            return defaultDeg;

        if (float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float deg))
            return deg;

        return defaultDeg;
    }
}
