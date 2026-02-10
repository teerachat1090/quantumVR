# -*- coding: utf-8 -*-
#!/usr/bin/env python3
from qiskit import QuantumCircuit
from qiskit.quantum_info import Statevector, Pauli
import datetime
import sys
import traceback
import json
import math

# NOTE:
# input file path will tell us if we need bloch sphere or q-sphere
# no need to create more input to indicate it

# cd "Assets/Scripts/New code"
# py ./sample.py "C:\Users\Lenovo\AppData\LocalLow\DefaultCompany\VR quantum\QuantumData\QuantumInput\bloch_circuit_input.json" 
# "C:\Users\Lenovo\AppData\LocalLow\DefaultCompany\VR quantum\QuantumData\QuantumOutput\bloch_circuit_output.json"

# single_input_gate[<key>](qc)(qubit)
# qc = QuantumCircuit(n)
# qubit -> position of qubit
single_input_gate = {
    "I":lambda qc, qubit : qc.id(qubit),
    "H":lambda qc, qubit : qc.h(qubit),
    "X":lambda qc, qubit : qc.x(qubit),
    "Y":lambda qc, qubit : qc.y(qubit),
    "Z":lambda qc, qubit : qc.z(qubit),
    "S":lambda qc, qubit : qc.s(qubit),
    "ST":lambda qc, qubit : qc.sdg(qubit), #S-dagger
    "T":lambda qc, qubit : qc.t(qubit),
    "TT":lambda qc, qubit : qc.tdg(qubit), #T-dagger
    "SQRTX":lambda qc, qubit : qc.sx(qubit),
    "SQRTXT":lambda qc, qubit : qc.sxdg(qubit), #square root x - dagger
}

# get json data
def load_circuit_from_json(json_path: str):
    with open(json_path, "r", encoding="utf-8") as file:
        return json.load(file)

#use only single input gate
def create_bloch_sphere_circuit(circuit_data):
    qubit = 1
    qubits = circuit_data.get("qubits", None)
    if(qubits is None):
        print("Json file error: no field name (qubits - array)!")
        sys.exit(1)

    gates = qubits[0].get("gateList", None)

    if(gates is None):
        print("Json file error: no field name (gateList - array)!")
        sys.exit(1)
    
    qc = QuantumCircuit(qubit)
    for gate in gates:
        gate_type = str(gate.get("gateName", "")).strip().upper()
        if gate_type is None:
            continue

        if gate_type in single_input_gate:
            single_input_gate[gate_type](qc,qubit-1)
    
    return qc

# TODO: make new case for Q-Sphere
def build_result_json(is_Bloch_Sphere, qc):
    states = Statevector.from_instruction(qc)
    #print("Statevector:", state.data)
    
    probs = states.probabilities()
    #print("Probabilities:", probs)

    #print(qc.draw())

    current_datetime = datetime.datetime.now()
    formatted_datetime = current_datetime.strftime("%H:%M:%S")
    formatted_string = current_datetime.strftime("%b %d, %Y")

    data = {
        "date": formatted_datetime,
        "time": formatted_string,
        "state" : []
    }

    for i, state in enumerate(states):
        entry = {"value":i, "real_part":state.real, "imag_part":state.imag, "prob":probs[i]}
        data["state"].append(entry)
    # print(data)

    return data

def main():
    script_name = sys.argv[0]
    if len(sys.argv) < 3:
        print(f"Usage: python {script_name} <arg1> <arg2>")
        print("Invalid arguments provided.")
        sys.exit(1)

    json_input_path = sys.argv[1]   # input
    json_output_path = sys.argv[2]  # output
    
    try:
        #json data type
        circuit_data = load_circuit_from_json(json_input_path)
        is_Bloch_Sphere = circuit_data.get("blochSphere", None)

        if(is_Bloch_Sphere is None):
            print("Json file error: no field name (blochSphere - bool)!")
            sys.exit(1)

        if(is_Bloch_Sphere is True):
            qc = create_bloch_sphere_circuit(circuit_data)
            result_json = build_result_json(is_Bloch_Sphere, qc)
            with open(json_output_path, "w", encoding="utf-8") as file:
                json.dump(result_json, file, indent=4)
        else:
            #for Q-sphere
            print("for Q-sphere")
    except:
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()