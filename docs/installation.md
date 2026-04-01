# Installation

Grasshopper is a visual programming language for Rhino3D, a popular 3D modelling software.
You can install Lama using the **Package Manager** or **manually**.

## Package Manager

1. Open Rhino3D and launch the **Package Manager** from the _Tools_ menu — or click the [link](rhino://package/search?name=Lama).

2. Search for **Lama** and click **Install**. The Package Manager will download and install the plugin.
3. Restart Rhino3D to activate the plugin.

!!! note
    If you have correctly followed the steps above, you will find the **Lama** tab on the Grasshopper toolbar.

## Install CalculiX

Lama requires a [CalculiX](http://www.calculix.de/) (`ccx`) executable to run analyses.

1. Download CalculiX from [calculix.de](http://www.calculix.de/) or use a prebuilt binary such as [bConverged](https://bconverged.com/calculix/).
2. Note the path to `ccx.exe` — you will need it in the **Run Analysis** component.

!!! note
    You can set the CalculiX path once in the **Run with Exe** component and it will be remembered for future sessions.
