using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yr
{
    public class MeshCut
    {
        private Plane plane = new Plane();
        
        MeshMaker leftHandSide = new MeshMaker();
        MeshMaker rightHandSide = new MeshMaker();

        // List to store new vertices generate from the slice
        List<Vector3> newVerticesCache = new List<Vector3>();

        // tmp variable to store triangle
        MeshMaker.Triangle triangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);

        // Variable for CutTriangle
        MeshMaker.Triangle leftTriangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
        MeshMaker.Triangle rightTriangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);
        MeshMaker.Triangle newTriangleCache = new MeshMaker.Triangle(new Vector3[3], new Vector2[3], new Vector3[3], new Vector4[3]);

        // variable for CapCut
        private List<int> capUsedIndices = new List<int>();
        private List<int> capPolygonIndices = new List<int>();

        public MeshCut()
        {
           
        }

        public GameObject[] Slice(GameObject victim, Vector3 planeNormal, Vector3 planeAnchor, Material capMaterial)
        {
            Vector3 inversedNormal = victim.transform.InverseTransformDirection(-planeNormal);
            Vector3 inversedAnchor = victim.transform.InverseTransformPoint(planeAnchor);
            plane.UpdateParam(inversedNormal, inversedAnchor);

            List<GameObject> sliced = new List<GameObject>();

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

                    bool isP1LeftHandSide = plane.isLeftSideFromPlane(p1);
                    bool isP2LeftHandSide = plane.isLeftSideFromPlane(p2);
                    bool isP3LeftHandSide = plane.isLeftSideFromPlane(p3);

                    if (isP1LeftHandSide == isP2LeftHandSide && isP1LeftHandSide == isP3LeftHandSide)
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

                Material[] mats = victim.GetComponent<MeshRenderer>().sharedMaterials;

                if (mats[mats.Length - 1] != capMaterial)
                {
                    // append capMaterial
                    Material[] newMats = new Material[mats.Length + 1];
                    mats.CopyTo(newMats, 0);
                    newMats[newMats.Length - 1] = capMaterial;
                    mats = newMats;
                }

                // subMeshIndex for cap
                CapCut(mats.Length - 1);

                Mesh leftHandSideMesh = leftHandSide.GetMesh("Left HandSide Mesh");
                Mesh rightHandSideMesh = rightHandSide.GetMesh("Right HandSide Mesh");

                GameObject leftHandSideObject = new GameObject("Left Hand Side", typeof(MeshFilter), typeof(MeshRenderer));
                leftHandSideObject.transform.position = victim.transform.position;
                leftHandSideObject.transform.rotation = victim.transform.rotation;
                leftHandSideObject.transform.localScale = victim.transform.localScale;
                if (victim.transform.parent != null)
                {
                    leftHandSideObject.transform.parent = victim.transform.parent;
                }
                leftHandSideObject.GetComponent<MeshFilter>().mesh = leftHandSideMesh;
                leftHandSideObject.GetComponent<MeshRenderer>().materials = mats;

                GameObject rightHandSideObject = new GameObject("Right Hand Side", typeof(MeshFilter), typeof(MeshRenderer));
                rightHandSideObject.transform.position = victim.transform.position;
                rightHandSideObject.transform.rotation = victim.transform.rotation;
                rightHandSideObject.transform.localScale = victim.transform.localScale;
                if (victim.transform.parent != null)
                {
                    rightHandSideObject.transform.parent = victim.transform.parent;
                }
                rightHandSideObject.GetComponent<MeshFilter>().mesh = rightHandSideMesh;
                rightHandSideObject.GetComponent<MeshRenderer>().materials = mats;

                sliced.Add(leftHandSideObject);
                sliced.Add(rightHandSideObject);
            }

            leftHandSide.Clear();
            leftHandSide.Clear();
            newVerticesCache.Clear();

            return sliced.ToArray();
        }

        void CapCut(int subMeshIndex)
        {
            capUsedIndices.Clear();
            capPolygonIndices.Clear();

            for (int i = 0; i < newVerticesCache.Count; i += 2)
            {
                if (!capUsedIndices.Contains(i))
                {
                    capPolygonIndices.Clear();
                    capPolygonIndices.Add(i);
                    capPolygonIndices.Add(i + 1);

                    capUsedIndices.Add(i);
                    capUsedIndices.Add(i + 1);

                    Vector3 connectionPointLeft = newVerticesCache[i];
                    Vector3 connectionPointRight = newVerticesCache[i + 1];
                    bool isDone = false;

                    // find next point that chains with current connect point
                    while (isDone == false)
                    {
                        isDone = true;

                        // loop throught newVerticesCache to find next chain
                        // if there is no more chain, next loop won't set isDone to false, which escapes the loop
                        for (int index = 0; index < newVerticesCache.Count; index += 2)
                        {
                            // not used point
                            if (!capUsedIndices.Contains(index))
                            {
                                Vector3 newConnectionPointLeft = newVerticesCache[index];
                                Vector3 newConnectionPointRight = newVerticesCache[index + 1];

                                bool isChained = (
                                    connectionPointLeft == newConnectionPointLeft ||
                                    connectionPointLeft == newConnectionPointRight ||
                                    connectionPointRight == newConnectionPointLeft ||
                                    connectionPointRight == newConnectionPointRight
                                );

                                if (isChained)
                                {
                                    capUsedIndices.Add(index);
                                    capUsedIndices.Add(index + 1);

                                    if (connectionPointLeft == newConnectionPointLeft)
                                    {
                                        capPolygonIndices.Insert(0, index + 1);
                                        connectionPointLeft = newVerticesCache[index + 1];
                                    }
                                    else if (connectionPointLeft == newConnectionPointRight)
                                    {
                                        capPolygonIndices.Insert(0, index);
                                        connectionPointLeft = newVerticesCache[index];
                                    }
                                    else if (connectionPointRight == newConnectionPointLeft)
                                    {
                                        capPolygonIndices.Add(index + 1);
                                        connectionPointRight = newVerticesCache[index + 1];
                                    }
                                    else if (connectionPointRight == newConnectionPointRight)
                                    {
                                        capPolygonIndices.Add(index);
                                        connectionPointRight = newVerticesCache[index];
                                    }

                                    isDone = false;
                                }
                            }
                        }
                    }

                    // check the loop is closed or not
                    Vector3 startPoint = newVerticesCache[capPolygonIndices[0]];
                    Vector3 endPoint = newVerticesCache[capPolygonIndices[capPolygonIndices.Count - 1]];
                    if (startPoint == endPoint)
                    {
                        // if start point & end point are same, rename index inorder to make "index" loop
                        capPolygonIndices[capPolygonIndices.Count - 1] = capPolygonIndices[0];
                    }
                    else
                    {
                        // connect to start point
                        capPolygonIndices.Add(capPolygonIndices[0]);
                    }

                    FillCap(capPolygonIndices, subMeshIndex);
                }
            }
        }

        void FillCap(List<int> indices, int subMeshIndex)
        {
            // compute center
            Vector3 center = Vector3.zero;
            foreach (var i in indices)
            {
                center += newVerticesCache[i];
            }
            center /= indices.Count;

            // rotate plane normal by 90 degrees
            Vector3 planeN = plane.GetNormal();
            Vector3 up = new Vector3(planeN.y, -planeN.x, planeN.z);
            Vector3 left = Vector3.Cross(planeN, up);

            Vector3 displacement = Vector3.zero;
            Vector2 uv1 = Vector2.zero;
            Vector2 uv2 = Vector2.zero;

            for (int i = 0; i < indices.Count - 1; ++i)
            {
                // Connect every two points to center
                // TODO: Refactor to connect triangle each by each, that is comsumes two edge and product one edge

                // use displacement to calculate uv
                displacement = newVerticesCache[indices[i]] - center;
                uv1.x = Vector3.Dot(displacement, left) + 0.5f;
                uv1.y = Vector3.Dot(displacement, up) + 0.5f;

                displacement = newVerticesCache[indices[i + 1]] - center;
                uv2.x = Vector3.Dot(displacement, left) + 0.5f;
                uv2.y = Vector3.Dot(displacement, up) + 0.5f;

                triangleCache.vertices[0] = newVerticesCache[indices[i]];
                triangleCache.uvs[0] = uv1;
                triangleCache.normals[0] = -planeN;
                triangleCache.tangents[0] = Vector4.zero;

                triangleCache.vertices[1] = newVerticesCache[indices[i + 1]];
                triangleCache.uvs[1] = uv2;
                triangleCache.normals[1] = -planeN;
                triangleCache.tangents[1] = Vector4.zero;

                triangleCache.vertices[2] = center;
                triangleCache.uvs[2] = new Vector2(0.5f, 0.5f); // since (0.5, 0.5) is the center of uv coordinates
                triangleCache.normals[2] = -planeN;
                triangleCache.tangents[2] = Vector4.zero;

                CheckNormal(ref triangleCache);
                leftHandSide.AddTriangle(triangleCache, subMeshIndex);

                // flip the normal for another cap
                triangleCache.normals[0] = planeN;
                triangleCache.normals[1] = planeN;
                triangleCache.normals[2] = planeN;

                CheckNormal(ref triangleCache);
                rightHandSide.AddTriangle(triangleCache, subMeshIndex);
            }
        }

        void CutTriangle(ref Yr.MeshMaker.Triangle triangleCache, int subMeshIndex)
        {
            // first, we split three points into two sides

            int leftCount = 0, rightCount = 0;

            for (int i = 0; i < 3; ++i)
            {
                bool isLeftHandSide = plane.isLeftSideFromPlane(triangleCache.vertices[i]);

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
                triangleCache.uvs[0] = leftTriangleCache.uvs[0];
                triangleCache.normals[0] = leftTriangleCache.normals[0];
                triangleCache.tangents[0] = leftTriangleCache.tangents[0];

                triangleCache.vertices[1] = rightTriangleCache.vertices[0];
                triangleCache.uvs[1] = rightTriangleCache.uvs[0];
                triangleCache.normals[1] = rightTriangleCache.normals[0];
                triangleCache.tangents[1] = rightTriangleCache.tangents[0];

                triangleCache.vertices[2] = rightTriangleCache.vertices[1];
                triangleCache.uvs[2] = rightTriangleCache.uvs[1];
                triangleCache.normals[2] = rightTriangleCache.normals[1];
                triangleCache.tangents[2] = rightTriangleCache.tangents[1];
            }
            else
            {
                triangleCache.vertices[0] = rightTriangleCache.vertices[0];
                triangleCache.uvs[0] = rightTriangleCache.uvs[0];
                triangleCache.normals[0] = rightTriangleCache.normals[0];
                triangleCache.tangents[0] = rightTriangleCache.tangents[0];

                triangleCache.vertices[1] = leftTriangleCache.vertices[0];
                triangleCache.uvs[1] = leftTriangleCache.uvs[0];
                triangleCache.normals[1] = leftTriangleCache.normals[0];
                triangleCache.tangents[1] = leftTriangleCache.tangents[0];

                triangleCache.vertices[2] = leftTriangleCache.vertices[1];
                triangleCache.uvs[2] = leftTriangleCache.uvs[1];
                triangleCache.normals[2] = leftTriangleCache.normals[1];
                triangleCache.tangents[2] = leftTriangleCache.tangents[1];
            }

            // Get intersect point

            float d1 = 0.0f, d2 = 0.0f;
            float normalizedDistance = 0.0f;

            // Deal intersect point 1
            d1 = plane.DistFromPlane(triangleCache.vertices[0]);
            d2 = plane.DistFromPlane(triangleCache.vertices[1]);
            normalizedDistance = d1 / (d1 - d2);

            newTriangleCache.vertices[0] = Vector3.Lerp(triangleCache.vertices[0], triangleCache.vertices[1], normalizedDistance);
            newTriangleCache.uvs[0] = Vector2.Lerp(triangleCache.uvs[0], triangleCache.uvs[1], normalizedDistance);
            newTriangleCache.normals[0] = Vector3.Lerp(triangleCache.normals[0], triangleCache.normals[1], normalizedDistance);
            newTriangleCache.tangents[0] = Vector4.Lerp(triangleCache.tangents[0], triangleCache.tangents[1], normalizedDistance);

            // Deal intersect point 2
            d1 = plane.DistFromPlane(triangleCache.vertices[0]);
            d2 = plane.DistFromPlane(triangleCache.vertices[2]);
            normalizedDistance = d1 / (d1 - d2);

            newTriangleCache.vertices[1] = Vector3.Lerp(triangleCache.vertices[0], triangleCache.vertices[2], normalizedDistance);
            newTriangleCache.uvs[1] = Vector2.Lerp(triangleCache.uvs[0], triangleCache.uvs[2], normalizedDistance);
            newTriangleCache.normals[1] = Vector3.Lerp(triangleCache.normals[0], triangleCache.normals[2], normalizedDistance);
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
                triangleCache.uvs[0] = leftTriangleCache.uvs[0];
                triangleCache.normals[0] = leftTriangleCache.normals[0];
                triangleCache.tangents[0] = leftTriangleCache.tangents[0];

                triangleCache.vertices[1] = newTriangleCache.vertices[0];
                triangleCache.uvs[1] = newTriangleCache.uvs[0];
                triangleCache.normals[1] = newTriangleCache.normals[0];
                triangleCache.tangents[1] = newTriangleCache.tangents[0];

                triangleCache.vertices[2] = newTriangleCache.vertices[1];
                triangleCache.uvs[2] = newTriangleCache.uvs[1];
                triangleCache.normals[2] = newTriangleCache.normals[1];
                triangleCache.tangents[2] = newTriangleCache.tangents[1];

                // Check Normal
                CheckNormal(ref triangleCache);

                leftHandSide.AddTriangle(triangleCache, subMeshIndex);

                // Connect Triangle: other side point 1, intersect p1, intersect p2
                triangleCache.vertices[0] = rightTriangleCache.vertices[0];
                triangleCache.uvs[0] = rightTriangleCache.uvs[0];
                triangleCache.normals[0] = rightTriangleCache.normals[0];
                triangleCache.tangents[0] = rightTriangleCache.tangents[0];

                triangleCache.vertices[1] = newTriangleCache.vertices[0];
                triangleCache.uvs[1] = newTriangleCache.uvs[0];
                triangleCache.normals[1] = newTriangleCache.normals[0];
                triangleCache.tangents[1] = newTriangleCache.tangents[0];

                triangleCache.vertices[2] = newTriangleCache.vertices[1];
                triangleCache.uvs[2] = newTriangleCache.uvs[1];
                triangleCache.normals[2] = newTriangleCache.normals[1];
                triangleCache.tangents[2] = newTriangleCache.tangents[1];

                // Check Normal
                CheckNormal(ref triangleCache);

                rightHandSide.AddTriangle(triangleCache, subMeshIndex);

                // Connect Triangle: other side point 1, other side point 2, intersect p2
                triangleCache.vertices[0] = rightTriangleCache.vertices[0];
                triangleCache.uvs[0] = rightTriangleCache.uvs[0];
                triangleCache.normals[0] = rightTriangleCache.normals[0];
                triangleCache.tangents[0] = rightTriangleCache.tangents[0];

                triangleCache.vertices[1] = rightTriangleCache.vertices[1];
                triangleCache.uvs[1] = rightTriangleCache.uvs[1];
                triangleCache.normals[1] = rightTriangleCache.normals[1];
                triangleCache.tangents[1] = rightTriangleCache.tangents[1];

                triangleCache.vertices[2] = newTriangleCache.vertices[1];
                triangleCache.uvs[2] = newTriangleCache.uvs[1];
                triangleCache.normals[2] = newTriangleCache.normals[1];
                triangleCache.tangents[2] = newTriangleCache.tangents[1];

                // Check Normal
                CheckNormal(ref triangleCache);

                rightHandSide.AddTriangle(triangleCache, subMeshIndex);
            }
            else
            {
                // Connect Triangle: Single, intersect p1, intersect p2
                triangleCache.vertices[0] = rightTriangleCache.vertices[0];
                triangleCache.uvs[0] = rightTriangleCache.uvs[0];
                triangleCache.normals[0] = rightTriangleCache.normals[0];
                triangleCache.tangents[0] = rightTriangleCache.tangents[0];

                triangleCache.vertices[1] = newTriangleCache.vertices[0];
                triangleCache.uvs[1] = newTriangleCache.uvs[0];
                triangleCache.normals[1] = newTriangleCache.normals[0];
                triangleCache.tangents[1] = newTriangleCache.tangents[0];

                triangleCache.vertices[2] = newTriangleCache.vertices[1];
                triangleCache.uvs[2] = newTriangleCache.uvs[1];
                triangleCache.normals[2] = newTriangleCache.normals[1];
                triangleCache.tangents[2] = newTriangleCache.tangents[1];

                // Check Normal
                CheckNormal(ref triangleCache);

                rightHandSide.AddTriangle(triangleCache, subMeshIndex);

                // Connect Triangle: other side point 1, intersect p1, intersect p2
                triangleCache.vertices[0] = leftTriangleCache.vertices[0];
                triangleCache.uvs[0] = leftTriangleCache.uvs[0];
                triangleCache.normals[0] = leftTriangleCache.normals[0];
                triangleCache.tangents[0] = leftTriangleCache.tangents[0];

                triangleCache.vertices[1] = newTriangleCache.vertices[0];
                triangleCache.uvs[1] = newTriangleCache.uvs[0];
                triangleCache.normals[1] = newTriangleCache.normals[0];
                triangleCache.tangents[1] = newTriangleCache.tangents[0];

                triangleCache.vertices[2] = newTriangleCache.vertices[1];
                triangleCache.uvs[2] = newTriangleCache.uvs[1];
                triangleCache.normals[2] = newTriangleCache.normals[1];
                triangleCache.tangents[2] = newTriangleCache.tangents[1];

                // Check Normal
                CheckNormal(ref triangleCache);

                leftHandSide.AddTriangle(triangleCache, subMeshIndex);

                // Connect Triangle: other side point 1, other side point 2, intersect p2
                triangleCache.vertices[0] = leftTriangleCache.vertices[0];
                triangleCache.uvs[0] = leftTriangleCache.uvs[0];
                triangleCache.normals[0] = leftTriangleCache.normals[0];
                triangleCache.tangents[0] = leftTriangleCache.tangents[0];

                triangleCache.vertices[1] = leftTriangleCache.vertices[1];
                triangleCache.uvs[1] = leftTriangleCache.uvs[1];
                triangleCache.normals[1] = leftTriangleCache.normals[1];
                triangleCache.tangents[1] = leftTriangleCache.tangents[1];

                triangleCache.vertices[2] = newTriangleCache.vertices[1];
                triangleCache.uvs[2] = newTriangleCache.uvs[1];
                triangleCache.normals[2] = newTriangleCache.normals[1];
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
    }
}
