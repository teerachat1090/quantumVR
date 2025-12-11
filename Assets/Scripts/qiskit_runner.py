#!/usr/bin/env python3
"""
Qiskit Circuit Runner for Unity VR Quantum Simulator
รับ JSON จาก Unity → สร้าง Quantum Circuit → รันด้วย Qiskit
"""

import json
import sys
from qiskit import QuantumCircuit, transpile
from qiskit_aer import AerSimulator
from qiskit.visualization import circuit_drawer

def load_circuit_from_json(json_path):
    """โหลด Circuit จาก JSON file"""
    with open(json_path, 'r') as f:
        data = json.load(f)
    return data

def create_quantum_circuit(circuit_data):
    """สร้าง Quantum Circuit จากข้อมูล JSON"""
    num_qubits = circuit_data['num_qubits']
    gates = circuit_data['gates']
    
    # สร้าง Circuit
    num_qubits = 1
    qc = QuantumCircuit(num_qubits, num_qubits)
    
    print(f"🔧 Creating circuit with {num_qubits} qubits")
    print(f"🔧 Adding {len(gates)} gates...")
    
    # เพิ่ม Gates ตามลำดับ
    for i, gate in enumerate(gates):
        gate_type = gate['gate_type'].upper()
        qubit = gate['qubit']
        target = gate.get('target_qubit', -1)
        
        print(f"  ⚡ Gate {i+1}: {gate_type} on qubit {qubit}")
        
        # Single-qubit gates
        if gate_type == 'H':
            qc.h(qubit)
        elif gate_type == 'X':
            qc.x(qubit)
        elif gate_type == 'Y':
            qc.y(qubit)
        elif gate_type == 'Z':
            qc.z(qubit)
        elif gate_type == 'S':
            qc.s(qubit)
        elif gate_type == 'T':
            qc.t(qubit)
        
        # Two-qubit gates
        elif gate_type == 'CNOT' or gate_type == 'CX':
            if target >= 0:
                qc.cx(qubit, target)
            else:
                print(f"  ⚠️  Warning: CNOT requires target qubit")
        
        elif gate_type == 'CZ':
            if target >= 0:
                qc.cz(qubit, target)
        
        else:
            print(f"  ⚠️  Unknown gate type: {gate_type}")
    
    # เพิ่ม measurement
    qc.measure_all()
    
    return qc

def run_circuit(qc, shots=1024):
    """รัน Circuit ด้วย Qiskit Aer Simulator"""
    print(f"\n🚀 Running circuit with {shots} shots...")
    
    # ใช้ AerSimulator
    simulator = AerSimulator()
    
    # Transpile และรัน
    compiled_circuit = transpile(qc, simulator)
    job = simulator.run(compiled_circuit, shots=shots)
    result = job.result()
    
    # ดึงผลลัพธ์
    counts = result.get_counts(compiled_circuit)
    
    return counts

def display_results(counts):
    """แสดงผลลัพธ์"""
    print("\n" + "="*50)
    print("📊 QUANTUM CIRCUIT RESULTS")
    print("="*50)
    
    # เรียงลำดับตาม probability
    sorted_counts = sorted(counts.items(), key=lambda x: x[1], reverse=True)
    
    total_shots = sum(counts.values())
    
    for state, count in sorted_counts[:10]:  # แสดง top 10
        probability = (count / total_shots) * 100
        bar = "█" * int(probability / 2)
        print(f"|{state}⟩: {count:4d} ({probability:5.2f}%) {bar}")
    
    print("="*50)
    
    # Return JSON สำหรับ Unity
    result_json = {
        "success": True,
        "total_shots": total_shots,
        "num_states": len(counts),
        "top_state": sorted_counts[0][0],
        "top_probability": (sorted_counts[0][1] / total_shots) * 100,
        "counts": dict(sorted_counts[:5])  # ส่งแค่ top 5 กลับไป
    }
    
    return result_json

def main():
    if len(sys.argv) < 2:
        print("Usage: python qiskit_runner.py <circuit_json_path>")
        sys.exit(1)
    
    json_path = sys.argv[1]
    
    try:
        print("="*50)
        print("🔬 QISKIT QUANTUM CIRCUIT RUNNER")
        print("="*50)
        
        # 1. โหลดข้อมูล
        circuit_data = load_circuit_from_json(json_path)
        print(f"✅ Loaded circuit from: {json_path}")
        
        # 2. สร้าง Circuit
        qc = create_quantum_circuit(circuit_data)
        
        # 3. แสดง Circuit
        print("\n📋 Circuit Diagram:")
        print(qc.draw(output='text'))
        
        # 4. รัน Circuit
        counts = run_circuit(qc, shots=1024)
        
        # 5. แสดงผลลัพธ์
        result_json = display_results(counts)
        
        # 6. ส่งผลลัพธ์กลับ Unity (ผ่าน stdout)
        print("\n📤 JSON Result:")
        print(json.dumps(result_json, indent=2))
        
        print("\n✅ Execution completed successfully!")
        
    except Exception as e:
        print(f"❌ Error: {str(e)}", file=sys.stderr)
        import traceback
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()