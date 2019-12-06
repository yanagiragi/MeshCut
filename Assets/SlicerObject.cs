using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Yr;

[RequireComponent(typeof(MeshFilter))]

public class SlicerObject : MonoBehaviour
{
    public GameObject Plane;

    private Vector3 planeN;
    private float planeD;

    private List<Vector3> contactPoints;

    private Mesh originalMesh;

    private void Awake()
    {
        contactPoints = new List<Vector3>();
    }

    // Start is called before the first frame update
    void Start()
    {        
        originalMesh = GetComponent<MeshFilter>().mesh;
    }

    // Update is called once per frame
    void Update()
    {
        planeN = Plane.transform.up;
        planeD = -Plane.transform.position.y;

        if (Input.GetMouseButtonDown(0))
        {
            Slice();
        }
    }

    void Slice()
    {
        Debug.Log("Slice!");

        MeshMaker leftHandSide = new MeshMaker();
        MeshMaker rightHandSide = new MeshMaker();

        Yr.MeshMaker.Triangle triangleCache = new Yr.MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);

        Vector3[] vertices = originalMesh.vertices;
        Vector3[] normals = originalMesh.normals;
        Vector2[] uvs = originalMesh.uv;
        Vector4[] tangents = originalMesh.tangents;
        
        for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; ++subMeshIndex)
        {
            int[] indices = originalMesh.GetTriangles(subMeshIndex);
            
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 p1 = vertices[indices[i + 0]];
                Vector3 p2 = vertices[indices[i + 1]];
                Vector3 p3 = vertices[indices[i + 2]];

                Vector3 n1 = normals[indices[i + 0]];
                Vector3 n2 = normals[indices[i + 1]];
                Vector3 n3 = normals[indices[i + 2]];

                Vector2 uv1 = uvs[indices[i + 0]];
                Vector2 uv2 = uvs[indices[i + 1]];
                Vector2 uv3 = uvs[indices[i + 2]];

                Vector4 tangent1 = tangents[indices[i + 0]];
                Vector4 tangent2 = tangents[indices[i + 1]];
                Vector4 tangent3 = tangents[indices[i + 2]];

                triangleCache.vertices[0] = p1;
                triangleCache.vertices[1] = p2;
                triangleCache.vertices[2] = p3;

                triangleCache.normals[0] = n1;
                triangleCache.normals[1] = n2;
                triangleCache.normals[2] = n3;

                triangleCache.uvs[0] = uv1;
                triangleCache.uvs[1] = uv2;
                triangleCache.uvs[2] = uv3;

                triangleCache.tangents[0] = tangent1;
                triangleCache.tangents[1] = tangent2;
                triangleCache.tangents[2] = tangent3;

                bool isP1LeftHandSide = isLeftSideFromPlane(planeN, planeD, p1);
                bool isP2LeftHandSide = isLeftSideFromPlane(planeN, planeD, p2);
                bool isP3LeftHandSide = isLeftSideFromPlane(planeN, planeD, p3);

                if(isP1LeftHandSide == isP2LeftHandSide && isP1LeftHandSide == isP3LeftHandSide)
                {
                    // whole triangle belongs to same side of the plane
                    if (isP1LeftHandSide) // Left Side
                    {
                        leftHandSide.AddTriangle(triangleCache);
                    }
                    else
                    {
                        rightHandSide.AddTriangle(triangleCache);
                    }
                }
                else
                {
                    // need to deal with cuts
                    TrianglePlaneIntersect(p1, p2, p3, contactPoints);
                }
            }

            Mesh leftHandSideMesh = leftHandSide.GetMesh("Left HandSide Mesh");
            Mesh rightHandSideMesh = rightHandSide.GetMesh("Right HandSide Mesh");

            Material[] mats = GetComponent<MeshRenderer>().sharedMaterials;

            GameObject leftHandSideObject = new GameObject("Left Hand Side", typeof(MeshFilter), typeof(MeshRenderer));
            leftHandSideObject.transform.position = transform.position;
            leftHandSideObject.transform.rotation = transform.rotation;
            leftHandSideObject.transform.localScale = transform.localScale;
            if (transform.parent != null)
            {
                leftHandSideObject.transform.parent = transform.parent;
            }
            leftHandSideObject.GetComponent<MeshFilter>().mesh = leftHandSideMesh;
            leftHandSideObject.GetComponent<MeshRenderer>().materials = mats;

            GameObject rightHandSideObject = new GameObject("Right Hand Side", typeof(MeshFilter), typeof(MeshRenderer));
            rightHandSideObject.transform.position = transform.position;
            rightHandSideObject.transform.rotation = transform.rotation;
            rightHandSideObject.transform.localScale = transform.localScale;
            if (transform.parent != null)
            {
                rightHandSideObject.transform.parent = transform.parent;
            }
            rightHandSideObject.GetComponent<MeshFilter>().mesh = rightHandSideMesh;
            rightHandSideObject.GetComponent<MeshRenderer>().materials = mats;

            // For Debug
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && contactPoints != null && contactPoints.Count > 0)
        {
            foreach (var v3 in contactPoints)
            {
                Gizmos.DrawWireSphere(v3, 0.1f);
            }
        }
    }

    float DistFromPlane(Vector3 planeN, float planeD, Vector3 point)
    {
        return Vector3.Dot(planeN, point) + planeD;
    }

    bool isLeftSideFromPlane(Vector3 planeN, float planeD, Vector3 point)
    {
        return (Vector3.Dot(planeN, point) + planeD) > 0.0f;
    }

    void GetSegmentPlaneIntersect(Vector3 p1, Vector3 p2, List<Vector3> result)
    {
        float d1 = DistFromPlane(planeN, planeD, p1);
        float d2 = DistFromPlane(planeN, planeD, p2);

        bool p1OnPlane = Mathf.Abs(d1) < Mathf.Epsilon;
        bool p2OnPlane = Mathf.Abs(d2) < Mathf.Epsilon;

        if (p1OnPlane)
        {
            result.Add(p1);
        }

        if (p2OnPlane)
        {
            result.Add(p2);
        }

        if (p1OnPlane && p2OnPlane)
        {
            return;
        }

        if (d1 * d2 > Mathf.Epsilon) {
            return ;
        }
                
        float t = d1 / (d1 - d2);
        result.Add(p1 + t * (p2 - p1));
    }

    void TrianglePlaneIntersect(Vector3 p1, Vector3 p2, Vector3 p3, List<Vector3> result)
    {
        GetSegmentPlaneIntersect(p1, p2, result);
        GetSegmentPlaneIntersect(p2, p3, result);
        GetSegmentPlaneIntersect(p3, p1, result);
    }
    
}
