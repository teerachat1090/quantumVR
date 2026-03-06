# -*- coding: utf-8 -*-
# !/usr/bin/env python3
import datetime
import sys
import traceback
import json
from itertools import zip_longest
from qiskit import QuantumCircuit
from qiskit.quantum_info import Statevector

# NOTE:
# input file path will tell us if we need bloch sphere or q-sphere
# no need to create more input to indicate it

# at esic: py->python, lenovo->esicl
# cd "Assets/Scripts/New code"
# py ./sample.py "C:\Users\lenovo\AppData\LocalLow\DefaultCompany\VR quantum\QuantumData\QuantumInput\bloch_circuit_input.json"
# "C:\Users\lenovo\AppData\LocalLow\DefaultCompany\VR quantum\QuantumData\QuantumOutput\bloch_circuit_output.json"

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
    """ Load string from json file given its data path"""
    with open(json_path, "r", encoding="utf-8") as file:
        return json.load(file)

#use only single input gate
def create_bloch_sphere_circuit(circuit_data):
    """sample text"""
    qubit = circuit_data.get("qubitAmount", None)
    qubits = circuit_data.get("qubits", None)
    
    if qubit is None:
        print("Json file error: no field name (qubit - int)!")
        sys.exit(1)

    if qubits is None:
        print("Json file error: no field name (qubits - array)!")
        sys.exit(1)

    gates = qubits[0].get("gateList", None)
    qubit_list = [q.get("gateList", None) for q in qubits]


    if gates is None:
        print("Json file error: no field name (gateList - array)!")
        sys.exit(1)

    qc = QuantumCircuit(qubit)
    
    

    for gate in gates:
        gate_type = str(gate.get("gateName", None)).strip().upper()
        if gate_type is None:
            continue

        if gate_type in single_input_gate:
            single_input_gate[gate_type](qc,qubit-1)

    return qc

# New issue: make new case for Q-Sphere
def build_result_json(is_bloch_sphere : bool, qc : QuantumCircuit):
    """Get output of the circuit and put result to dictionary """

    current_datetime = datetime.datetime.now()
    formatted_time = current_datetime.strftime("%H:%M:%S")
    formatted_date = current_datetime.strftime("%b %d, %Y")
    data = {
        "date": formatted_time,
        "time": formatted_date,
        "state" : []}

    states = Statevector.from_instruction(qc)
    probs = states.probabilities()

    for i, state in enumerate(states):
        entry = {"value":i, "real_part":state.real, "imag_part":state.imag, "prob":probs[i]}
        data["state"].append(entry)


    return data

def main():
    """Main function: Start the script here"""
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
        is_bloch_sphere = circuit_data.get("blochSphere", None)

        if is_bloch_sphere is None :
            print("Json file error: no field name (blochSphere - bool)!")
            sys.exit(1)

        if is_bloch_sphere is True :
            qc = create_bloch_sphere_circuit(circuit_data)
            result_json = build_result_json(is_bloch_sphere, qc)

            with open(json_output_path, "w", encoding="utf-8") as file:
                json.dump(result_json, file, indent=4)
        else:
            #for Q-sphere
            print("for Q-sphere")
    except FileNotFoundError:
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
