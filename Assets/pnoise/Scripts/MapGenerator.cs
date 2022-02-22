using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PerlinNoise;

public class MapGenerator : MonoBehaviour
{
	public static MapGenerator Instanc;
	#region Variables

	public enum DrawMode
	{	
		NoiseMap, 
		ColorMap,
		Mesh
	};

	public MapDisplay mapDisplay;
	public GameObject plane;
	public GameObject mesh;

	public int mapChunkSize = 241;
	[Range(0, 6)]
	public int levelOfDetail;
	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool autoUpdate;

	public DrawMode drawMode;

	public TerrainType[] terrainTypes;

	public float meshHeightMultiplier;
	public AnimationCurve heightCurve;
	public AnimationCurve heightCurve2;
	public Vector2Int startPos;
	public NormalizeMode normalizeMode;
	private void Awake()
    {
        if (Instanc==null)
        {
			Instanc = this;

		}

PerlinNoise.pseudoRandomNumber = new System.Random(seed);
      PerlinNoise. octaveOffsets =PerlinNoise. GenerateOffsetsOnSeeds(octaves, seed, offset);

		//Noise.CalculateRandomSeed(seed);
		//Noise.CalculateOctaves(octaves, offset);
	
	}

	#endregion

	



	public float Frequency;

	public float FrequencyMultiplier;

	public float Amplitude;

	public float AmplitudeMultiplier;

	public  float[,]  GenerateMapData(float[,] noiseMap, Vector2 mapCenter, Vector2Int startpos, int size)

	{
	//	float[,] noiseMap = new float[size, size];
		noiseMap = PerlinNoise.GenerateNoiseMap2(noiseMap, size, size, startpos, noiseScale, seed, octaves, persistance, lacunarity, offset, normalizeMode);
		return noiseMap;
	}	
	public  float GenerateMapData2( Vector2 mapCenter, int x,int y, int size)

	{
	//	float[,] noiseMap = new float[size, size];
		return PerlinNoise.GenerateNoiseMapSingel( size, size,x, y, noiseScale, seed, octaves, persistance, lacunarity, offset, normalizeMode);
		
	}	
		public void GenerateMap()
	{
		float[,] noiseMap = new float[mapChunkSize, mapChunkSize];
		 noiseMap = Noise.GenerateNoiseMap(noiseMap,mapChunkSize, mapChunkSize, startPos, seed, noiseScale, octaves, persistance, lacunarity, offset);
//	noiseMap = PerlinNoise.GenerateNoiseMap2(noiseMap,mapChunkSize, mapChunkSize, startPos, noiseScale, seed , octaves, persistance, lacunarity, offset, normalizeMode);
	//	noiseMap = PerlinNoise.GenerateHeights(noiseMap, mapChunkSize, mapChunkSize, startPos, noiseScale, seed, octaves, Frequency, FrequencyMultiplier, Amplitude, AmplitudeMultiplier);

		if (drawMode == DrawMode.NoiseMap)
		{
			mesh.SetActive(false);
			plane.SetActive(true);

			Texture2D texture = TextureGenerator.GenerateTextureFromNoiseMap(noiseMap);
			mapDisplay.DrawTexture(texture);
		}
		else if (drawMode == DrawMode.ColorMap)
		{
			mesh.SetActive(false);
			plane.SetActive(true);

			Texture2D texture = TextureGenerator.GenerateTextureFromColorMap(GenerateColorMap(noiseMap), mapChunkSize, mapChunkSize);

			mapDisplay.DrawTexture(texture);
		}
		else if (drawMode == DrawMode.Mesh)
		{
			mesh.SetActive(true);
			plane.SetActive(false);

			Texture2D texture = TextureGenerator.GenerateTextureFromColorMap(GenerateColorMap(noiseMap), mapChunkSize, mapChunkSize);
			MeshDataCustom meshData = MeshGenerator.GenerateTerrianMesh(noiseMap, meshHeightMultiplier, heightCurve2, levelOfDetail);

			mapDisplay.DrawMesh(meshData, texture);
		}
	}

	private Color[] GenerateColorMap (float[,] noiseMap)
	{
		int width = noiseMap.GetLength(0);
		int height = noiseMap.GetLength(1);

		Color[] colorMap = new Color[width * height];

		for (int x = 0; x < width; ++x)
			for (int y = 0; y < height; ++y)
			{
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < terrainTypes.Length; ++i)
					if (currentHeight <= terrainTypes[i].height)
					{
						colorMap[y * width + x] = terrainTypes[i].color;
						break;
					}
			}

		return colorMap;
	}

	private void OnValidate()
	{
		if (lacunarity < 1)
			lacunarity = 1;
		if (octaves < 1)
			octaves = 1;
	}

	[System.Serializable]
	public struct TerrainType
	{
		public string name;
		public float height;
		public Color color;
	};
}
