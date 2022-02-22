//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace StylizedWater2
{
    [Serializable]
    public class WaterMesh
    {
        public enum Shape
        {
            Rectangle,
            Disk
        }
        public Shape shape;

        [Range(10, 1000)]
        public float size = 100f;
        public float UVTiling = 1f;

        [Range(1, 255)]
        public int subdivisions = 32;

        [Tooltip("Shifts the vertices in a random direction. Definitely use this when using flat shading")]
        [Range(0f, 1f)]
        public float noise;

        //Ensure the bounds has some height, otherwise it will prematurely be culled when using high waves
        private const float BOUNDS_HEIGHT_PADDING = 4f;

        public Mesh Rebuild()
        {
            switch (shape)
            {
                case Shape.Rectangle: return CreatePlane();
                case Shape.Disk: return CreateCircle();
            }

            return null;
        }

        public static Mesh Create(Shape shape, float size, int subdivisions, float uvTiling = 1f, float noise = 0f)
        {
            WaterMesh waterMesh = new WaterMesh();
            waterMesh.shape = shape;
            waterMesh.size = size;
            waterMesh.subdivisions = subdivisions;
            waterMesh.UVTiling = uvTiling;
            waterMesh.noise = noise;
			
            return waterMesh.Rebuild();
        }

        // Get the index of point number 'x' in circle number 'c'
        private int GetPointIndex(int c, int x)
        {
            if (c < 0) return 0;

            x = x % ((c + 1) * 6); 

            return (3 * c * (c + 1) + x + 1);
        }

        private Mesh CreateCircle()
        {
            Mesh m = new Mesh();
            m.name = "WaterDisk";

            float distance = 1f / subdivisions;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            vertices.Add(Vector3.zero); //center
            var tris = new List<int>();

            float uvFactor = UVTiling / subdivisions;

            // First pass => build vertices
            for (int loop = 0; loop < subdivisions; loop++)
            {
                float angleStep = (Mathf.PI * 2f) / ((loop + 1) * 6);
                for (int point = 0; point < (loop + 1) * 6; ++point)
                {
                    Vector3 vPos = new Vector3(
                    Mathf.Sin(angleStep * point) ,
                    0f,
                    Mathf.Cos(angleStep * point));
                    
                    UnityEngine.Random.InitState(loop + point);
                    vPos.x += UnityEngine.Random.Range(-noise * 0.01f, noise * 0.01f);
                    vPos.z -= UnityEngine.Random.Range(noise * 0.01f, -noise * 0.01f);

                    vertices.Add(vPos * (size * 0.5f) * distance * (loop + 1));
                }
            }

            //planar mapping
            for (int i = 0; i < vertices.Count; i++)
            {
                uvs.Add(new Vector2((vertices[i].x / size) * UVTiling,(vertices[i].z / size) * UVTiling));
            }

            // Second pass => connect vertices into triangles
            for (int circ = 0; circ < subdivisions; ++circ)
            {
                for (int point = 0, other = 0; point < (circ + 1) * 6; ++point)
                {
                    if (point % (circ + 1) != 0)
                    {
                        // Create 2 triangles
                        tris.Add(GetPointIndex(circ - 1, other + 1));
                        tris.Add(GetPointIndex(circ - 1, other));
                        tris.Add(GetPointIndex(circ, point));

                        tris.Add(GetPointIndex(circ, point));
                        tris.Add(GetPointIndex(circ, point + 1));
                        tris.Add(GetPointIndex(circ - 1, other + 1));
                        ++other;
                    }
                    else
                    {
                        // Create 1 inverse triange
                        tris.Add(GetPointIndex(circ, point));
                        tris.Add(GetPointIndex(circ, point + 1));
                        tris.Add(GetPointIndex(circ - 1, other));
                        // Do not move to the next point in the smaller circle
                    }
                }
            }

            // Create the mesh
            
            m.SetVertices(vertices);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.SetUVs(0, uvs);
            m.colors = new Color[vertices.Count];
            m.bounds = new Bounds(Vector3.zero, new Vector3(size, BOUNDS_HEIGHT_PADDING, size));

            return m;
        }

        private Mesh CreatePlane()
        {
            Mesh m = new Mesh();
            m.name = "WaterPlane";

            size = Mathf.Max(1f, size);
            
            int xCount = subdivisions + 1;
            int zCount = subdivisions + 1;
            int numTriangles = subdivisions * subdivisions * 6;
            int numVertices = xCount * zCount;

            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[numTriangles];
            Vector4[] tangents = new Vector4[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

            int index = 0;
            float uvFactorX = UVTiling / subdivisions;
            float uvFactorY = UVTiling / subdivisions;
            float scaleX = size / subdivisions;
            float scaleY = size / subdivisions;

            for (int z = 0; z < zCount; z++)
            {
                for (int x = 0; x < xCount; x++)
                {
                    vertices[index] = new Vector3(x * scaleX - (size * 0.5f), 0f, z * scaleY - (size * 0.5f));
                    
                    UnityEngine.Random.InitState(z + x);
                    vertices[index].x += UnityEngine.Random.Range(-noise, noise);
                    vertices[index].z -= UnityEngine.Random.Range(noise, -noise);
                    
                    tangents[index] = tangent;
                    uvs[index] = new Vector2(vertices[index].x * uvFactorX, vertices[index].z * uvFactorY);
                    normals[index] = Vector3.up;
                    
                    index++;
                }
            }

            index = 0;
            for (int z = 0; z < subdivisions; z++)
            {
                for (int x = 0; x < subdivisions; x++)
                {
                    triangles[index] = (z * xCount) + x;
                    triangles[index + 1] = ((z + 1) * xCount) + x;
                    triangles[index + 2] = (z * xCount) + x + 1;

                    triangles[index + 3] = ((z + 1) * xCount) + x;
                    triangles[index + 4] = ((z + 1) * xCount) + x + 1;
                    triangles[index + 5] = (z * xCount) + x + 1;
                    index += 6;
                }
            }

            m.vertices = vertices;
            m.triangles = triangles;
            m.uv = uvs;
            m.tangents = tangents;
            m.normals = normals;
            m.colors = new Color[vertices.Length];
            m.bounds = new Bounds(Vector3.zero, new Vector3(size, BOUNDS_HEIGHT_PADDING, size));

            return m;
        }
    }
}