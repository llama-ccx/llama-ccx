# Cantilever Beam

A simple example demonstrating the basic Lama workflow.

## Problem

A cantilever beam of length 1 m, fixed at one end, with a point load at the free end.

## Setup

1. Create a beam geometry in Rhino
2. Mesh with the **Gmsh Tetra Mesh** component
3. Assign an **Isotropic Material** (e.g. Steel: E = 210 GPa, ν = 0.3)
4. Define a **Solid Section**
5. Apply a **Fixed Support** at one end
6. Apply a **Nodal Load** at the free end
7. Add a **Linear Static Step** with output requests
8. **Assemble** the model
9. **Build Input Deck** and **Run Analysis**
10. **Read Results** and **Visualise**

!!! note
    More detailed examples with Grasshopper screenshots will be added soon.
