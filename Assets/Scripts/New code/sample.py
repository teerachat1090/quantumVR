# -*- coding: utf-8 -*-
#!/usr/bin/env python3
from qiskit import QuantumCircuit
from qiskit.circuit.library import CXGate

def func( a:int = 0, b:int = 1):
    return a + b

def func2():
    # Create a circuit with 2 qubits
    qc = QuantumCircuit(3)

    # Add a Hadamard gate to qubit 0
    qc.h(0)

    # Add a CNOT gate (control: 0, target: 1)
    qc.cx(0, 2)
    qc.x(1)
    print(qc.draw())

def main():
    print(func(5))
    print("Hello world")
    func2()

if __name__ == "__main__":
    main()