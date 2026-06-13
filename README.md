# quantumVR

This is Unity VR project which aim to be a learning material about basic concept of quantum computing.

## Table of Content
[About the Project](#about-the-project)<br>
[Getting Started](#getting-started) <br>
[Help](#help) <br>
[GitHub Link](#github-link) <br>
[Author](#author)

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

## Help

### Problem

**VR camera don't move by you head**
This happen because Unity still use VR simulator, we use this for testing when we don't have VR headset. To disable, do these step:

1. go to **'edit'** > **'Project Settings...'**
2. see **'XR Interaction Tool Kit'** in **'XR Plug-in Management'**
3. Deselect **'Use XR Interaction Simulator in scenes'**

Addition step: Disable simulator related GameObject in the scene e.g. 'XR Device Simulator', 'XR Interaction Simulator'. You can find it in **Hierachy** tab.

### Project Detail
All of these are in the **Project** tab
* **Source Code**: go to **Assets** > **Scripts**
* **Custom asset**: go to **Assets** > **Custom Asset**
* **Prefab**: go to **Assets** > **Prefabs**

### Saved Data
All input-output data related to quantum circuit is stored in json file at: <br>
**Windows**: "C:\\Users\\\<username\>\\AppData\\LocalLow\\quantumVR\\quantumVR" <br>

**MacOS**: "~/Library/Application Support/quantumVR/quantumVR" <br>

**Linux**: "~/.config/unity3d/quantumVR/quantumVR"

## GitHub Link
[https://github.com/teerachat1090/quantumVR/](https://github.com/teerachat1090/quantumVR/)

## Authors

* Theerakan Thadawuth 65070501029
* Tamonwan Tabloi 65070501077
* Teerachat Khuntanopajai 65070501090 