# -*- coding: utf-8 -*-
#!/usr/bin/env python3
"""this is module"""
import datetime
import sys
import traceback
import json
from qiskit import QuantumCircuit
import quantum_circuit #custom python script: needed rename

# cd "Assets/Scripts/Python File"
# py ./Quantum_Sequence.py "<json_input_path>" "<json_output_path>"

single_input_gate = quantum_circuit.single_input_gate
multi_input_gate = quantum_circuit.multi_input_gate
do_measurement_on = quantum_circuit.do_measurement_on

def qubit_sequence(circuit_data: dict):
    """
        return dictionary of sequence
    """
    qubit_amount = circuit_data.get("qubitAmount", None)
    socket_amount = circuit_data.get("socketAmount", None)
    cbit_amount = circuit_data.get("CBitAmount", None)

    if qubit_amount is None:
        print("Json file error: no field name (qubits - array)!")
        sys.exit(1)

    if socket_amount is None:
        print("Json file error: no field name (socketAmount - int)!")
        sys.exit(1)

    if cbit_amount is None:
        print("Json file error: no field name (CBitAmount - int)!")
        sys.exit(1)

    qc = QuantumCircuit(qubit_amount, qubit_amount)
    cbit_arr = [0]*cbit_amount

    current_datetime = datetime.datetime.now()
    formatted_datetime = current_datetime.strftime("%H:%M:%S")
    formatted_string = current_datetime.strftime("%b %d, %Y")

    result_list = []
    sequence_list = {
        "date" : formatted_datetime,
        "time" : formatted_string,
        "stepAmount" : 0,
        "columnList" : [],
        "resultList" : []
    }

    column_list = circuit_data.get("columnList", [])
    column_list.sort()
    sequence_list["columnList"] = column_list

    index = 0
    #print(cbit_arr)
    circuit_result = quantum_circuit.build_result_json(qc, cbit_arr=cbit_arr, use_date=False)
    # print(circuit_result)
    # print("\n\n")
    step_result = {"sequenceIndex": index}
    step_result = {**step_result, **circuit_result}
    result_list.append(step_result)
    # print("Step result", step_result)
    # print("\n\n")
    index+=1

    gate_list = circuit_data.get("gateList", [])

    for column in column_list:
        gates_in_column = [gate for gate in gate_list if gate["column"] == column]
        if not gates_in_column:
            continue

        # iterate through every gate in column
        for gate in gates_in_column:
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

            elif gate_type in single_input_gate:
                single_input_gate[gate_type](qc,control_list[0])

        #assign new step
        circuit_result = quantum_circuit.build_result_json(qc, cbit_arr=cbit_arr, use_date=False)
        step_result = {"sequenceIndex": index}
        step_result = {**step_result, **(circuit_result.copy())}
        result_list.append(step_result)
        
        # print("Step result",f"({index}): ", step_result)
        # print("\n\n")
        # print("Overall Sequence:", result_list, "\n------------------------------------------------------------------")
        del step_result
        index+=1

    # print(result_list)
    # print("\n\n")
    sequence_list["resultList"] = result_list
    sequence_list["stepAmount"] = index-1

    # print(sequence_list)
    # print("\n\n")
    return sequence_list

def main():
    """Main function: Start the script here"""
    script_name = sys.argv[0]
    if len(sys.argv) < 3:
        # print(f"Usage: python {script_name} <arg1> <arg2>")
        # print("Invalid arguments provided.")
        sys.exit(1)

    json_input_path = sys.argv[1]   # input
    json_output_path = sys.argv[2]  # output

    try:
        circuit_data = quantum_circuit.load_circuit_from_json(json_input_path)

        
        result_sequencs_json = qubit_sequence(circuit_data)
        # print(result_sequencs_json)

        with open(json_output_path, "w", encoding="utf-8") as file:
            json.dump(result_sequencs_json, file, indent=4)

        return
    except FileNotFoundError:
        traceback.print_exc(file=sys.stderr)
        sys.exit(1)
    except Exception as exception: # pylint: disable=broad-except
        error_explain = type(exception).__name__
        print(f"Other Error: {error_explain}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
