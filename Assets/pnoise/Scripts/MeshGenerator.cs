using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator 
{


	public static MeshDataCustom GenerateTerrianMesh(float[,] heightMap, float meshHeightAmplitude, AnimationCurve meshHeightCurve, int levelOfDetail)
	{
		int chunkSize = heightMap.GetLength(0);

		// 构建坐标， 原点在map中心
		// topLeftX, topLeftZ 分别代表第二象限 最左上顶点
		float topLeftX = (chunkSize - 1) / -2f;
		float topLeftZ = (chunkSize - 1) / 2f;

		// LOD 渲染中 根据LOD级别跳过的点
		// 当前级别LOD mesh图的点的数量 
		int meshLODIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
		int meshLODWidth = (chunkSize - 1) / meshLODIncrement + 1;

		AnimationCurve animationCurve = new AnimationCurve(meshHeightCurve.keys);

		MeshDataCustom meshData = new MeshDataCustom(meshLODWidth, meshLODWidth);
		int vertexIndex = 0;

		for (int y = 0; y < chunkSize; y += meshLODIncrement)
		{
			for (int x = 0; x < chunkSize; x += meshLODIncrement)
			{
				// vec3 XoZ面的坐标为原noiseMap的x和y， Y轴为njhmoiseMap的值
				meshData.vertices[vertexIndex] = new Vector3(
					topLeftX + x, animationCurve.Evaluate(heightMap[y, x]) * meshHeightAmplitude, topLeftZ - y);
				meshData.uvs[vertexIndex] = new Vector2((y / (float)chunkSize), (x / (float)chunkSize));

				if (x < chunkSize - 1 && y < chunkSize - 1)
				{
					meshData.AddTriangleVertices(vertexIndex, vertexIndex + meshLODWidth + 1, vertexIndex + meshLODWidth);
					meshData.AddTriangleVertices(vertexIndex + meshLODWidth + 1, vertexIndex, vertexIndex + 1);
				}
				vertexIndex++;
			}
		}
		return meshData;
	}


}

public class MeshDataCustom
{
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;

	private int triangleIndex = 0;

	public MeshDataCustom(int meshWidth, int meshHeight)
	{
		vertices = new Vector3[meshWidth * meshHeight];
		uvs = new Vector2[meshWidth * meshHeight];
		triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
	}

	public void AddTriangleVertices(int vertexIndexA, int vertexIndexB, int vertexIndexC)
	{
		triangles[triangleIndex] = vertexIndexA;
		triangles[triangleIndex + 1] = vertexIndexB;
		triangles[triangleIndex + 2] = vertexIndexC;

		triangleIndex += 3;
	}

	private Vector3[] GetNormals()
	{
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int trianglesCount = triangles.Length / 3;
		for (int i = 0; i < trianglesCount; i++)
		{
			int triangleNormalIndex = i * 3;

			int vertexIndexA = triangles[triangleNormalIndex];
			int vertexIndexB = triangles[triangleNormalIndex + 1];
			int vertexIndexC = triangles[triangleNormalIndex + 2];

			Vector3 triangleNormal = CalculateNormal(vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		for (int i = 0; i < vertexNormals.Length; i++)
		{
			vertexNormals[i].Normalize();
		}

		return vertexNormals;
	}

	private Vector3 CalculateNormal(int vertexIndexA, int vertexIndexB, int vertexIndexC)
	{
		Vector3 vertexA = vertices[vertexIndexA];
		Vector3 vertexB = vertices[vertexIndexB];
		Vector3 vertexC = vertices[vertexIndexC];

		Vector3 vecAB = vertexB - vertexA;
		Vector3 vecAC = vertexC - vertexA;

		return Vector3.Cross(vecAB, vecAC).normalized;
	}

	public Mesh GetMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.normals = GetNormals();
		return mesh;
	}
}