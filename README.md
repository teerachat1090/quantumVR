# quantumVR

This is Unity VR project which aim to be a learning material about basic concept of quantum computing.

## Table of Content
[About the Project](#about-the-project) <br>
[Getting Started](#getting-started) <br>
[Project Detail](#project-detail) <br>
[Help](#help) <br>
[XR Simulator](#xr-simulator) <br>
[GitHub Link](#github-link) <br>
[Authors](#authors)

## About the Project

This project is a part of senior project, named "**VR-Based Interactive Learning Tool for Quantum Information Concepts**". The project is a part of education Bachelor of Engineering course from Computer Engineering deprtment in engineering faculty of King Mongkut's University of Technology Thonburi (KMUTT), semister 2025.

## Getting Started

### Dependencies

* **Unity Editor Version**: Unity 6 (6000.3.10f1 LTS)
* **OS**: Windows, MacOS, Linux
* **Platform**: VR (Meta Quest 3)
* **External Dependency**: Git LFS

### Installation & Setup

1. **Clone the repository / Import project**
2. **Open project**
    * Launch Unity Hub
    * Click **'Add'** and select **'Add project from disk'**
    * Select project location. The Unity project folder is inside the main project with same name. (**'.../quantumVR/quantumVR'**)
    * Click project on the list to open.
3. **Load main scene**: go to **Project tab > Assets > Scenes > MainMenu**
4. **Start game**: press **'Play'** at the top of screen.

<br>**ฺBecause we use Git Large File Storage (LFS) for storing large files like .mp4, we not recommend downloading this project as zip; this will make those files corrupted. We advises you to clone this project by using git instead.

## Project Detail
<p>Our objective is to develop virtual reality (VR) to make user understand funtamental quantum information concept.

<p>Our software consist of 6 parts:<p>


0. **Main menu**: This is the first place where user start this software and choose learning topics.
![Main menu](./Image/main%20menu.jpg)
&nbsp;

1. **Qubit state & Bloch sphere**: This part will show user the representation of single qubit, Bloch sphere, and a way to interact with by using quantum gates.
![Chapter 1](./Image/chapter%201%20-%20Qubit%20State%20&%20Bloch%20sphere.jpg)
&nbsp;

2. **Entanglement**: This part will show user the representation of multiple qubits, Q-sphere, and a classical bit including a way to interact with by using quantum gates.
![Chapter 2](./Image/chapter%202%20-%20Entanglement.jpg)
&nbsp;

3. **Quantum Teleportation**: This part will show user about how to send quantum information through network, how to make signal stronger and interactive network topology.
![Chapter 3](./Image/chapter%203%20-%20Quantum%20Teleportation.jpg)
&nbsp;

4. **Quantum Network**: This part will show user about how to send quantum information through network and simple network topology.
![Chapter 4: intro](./Image/chapter%204%20-%20Quantum%20Network.jpg) &nbsp;
![Chapter 4: repeater](./Image/chapter%204%20-%20Quantum%20Network(1).jpg) &nbsp;
![Chapter 4: topology](./Image/chapter%204%20-%20Quantum%20Network(2).jpg) &nbsp;
&nbsp;

5. **Sandbox Mode**: This is where user can freely create thier own quantum circuit and it  will display the result using diagram (Q-sphere). The scene will be the same as chapter 2 but there's no guide showing here.

## Help

### Problem

**VR camera don't move by you head**
This happen because Unity still use VR simulator, we use this for testing when we don't have VR headset. To disable, do these step:

1. go to **'edit'** > **'Project Settings...'**
2. see **'XR Interaction Tool Kit'** in **'XR Plug-in Management'**
3. Deselect **'Use XR Interaction Simulator in scenes'**

Addition step: Disable simulator related GameObject in the scene e.g. 'XR Device Simulator', 'XR Interaction Simulator'. You can find it in **Hierachy** tab.

### Project Resource
All of these are in the **Project** tab
* **Source Code**: go to **Assets** > **Scripts**
* **Custom asset**: go to **Assets** > **Custom Asset**
* **Prefab**: go to **Assets** > **Prefabs**

### Saved Data
All input-output data related to quantum circuit is stored in json file at: <br>

* **Windows**: "C:\\Users\\\<username\>\\AppData\\LocalLow\\quantumVR\\quantumVR" <br>
* **MacOS**: "~/Library/Application Support/quantumVR/quantumVR" <br>
* **Linux**: "~/.config/unity3d/quantumVR/quantumVR"

## XR Simulator
In case of not having VR headset, you can use Unity XR Device Simulator for testing instead by doing this step:

1. go to **'edit'** > **'Project Settings...'**
2. see **'XR Interaction Tool Kit'** in **'XR Plug-in Management'**
3. Select **'Use XR Interaction Simulator in scenes'**
4. Select prefab and find it in **Project tab**
5. drag prefab to **Hierachy tab** > **XR** > **Origin XR** and enable it

After press play, you can control player by using WASD for moving on the ground and QE for moving up and down.
(More control option can be seen in the XR Device Simuator UI)

## GitHub Link
[https://github.com/teerachat1090/quantumVR/](https://github.com/teerachat1090/quantumVR/)

## Authors

* Theerakan Thadawuth 65070501029
* Tamonwan Tabloi 65070501077
* Teerachat Khuntanopajai 65070501090 
