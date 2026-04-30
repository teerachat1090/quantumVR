"""This module will get circuit data from json, calculate, and send result as json"""
# -*- coding: utf-8 -*-
# !/usr/bin/env python3
import datetime
import sys
import traceback
import json
import math

import numpy as np
from qiskit import QuantumCircuit
from qiskit.quantum_info import Statevector

# NOTE:
# input file path will tell us if we need bloch sphere or q-sphere
# no need to create more input to indicate it

# at esic: py->python, lenovo->esicl
# cd "Assets/Scripts/Python File"
# py ./quantum_circuit.py <json input path> <json output path>

# single_input_gate[<key>](qc)(qubit_amount)
# qc = QuantumCircuit(n)
# qubit_amount -> position of qubit_amount
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

input_related_gate = {
    "P":lambda qc, qubit, phase : qc.p(phase, qubit),
    "RZ": lambda qc, qubit, phase : qc.rz(phase, qubit),
    "RX": lambda qc, qubit, phase : qc.rx(phase, qubit),
    "RY": lambda qc, qubit, phase : qc.ry(phase, qubit)
}

multi_input_gate = {
    "CNOT": lambda qc, control, target : qc.cx(*control, *target)
}

# get json data
def load_circuit_from_json(json_path: str):
    """ Load string from json file given its data path"""
    with open(json_path, "r", encoding="utf-8") as file:
        return json.load(file)

def do_measurement_on(qc, qubit_measured: int):
    """simulate state vector after measurement like in IBM composer"""
    sv = Statevector(qc)
    outcome, collapsed_sv = sv.measure([qubit_measured])
    collapsed_sv = collapsed_sv / np.linalg.norm(collapsed_sv.data)
    qc_new = QuantumCircuit(qc.num_qubits)
    qc_new.initialize(collapsed_sv.data)
    return int(outcome[0]), qc_new

def create_circuit(circuit_data):
    """A method to extract data from new json format"""
    qubit_amount = circuit_data.get("qubitAmount", None)
    socket_amount = circuit_data.get("socketAmount", None)
    cbit_amount = circuit_data.get("CBitAmount", None)

    if qubit_amount is None:
        print("Json file error: no field name (qubitAmount - int)!")
        sys.exit(1)

    if socket_amount is None:
        print("Json file error: no field name (socketAmount - int)!")
        sys.exit(1)

    if cbit_amount is None:
        print("Json file error: no field name (CBitAmount - int)!")
        sys.exit(1)

    qc = QuantumCircuit(qubit_amount, qubit_amount)
    cbit_arr = [0]*cbit_amount

    column_list = circuit_data.get("columnList", None)
    if not column_list:
        return qc, cbit_arr

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
                if gate.get("classical", False):
                    if gate.get("condition", False) and cbit_arr[target_list[0]]:
                        single_input_gate[gate_type](qc,control_list[0])
                    else:
                        cbit_arr[target_list[0]], qc = do_measurement_on(qc, control_list[0])
                elif gate_type in multi_input_gate:
                    multi_input_gate[gate_type](qc, control_list, target_list)

            elif gate.get("useInput", False):
                input_value = gate.get("input", 0)
                input_related_gate[gate_type](qc, control_list[0], math.radians(input_value))

            elif gate_type in single_input_gate:
                single_input_gate[gate_type](qc,control_list[0])

    return qc, cbit_arr

# New issue: make new case for Q-Sphere
def build_result_json(qc : QuantumCircuit, cbit_arr : list[int], use_date : bool = True):
    """Get output of the circuit and put result to dictionary """

    if cbit_arr is None:
        cbit_arr = []

    current_datetime = datetime.datetime.now()
    formatted_time = current_datetime.strftime("%H:%M:%S")
    formatted_date = current_datetime.strftime("%b %d, %Y")
    data = {
        **({"date": formatted_time} if use_date else {}),
        **({"time": formatted_date} if use_date else {}),
        "cBits" : cbit_arr,
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

        qc, cbit_arr = create_circuit(circuit_data)
        result_json = build_result_json(qc, cbit_arr=cbit_arr)

        with open(json_output_path, "w", encoding="utf-8") as file:
            json.dump(result_json, file, indent=4)

    except FileNotFoundError:
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
