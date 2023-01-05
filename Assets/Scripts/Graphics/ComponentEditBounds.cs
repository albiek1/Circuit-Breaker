using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ComponentEditBounds : MonoBehaviour
{
    public float thickness = 0.1f;
    public Material material;
    public Transform inputSignalArea;
    public Transform outputSignalArea;

    Mesh quadMesh;
    Matrix4x4[] trs;

    private void Start()
    {
        if (Application.isPlaying)
        {
            MeshShapeCreator.CreateQuadMesh(ref quadMesh);
            CreateMatrices();
        }
    }

    void UpdateSignalAreaSizeAndPos(Transform signalArea)
    {
        signalArea.position = new Vector3(signalArea.position.x, transform.position.y, signalArea.position.z);
        signalArea.localScale = new Vector3(signalArea.localScale.x, transform.localScale.y, 1);
    }

    void CreateMatrices()
    {
        Vector3 center = transform.position;
        float width = Mathf.Abs(transform.localScale.x);
        float height = Mathf.Abs(transform.localScale.y);

        Vector3[] edgeCenters =
        {
            center + Vector3.left * width / 2,
            center + Vector3.right * width/ 2,
            center + Vector3.up * width / 2,
            center + Vector3.down * width / 2
        };

        Vector3[] edgeScales =
        {
            new Vector3(thickness, height + thickness, 1),
            new Vector3(thickness, height + thickness, 1),
            new Vector3(width + thickness, thickness, 1),
            new Vector3(width + thickness, thickness, 1)
        };

        trs = new Matrix4x4[4];
        for(int i = 0; i < 4; i++)
        {
            trs[i] = Matrix4x4.TRS(edgeCenters[i], Quaternion.identity, edgeScales[i]);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            MeshShapeCreator.CreateQuadMesh(ref quadMesh);
            CreateMatrices();
            UpdateSignalAreaSizeAndPos(inputSignalArea);
            UpdateSignalAreaSizeAndPos(outputSignalArea);
        }

        for(int i = 0; i < 4; i++)
        {
            Graphics.DrawMesh(quadMesh, trs[i], material, 0);
        }
    }
}
