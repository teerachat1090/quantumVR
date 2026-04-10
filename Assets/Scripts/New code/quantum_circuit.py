"""This module will get circuit data from json, calculate, and send result as json"""
# -*- coding: utf-8 -*-
# !/usr/bin/env python3
import datetime
import sys
import traceback
import json

import numpy as np
from qiskit import QuantumCircuit
from qiskit.quantum_info import Statevector

# NOTE:
# input file path will tell us if we need bloch sphere or q-sphere
# no need to create more input to indicate it

# at esic: py->python, lenovo->esicl
# cd "Assets/Scripts/New code"
# py ./quantum_circuit.py <json input path>

# single_input_gate[<key>](qc)(qubit_amount)
# qc = QuantumCircuit(n)
# qubit_amount -> position of qubit_amount
single_input_gate = {
    "I":lambda qc, qubit_amount : qc.id(qubit_amount),
    "H":lambda qc, qubit_amount : qc.h(qubit_amount),
    "X":lambda qc, qubit_amount : qc.x(qubit_amount),
    "Y":lambda qc, qubit_amount : qc.y(qubit_amount),
    "Z":lambda qc, qubit_amount : qc.z(qubit_amount),
    "S":lambda qc, qubit_amount : qc.s(qubit_amount),
    "ST":lambda qc, qubit_amount : qc.sdg(qubit_amount), #S-dagger
    "T":lambda qc, qubit_amount : qc.t(qubit_amount),
    "TT":lambda qc, qubit_amount : qc.tdg(qubit_amount), #T-dagger
    "SQRTX":lambda qc, qubit_amount : qc.sx(qubit_amount),
    "SQRTXT":lambda qc, qubit_amount : qc.sxdg(qubit_amount), #square root x - dagger
}

multi_input_gate = {
    "CNOT": lambda qc, control, target : qc.cx(*control, *target)
}

# get json data
def load_circuit_from_json(json_path: str):
    """ Load string from json file given its data path"""
    with open(json_path, "r", encoding="utf-8") as file:
        return json.load(file)

def create_circuit(circuit_data: str):
    """A method to extract data from new json format"""
    qubit_amount = circuit_data.get("qubitAmount", None)
    socket_amount = circuit_data.get("socketAmount", None)

    if qubit_amount is None:
        print("Json file error: no field name (qubitAmount - int)!")
        sys.exit(1)

    if socket_amount is None:
        print("Json file error: no field name (socketAmount - int)!")
        sys.exit(1)

    qc = QuantumCircuit(qubit_amount)

    column_list = circuit_data.get("columnList", None)
    if not column_list:
        return qc

    gate_list = circuit_data.get("gateList", None)

    for column in column_list:
        col_list = [gate for gate in gate_list if gate["column"] == column]
        if not col_list:
            continue

        for gate in col_list:
            gate_type = str(gate.get("name", None)).strip().upper()

            target_list = gate.get("targetRow", None)
            control_list = gate.get("controlRow", None)

            if not control_list:
                continue

            if target_list:
                multi_input_gate[gate_type](qc, control_list, target_list)
            else:
                single_input_gate[gate_type](qc,control_list[0])
    return qc

# New issue: make new case for Q-Sphere
def build_result_json(qc : QuantumCircuit):
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
    phase = [p+360 if p < 0 else p for p in np.angle(states.data, deg=True)]

    for i, state in enumerate(states):
        entry = {
            "value":i, "real_part":state.real, "imag_part":state.imag, 
            "phase":phase[i], 
            "prob":probs[i]
        }
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

        qc = create_circuit(circuit_data)
        result_json = build_result_json(qc)

        with open(json_output_path, "w", encoding="utf-8") as file:
            json.dump(result_json, file, indent=4)

    except FileNotFoundError:
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
