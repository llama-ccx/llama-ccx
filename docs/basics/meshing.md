# Meshing

Lama integrates [Gmsh](https://gmsh.info/) for tetrahedral mesh generation.

## Tetra Mesh

The **Gmsh Tetra Mesh** component generates a tetrahedral mesh from a Brep geometry.

### Parameters

| Input         | Description                          |
| ------------- | ------------------------------------ |
| Brep          | The geometry to mesh                 |
| Mesh Size     | Target element size                  |
| Mesh Order    | Element order (1 = linear, 2 = quadratic) |

### Output

A tetrahedral mesh that can be converted to a Lama structural model using the **Tetra Mesh to Model** component.

## Hex Mesh

Hexahedral meshes can be imported and converted using the **Hex Mesh to Model** component.
