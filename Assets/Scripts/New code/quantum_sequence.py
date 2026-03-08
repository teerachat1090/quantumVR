# -*- coding: utf-8 -*-
#!/usr/bin/env python3
"""this is module"""
import datetime
import sys
import traceback
import json
from qiskit import QuantumCircuit
import quantum_circuit #custom python script: needed rename

# cd "Assets/Scripts/New code"
# py ./QuantumSequence.py "C:\Users\Lenovo\AppData\LocalLow\DefaultCompany\VR quantum\QuantumData\QuantumInput\bloch_circuit_input.json" 
# "C:\Users\Lenovo\AppData\LocalLow\DefaultCompany\VR quantum\QuantumData\QuantumOutput\bloch_circuit_sequence.json"

single_input_gate = quantum_circuit.single_input_gate

def single_qubit_sequence(circuit_data: dict):
    """_summary_

    Args:
        circuit_data (str): _description_

    Returns:
        _type_: _description_
    """
    qubit = 1
    qubits = circuit_data.get("qubits", None)
    if qubits is None :
        print("Json file error: no field name (qubits - array)!")
        sys.exit(1)

    gates = qubits[0].get("gateList", None)

    if gates is None :
        print("Json file error: no field name (gateList - array)!")
        sys.exit(1)

    current_datetime = datetime.datetime.now()
    formatted_datetime = current_datetime.strftime("%H:%M:%S")
    formatted_string = current_datetime.strftime("%b %d, %Y")

    result_list = []
    sequence_list = {
        "date" : formatted_datetime,
        "time" : formatted_string,
        "gateAmount" : 0,
        "resultList" : None
    }
    qc = QuantumCircuit(qubit)

    index=0
    qubit_result_list = quantum_circuit.build_result_json(qc)
    qubit_result = { "sequenceIndex": index}
    qubit_result = {**qubit_result, **qubit_result_list} # merge dictionary
    result_list.append(qubit_result)
    index += 1

    for  gate in list(gates):
        gate_type = str(gate.get("gateName", None)).strip().upper()
        if gate_type is None:
            continue

        if gate_type in single_input_gate:
            single_input_gate[gate_type](qc,qubit-1)
            qubit_result_list = quantum_circuit.build_result_json(qc)

            qubit_result = { "sequenceIndex": index}
            qubit_result = {**qubit_result, **qubit_result_list}
            result_list.append(qubit_result)

            index+=1

    sequence_list["gateAmount"] = index-1
    sequence_list["resultList"] = result_list
    return sequence_list

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
        circuit_data = quantum_circuit.load_circuit_from_json(json_input_path)
        is_bloch_sphere = circuit_data.get("blochSphere", None)
        if is_bloch_sphere is None :
            print("Json file error: no field name (blochSphere - bool)!")
            sys.exit(1)

        if is_bloch_sphere is True :
            result_sequencs_json = single_qubit_sequence(circuit_data)

            with open(json_output_path, "w", encoding="utf-8") as file:
                json.dump(result_sequencs_json, file, indent=4)

        else:
            #for Q-sphere
            print("for Q-sphere")

        return
    except FileNotFoundError:
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
