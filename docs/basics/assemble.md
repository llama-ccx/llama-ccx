# Assemble Model

The **CcxModel** component assembles all parts of the structural model into a single CalculiX model ready for analysis.

## Inputs

| Input              | Description                              |
| ------------------ | ---------------------------------------- |
| Mesh / Model       | The meshed structural model              |
| Materials          | Material definitions                     |
| Sections           | Section assignments                      |
| Boundary Conditions| Supports and constraints                 |
| Loads              | Applied loads                            |
| Steps              | Analysis steps with output requests      |

## Output

A complete CalculiX model object that can be passed to the **Build Input Deck** or **Run Analysis** components.
