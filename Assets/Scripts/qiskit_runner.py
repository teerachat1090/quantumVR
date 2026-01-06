# -*- coding: utf-8 -*-
#!/usr/bin/env python3
"""
Qiskit Circuit Runner for Unity VR Quantum Simulator
รับ JSON จาก Unity → สร้าง Quantum Circuit → รันด้วย Qiskit
"""

import json
import sys
import io
import math
from qiskit import QuantumCircuit, transpile
from qiskit_aer import AerSimulator

# ตั้งค่า UTF-8 encoding สำหรับ Windows
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8")

# load from JSON
def load_circuit_from_json(json_path: str):
    with open(json_path, "r", encoding="utf-8") as f:
        return json.load(f)


def _to_radians(v):
    """
    Unity บางทีส่งมาเป็นองศา บางทีเป็นเรเดียน
    - ถ้ามี key angle_deg/theta_deg/... จะใช้ deg แน่นอน
    - ถ้าไม่มี: ถ้าค่ามากกว่า ~2π แบบชัดเจน จะเดาว่าเป็น deg
    """
    if v is None:
        return 0.0
    try:
        v = float(v)
    except:
        return 0.0

    # heuristic: ถ้าใหญ่เกิน 2π มาก ๆ ให้เดาว่าเป็น degrees
    if abs(v) > (2.0 * math.pi + 1e-6):
        return math.radians(v)
    return v


def create_quantum_circuit(circuit_data):
    num_qubits = int(circuit_data.get("num_qubits", 1))
    gates = circuit_data.get("gates", [])

    if len(gates) == 0:
        print("WARNING: No gates in circuit!")
        num_qubits = 1

    # ✅ 1 qreg + 1 creg (creg เดียว) ตาม num_qubits
    qc = QuantumCircuit(num_qubits, num_qubits)

    print(f"Creating circuit with {num_qubits} qubits")
    print(f"Adding {len(gates)} gates...")

    for i, gate in enumerate(gates):
        gate_type = str(gate.get("gate_type", "")).strip().upper()
        qubit = int(gate.get("qubit", 0))
        target = int(gate.get("target_qubit", -1))

        if qubit < 0 or qubit >= num_qubits:
            print(f"  WARNING: Gate {i+1} qubit index {qubit} out of range, skipping")
            continue

        print(f"  Gate {i+1}: {gate_type} on qubit {qubit}")

        # --------------------------
        # Single-qubit standard gates
        # --------------------------
        if gate_type in ("I", "ID"):
            qc.id(qubit)

        elif gate_type == "H":
            qc.h(qubit)

        elif gate_type == "X":
            qc.x(qubit)

        elif gate_type == "Y":
            qc.y(qubit)

        elif gate_type == "Z":
            qc.z(qubit)

        elif gate_type in ("S",):
            qc.s(qubit)

        elif gate_type in ("SDG", "ST", "St", "S-DAGGER"):
            qc.sdg(qubit)

        elif gate_type in ("T",):
            qc.t(qubit)

        elif gate_type in ("TDG", "TT", "Tt", "T-DAGGER"):
            qc.tdg(qubit)

        elif gate_type in ("SX", "sqrtX"):
            qc.sx(qubit)

        elif gate_type in ("SXDG", "SXDAG", "sqrtXt"):
            qc.sxdg(qubit)

        # --------------------------
        # Parameterized single-qubit rotations
        # --------------------------
        elif gate_type in ("RX", "RY", "RZ"):
            # รองรับ angle, angle_deg
            if "angle_deg" in gate:
                ang = math.radians(float(gate.get("angle_deg", 0)))
            else:
                ang = _to_radians(gate.get("angle", 0))

            if gate_type == "RX":
                qc.rx(ang, qubit)
            elif gate_type == "RY":
                qc.ry(ang, qubit)
            else:
                qc.rz(ang, qubit)

        # --------------------------
        # Phase gate: P(λ) / PHASE
        # --------------------------
        elif gate_type in ("P", "PHASE"):
            # รองรับ lambda / angle
            if "lambda_deg" in gate:
                lam = math.radians(float(gate.get("lambda_deg", 0)))
            elif "lambda" in gate:
                lam = _to_radians(gate.get("lambda", 0))
            elif "angle_deg" in gate:
                lam = math.radians(float(gate.get("angle_deg", 0)))
            else:
                lam = _to_radians(gate.get("angle", 0))

            qc.p(lam, qubit)

        # --------------------------
        # Universal single-qubit: U(θ, φ, λ) + aliases
        # --------------------------
        elif gate_type in ("U", "U3"):
            # key ที่รองรับ: theta/phi/lambda หรือ theta_deg/phi_deg/lambda_deg
            if "theta_deg" in gate: theta = math.radians(float(gate.get("theta_deg", 0)))
            else: theta = _to_radians(gate.get("theta", 0))

            if "phi_deg" in gate: phi = math.radians(float(gate.get("phi_deg", 0)))
            else: phi = _to_radians(gate.get("phi", 0))

            if "lambda_deg" in gate: lam = math.radians(float(gate.get("lambda_deg", 0)))
            else: lam = _to_radians(gate.get("lambda", 0))

            qc.u(theta, phi, lam, qubit)

        elif gate_type == "U2":
            # U2(φ, λ) = U(π/2, φ, λ)
            if "phi_deg" in gate: phi = math.radians(float(gate.get("phi_deg", 0)))
            else: phi = _to_radians(gate.get("phi", 0))

            if "lambda_deg" in gate: lam = math.radians(float(gate.get("lambda_deg", 0)))
            else: lam = _to_radians(gate.get("lambda", 0))

            qc.u(math.pi / 2, phi, lam, qubit)

        elif gate_type in ("U1",):
            # U1(λ) = P(λ)
            if "lambda_deg" in gate:
                lam = math.radians(float(gate.get("lambda_deg", 0)))
            elif "lambda" in gate:
                lam = _to_radians(gate.get("lambda", 0))
            elif "angle_deg" in gate:
                lam = math.radians(float(gate.get("angle_deg", 0)))
            else:
                lam = _to_radians(gate.get("angle", 0))

            qc.p(lam, qubit)

        # --------------------------
        # Two-qubit gates
        # --------------------------
        elif gate_type in ("CNOT", "CX"):
            if 0 <= target < num_qubits:
                qc.cx(qubit, target)
            else:
                print(f"  WARNING: CX requires valid target qubit (got {target})")

        elif gate_type == "CZ":
            if 0 <= target < num_qubits:
                qc.cz(qubit, target)
            else:
                print(f"  WARNING: CZ requires valid target qubit (got {target})")

        elif gate_type == "SWAP":
            if 0 <= target < num_qubits:
                qc.swap(qubit, target)
            else:
                print(f"  WARNING: SWAP requires valid target qubit (got {target})")

        else:
            print(f"  WARNING: Unknown gate type: {gate_type}")

    # ✅ สำคัญ: อย่าใช้ measure_all() เพราะมันเพิ่ม creg ใหม่ → ทำให้ผลเป็น '0 0'
    qc.measure(list(range(num_qubits)), list(range(num_qubits)))

    return qc


def run_circuit(qc, shots=1024):
    print(f"\nRunning circuit with {shots} shots...")

    simulator = AerSimulator()
    compiled = transpile(qc, simulator)
    job = simulator.run(compiled, shots=shots)
    result = job.result()
    counts = result.get_counts(compiled)
    return counts


def build_result_json(counts):
    # ✅ ล้างช่องว่างใน key กันเคสมีหลาย creg/format แปลก ๆ
    counts_clean = {str(k).replace(" ", ""): int(v) for k, v in counts.items()}

    total_shots = sum(counts_clean.values())
    sorted_counts = sorted(counts_clean.items(), key=lambda x: x[1], reverse=True)

    top_state = sorted_counts[0][0] if sorted_counts else ""
    top_prob = (sorted_counts[0][1] / total_shots) * 100 if sorted_counts and total_shots > 0 else 0.0

    return {
        "success": True,
        "total_shots": total_shots,
        "num_states": len(counts_clean),
        "top_state": top_state,
        "top_probability": top_prob,
        "counts": counts_clean
    }


def main():
    if len(sys.argv) < 2:
        print("Usage: python qiskit_runner.py <circuit_json_path>")
        sys.exit(1)

    json_path = sys.argv[1]

    try:
        print("=" * 50)
        print("QISKIT QUANTUM CIRCUIT RUNNER")
        print("=" * 50)

        circuit_data = load_circuit_from_json(json_path)
        print(f"Loaded circuit from: {json_path}")
        print(f"Circuit data: {json.dumps(circuit_data, indent=2)}")

        qc = create_quantum_circuit(circuit_data)

        print("\nCircuit Diagram:")
        print(qc.draw(output="text"))

        counts = run_circuit(qc, shots=1024)

        result_json = build_result_json(counts)
        print(json.dumps(result_json, ensure_ascii=False))

    except Exception as e:
        print(f"ERROR: {str(e)}", file=sys.stderr)
        import traceback
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
