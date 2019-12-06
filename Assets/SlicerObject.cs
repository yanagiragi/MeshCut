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

    MeshMaker leftHandSide = new MeshMaker();
    MeshMaker rightHandSide = new MeshMaker();

    // List to store new vertices generate from the slice
    List<Vector3> newVerticesCache = new List<Vector3>();

    Yr.MeshMaker.Triangle triangleCache = new Yr.MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);

    void Update()
    {
        planeN = Plane.transform.up;
        planeD = -Plane.transform.position.y;

        if (Input.GetMouseButtonDown(0))
        {
            Slice(gameObject);
        }
    }

    void Slice(GameObject victim)
    {
        Debug.Log("Slice!");

        Mesh originalMesh = victim.GetComponent<MeshFilter>().mesh;

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
                    CutTriangle(ref triangleCache, subMeshIndex);
                }
            }

            // TODO: fill the cap

            Mesh leftHandSideMesh = leftHandSide.GetMesh("Left HandSide Mesh");
            Mesh rightHandSideMesh = rightHandSide.GetMesh("Right HandSide Mesh");

            Material[] mats = GetComponent<MeshRenderer>().sharedMaterials;
            
            // TODO: deal cap material

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

    Yr.MeshMaker.Triangle leftTriangleCache = new Yr.MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    Yr.MeshMaker.Triangle rightTriangleCache = new Yr.MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    Yr.MeshMaker.Triangle newTriangleCache = new Yr.MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
    void CutTriangle(ref Yr.MeshMaker.Triangle triangleCache, int subMeshIndex)
    {
        // first, we split three points into two sides

        int leftCount = 0, rightCount = 0;

        for (int i = 0; i < 3; ++i)
        {
            bool isLeftHandSide = isLeftSideFromPlane(planeN, planeD, triangleCache.vertices[i]);

            if (isLeftHandSide)
            {
                leftTriangleCache.vertices[leftCount] = triangleCache.vertices[i];
                leftTriangleCache.uvs[leftCount] = triangleCache.uvs[i];
                leftTriangleCache.tangents[leftCount] = triangleCache.tangents[i];
                leftTriangleCache.normals[leftCount] = triangleCache.normals[i];
                ++leftCount;
            }
            else
            {
                rightTriangleCache.vertices[rightCount] = triangleCache.vertices[i];
                rightTriangleCache.uvs[rightCount] = triangleCache.uvs[i];
                rightTriangleCache.tangents[rightCount] = triangleCache.tangents[i];
                rightTriangleCache.normals[rightCount] = triangleCache.normals[i];
                ++rightCount;
            }
        }

        // Second, we fill the intersect points into newTriangleCache
        
        // Get single point that has no other point in the same side
        if (leftCount == 1)
        {
            triangleCache.vertices[0] = leftTriangleCache.vertices[0];
            triangleCache.uvs[0]      = leftTriangleCache.uvs[0];
            triangleCache.normals[0]  = leftTriangleCache.normals[0];
            triangleCache.tangents[0] = leftTriangleCache.tangents[0];

            triangleCache.vertices[1] = rightTriangleCache.vertices[0];
            triangleCache.uvs[1]      = rightTriangleCache.uvs[0];
            triangleCache.normals[1]  = rightTriangleCache.normals[0];
            triangleCache.tangents[1] = rightTriangleCache.tangents[0];

            triangleCache.vertices[2] = rightTriangleCache.vertices[1];
            triangleCache.uvs[2] = rightTriangleCache.uvs[1];
            triangleCache.normals[2] = rightTriangleCache.normals[1];
            triangleCache.tangents[2] = rightTriangleCache.tangents[1];
        }
        else
        {
            triangleCache.vertices[0] = rightTriangleCache.vertices[0];
            triangleCache.uvs[0]      = rightTriangleCache.uvs[0];
            triangleCache.normals[0]  = rightTriangleCache.normals[0];
            triangleCache.tangents[0] = rightTriangleCache.tangents[0];

            triangleCache.vertices[1] = leftTriangleCache.vertices[0];
            triangleCache.uvs[1]      = leftTriangleCache.uvs[0];
            triangleCache.normals[1]  = leftTriangleCache.normals[0];
            triangleCache.tangents[1] = leftTriangleCache.tangents[0];

            triangleCache.vertices[2] = leftTriangleCache.vertices[1];
            triangleCache.uvs[2]      = leftTriangleCache.uvs[1];
            triangleCache.normals[2]  = leftTriangleCache.normals[1];
            triangleCache.tangents[2] = leftTriangleCache.tangents[1];
        }

        // Get intersect point

        float d1 = 0.0f, d2 = 0.0f;
        float normalizedDistance = 0.0f;

        // Deal intersect point 1
        d1 = DistFromPlane(planeN, planeD, triangleCache.vertices[0]);
        d2 = DistFromPlane(planeN, planeD, triangleCache.vertices[1]);
        normalizedDistance = d1 / (d1 - d2);
        
        newTriangleCache.vertices[0] = Vector3.Lerp(triangleCache.vertices[0], triangleCache.vertices[1], normalizedDistance);
        newTriangleCache.uvs[0]      = Vector2.Lerp(triangleCache.uvs[0],      triangleCache.uvs[1],      normalizedDistance);
        newTriangleCache.normals[0]  = Vector3.Lerp(triangleCache.normals[0],  triangleCache.normals[1],  normalizedDistance);
        newTriangleCache.tangents[0] = Vector4.Lerp(triangleCache.tangents[0], triangleCache.tangents[1], normalizedDistance);

        // Deal intersect point 2
        d1 = DistFromPlane(planeN, planeD, triangleCache.vertices[0]);
        d2 = DistFromPlane(planeN, planeD, triangleCache.vertices[2]);
        normalizedDistance = d1 / (d1 - d2);

        newTriangleCache.vertices[1] = Vector3.Lerp(triangleCache.vertices[0], triangleCache.vertices[2], normalizedDistance);
        newTriangleCache.uvs[1]      = Vector2.Lerp(triangleCache.uvs[0],      triangleCache.uvs[2],      normalizedDistance);
        newTriangleCache.normals[1]  = Vector3.Lerp(triangleCache.normals[0],  triangleCache.normals[2],  normalizedDistance);
        newTriangleCache.tangents[1] = Vector4.Lerp(triangleCache.tangents[0], triangleCache.tangents[2], normalizedDistance);

        // record new create points
        if (newTriangleCache.vertices[0] != newTriangleCache.vertices[1])
        {
            newVerticesCache.Add(newTriangleCache.vertices[0]);
            newVerticesCache.Add(newTriangleCache.vertices[1]);
        }

        // Third, Connect intersect points with vertices

        // Again, Get Single point of the side. But this time we fill different data
        if (leftCount == 1)
        {
            // Connect Triangle: Single, intersect p1, intersect p2
            triangleCache.vertices[0] = leftTriangleCache.vertices[0];
            triangleCache.uvs[0]      = leftTriangleCache.uvs[0];
            triangleCache.normals[0]  = leftTriangleCache.normals[0];
            triangleCache.tangents[0] = leftTriangleCache.tangents[0];

            triangleCache.vertices[1] = newTriangleCache.vertices[0];
            triangleCache.uvs[1]      = newTriangleCache.uvs[0];
            triangleCache.normals[1]  = newTriangleCache.normals[0];
            triangleCache.tangents[1] = newTriangleCache.tangents[0];

            triangleCache.vertices[2] = newTriangleCache.vertices[1];
            triangleCache.uvs[2]      = newTriangleCache.uvs[1];
            triangleCache.normals[2]  = newTriangleCache.normals[1];
            triangleCache.tangents[2] = newTriangleCache.tangents[1];

            // Check Normal
            CheckNormal(ref triangleCache);

            leftHandSide.AddTriangle(triangleCache, subMeshIndex);

            // Connect Triangle: other side point 1, intersect p1, intersect p2
            triangleCache.vertices[0] = rightTriangleCache.vertices[0];
            triangleCache.uvs[0]      = rightTriangleCache.uvs[0];
            triangleCache.normals[0]  = rightTriangleCache.normals[0];
            triangleCache.tangents[0] = rightTriangleCache.tangents[0];

            triangleCache.vertices[1] = newTriangleCache.vertices[0];
            triangleCache.uvs[1]      = newTriangleCache.uvs[0];
            triangleCache.normals[1]  = newTriangleCache.normals[0];
            triangleCache.tangents[1] = newTriangleCache.tangents[0];

            triangleCache.vertices[2] = newTriangleCache.vertices[1];
            triangleCache.uvs[2]      = newTriangleCache.uvs[1];
            triangleCache.normals[2]  = newTriangleCache.normals[1];
            triangleCache.tangents[2] = newTriangleCache.tangents[1];

            // Check Normal
            CheckNormal(ref triangleCache);

            rightHandSide.AddTriangle(triangleCache, subMeshIndex);

            // Connect Triangle: other side point 1, other side point 2, intersect p2
            triangleCache.vertices[0] = rightTriangleCache.vertices[0];
            triangleCache.uvs[0]      = rightTriangleCache.uvs[0];
            triangleCache.normals[0]  = rightTriangleCache.normals[0];
            triangleCache.tangents[0] = rightTriangleCache.tangents[0];

            triangleCache.vertices[1] = rightTriangleCache.vertices[1];
            triangleCache.uvs[1]      = rightTriangleCache.uvs[1];
            triangleCache.normals[1]  = rightTriangleCache.normals[1];
            triangleCache.tangents[1] = rightTriangleCache.tangents[1];

            triangleCache.vertices[2] = newTriangleCache.vertices[1];
            triangleCache.uvs[2]      = newTriangleCache.uvs[1];
            triangleCache.normals[2]  = newTriangleCache.normals[1];
            triangleCache.tangents[2] = newTriangleCache.tangents[1];

            // Check Normal
            CheckNormal(ref triangleCache);

            rightHandSide.AddTriangle(triangleCache, subMeshIndex);
        }
        else
        {
            // Connect Triangle: Single, intersect p1, intersect p2
            triangleCache.vertices[0] = rightTriangleCache.vertices[0];
            triangleCache.uvs[0]      = rightTriangleCache.uvs[0];
            triangleCache.normals[0]  = rightTriangleCache.normals[0];
            triangleCache.tangents[0] = rightTriangleCache.tangents[0];

            triangleCache.vertices[1] = newTriangleCache.vertices[0];
            triangleCache.uvs[1]      = newTriangleCache.uvs[0];
            triangleCache.normals[1]  = newTriangleCache.normals[0];
            triangleCache.tangents[1] = newTriangleCache.tangents[0];

            triangleCache.vertices[2] = newTriangleCache.vertices[1];
            triangleCache.uvs[2]      = newTriangleCache.uvs[1];
            triangleCache.normals[2]  = newTriangleCache.normals[1];
            triangleCache.tangents[2] = newTriangleCache.tangents[1];

            // Check Normal
            CheckNormal(ref triangleCache);

            rightHandSide.AddTriangle(triangleCache, subMeshIndex);

            // Connect Triangle: other side point 1, intersect p1, intersect p2
            triangleCache.vertices[0] = leftTriangleCache.vertices[0];
            triangleCache.uvs[0]      = leftTriangleCache.uvs[0];
            triangleCache.normals[0]  = leftTriangleCache.normals[0];
            triangleCache.tangents[0] = leftTriangleCache.tangents[0];

            triangleCache.vertices[1] = newTriangleCache.vertices[0];
            triangleCache.uvs[1]      = newTriangleCache.uvs[0];
            triangleCache.normals[1]  = newTriangleCache.normals[0];
            triangleCache.tangents[1] = newTriangleCache.tangents[0];

            triangleCache.vertices[2] = newTriangleCache.vertices[1];
            triangleCache.uvs[2]      = newTriangleCache.uvs[1];
            triangleCache.normals[2]  = newTriangleCache.normals[1];
            triangleCache.tangents[2] = newTriangleCache.tangents[1];

            // Check Normal
            CheckNormal(ref triangleCache);

            leftHandSide.AddTriangle(triangleCache, subMeshIndex);

            // Connect Triangle: other side point 1, other side point 2, intersect p2
            triangleCache.vertices[0] = leftTriangleCache.vertices[0];
            triangleCache.uvs[0]      = leftTriangleCache.uvs[0];
            triangleCache.normals[0]  = leftTriangleCache.normals[0];
            triangleCache.tangents[0] = leftTriangleCache.tangents[0];

            triangleCache.vertices[1] = leftTriangleCache.vertices[1];
            triangleCache.uvs[1]      = leftTriangleCache.uvs[1];
            triangleCache.normals[1]  = leftTriangleCache.normals[1];
            triangleCache.tangents[1] = leftTriangleCache.tangents[1];

            triangleCache.vertices[2] = newTriangleCache.vertices[1];
            triangleCache.uvs[2]      = newTriangleCache.uvs[1];
            triangleCache.normals[2]  = newTriangleCache.normals[1];
            triangleCache.tangents[2] = newTriangleCache.tangents[1];

            // Check Normal
            CheckNormal(ref triangleCache);

            leftHandSide.AddTriangle(triangleCache, subMeshIndex);
        }
    }

    void CheckNormal(ref Yr.MeshMaker.Triangle triangleCache)
    {
        Vector3 crossProduct = Vector3.Cross(triangleCache.vertices[1] - triangleCache.vertices[0], triangleCache.vertices[2] - triangleCache.vertices[0]);
        Vector3 averageNormal = (triangleCache.normals[0] + triangleCache.normals[1] + triangleCache.normals[2]) / 3.0f;
        float dotProduct = Vector3.Dot(averageNormal, crossProduct);
        if (dotProduct < 0)
        {
            // swap index 0 and index 2
            Vector3 temp = triangleCache.vertices[2];
            triangleCache.vertices[2] = triangleCache.vertices[0];
            triangleCache.vertices[0] = temp;

            temp = triangleCache.normals[2];
            triangleCache.normals[2] = triangleCache.normals[0];
            triangleCache.normals[0] = temp;

            Vector2 temp2 = triangleCache.uvs[2];
            triangleCache.uvs[2] = triangleCache.uvs[0];
            triangleCache.uvs[0] = temp2;

            Vector4 temp3 = triangleCache.tangents[2];
            triangleCache.tangents[2] = triangleCache.tangents[0];
            triangleCache.tangents[0] = temp3;
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
    
}
