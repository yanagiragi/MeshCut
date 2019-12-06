using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yr
{
    public class MeshMaker
    {
        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector3> _normals = new List<Vector3>();
        private List<Vector2> _uvs = new List<Vector2>();
        private List<Vector4> _tangents = new List<Vector4>();
        private List<List<int>> _subIndices = new List<List<int>>();

        public int verticeCount
        {
            get
            {
                return _vertices.Count;
            }
        }

        void Clear()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _tangents.Clear();
            _subIndices.Clear();
        }

        public void AddTriangle(Yr.MeshMaker.Triangle triangle, int submesh = 0)
        {
            AddTriangle(triangle.vertices, triangle.uvs, triangle.normals, triangle.tangents, submesh);
        }

        public void AddTriangle(Vector3[] vertices, Vector2[] uvs, Vector3[] normals, Vector4[] tangents, int submesh = 0)
        {
            int vertCount = _vertices.Count;

            _vertices.Add(vertices[0]);
            _vertices.Add(vertices[1]);
            _vertices.Add(vertices[2]);

            _normals.Add(normals[0]);
            _normals.Add(normals[1]);
            _normals.Add(normals[2]);

            _uvs.Add(uvs[0]);
            _uvs.Add(uvs[1]);
            _uvs.Add(uvs[2]);

            if (tangents != null)
            {
                _tangents.Add(tangents[0]);
                _tangents.Add(tangents[1]);
                _tangents.Add(tangents[2]);
            }

            if (_subIndices.Count < submesh + 1)
            {
                for (int i = _subIndices.Count; i < submesh + 1; i++)
                {
                    _subIndices.Add(new List<int>());
                }
            }

            _subIndices[submesh].Add(vertCount);
            _subIndices[submesh].Add(vertCount + 1);
            _subIndices[submesh].Add(vertCount + 2);
        }

        public Mesh GetMesh(string meshName = "Generated Mesh")
        {           
            Mesh shape = new Mesh();
            shape.name = meshName;
            shape.SetVertices(_vertices);
            shape.SetNormals(_normals);
            shape.SetUVs(0, _uvs);
            shape.SetUVs(1, _uvs);

            if (_tangents.Count > 1)
                shape.SetTangents(_tangents);

            shape.subMeshCount = _subIndices.Count;

            for (int i = 0; i < _subIndices.Count; i++)
                shape.SetTriangles(_subIndices[i], i);

            return shape;            
        }

        public struct Triangle
        {
            public Vector3[] vertices;
            public Vector2[] uvs;
            public Vector3[] normals;
            public Vector4[] tangents;

            public Triangle(Vector3[] vertices = null, Vector2[] uvs = null, Vector3[] normals = null, Vector4[] tangents = null)
            {
                this.vertices = vertices;
                this.uvs = uvs;
                this.normals = normals;
                this.tangents = tangents;
            }
        }
    }

}

