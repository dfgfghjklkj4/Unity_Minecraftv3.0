using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise 
{
	private static Vector2[] octaveOffsets;
	private static System.Random prng;

	public static float[,] GenerateNoiseMap (float[,] n,int mapWidth, int mapHeight, Vector2Int startPos, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
	{
		CalculateRandomSeed(seed);
		CalculateOctaves(octaves, offset);

		return GetNewNoiseMap(n,mapWidth, mapHeight, startPos, scale, octaves, persistance, lacunarity);
	}

	public static void CalculateRandomSeed(int seed)
	{
		prng = new System.Random(seed);
	}

	public static void CalculateOctaves (int octaves, Vector2 offset)
	{
		
		octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; ++i)
		{
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}
	}

	private static float[,] GetNewNoiseMap(float[,] noiseMap,int mapWidth, int mapHeight, float scale, int octaves, float persistance, float lacunarity)
	{
	//	float[,] noiseMap = new float[mapWidth, mapHeight];

		float minNoiseValue = float.MaxValue;
		float maxNoiseValue = float.MinValue;

		int halfWidth = mapWidth / 2;
		int halfHeight = mapHeight / 2;

		for (int x = 0; x < mapWidth; ++x)
			for (int y = 0; y < mapHeight; ++y)
			{
				float perlinValue = PerlinNoise(x - halfWidth, y - halfHeight, scale, octaves, persistance, lacunarity);
				noiseMap[x, y] = perlinValue;

				if (perlinValue > maxNoiseValue)
					maxNoiseValue = perlinValue;
				if (perlinValue < minNoiseValue)
					minNoiseValue = perlinValue;
			}

		for (int x = 0; x < mapWidth; ++x)
			for (int y = 0; y < mapHeight; ++y)
				noiseMap[x, y] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x, y]);

		return noiseMap;
	}


	private static float[,] GetNewNoiseMap(float[,] noiseMap,int mapWidth, int mapHeight, Vector2Int startPos, float scale, int octaves, float persistance, float lacunarity)
	{
		//float[,] noiseMap = new float[mapWidth, mapHeight];

		float minNoiseValue = float.MaxValue;
		float maxNoiseValue = float.MinValue;

		int halfWidth = mapWidth / 2;
		int halfHeight = mapHeight / 2;

		int y = startPos.y;
		int x = startPos.x;
		int x0 = -1;
		int y0 = -1;
		for (y = startPos.y; y < mapHeight + startPos.y; y++)
		{
			y0++;
			x0 = -1;
			for (x = startPos.x; x < mapWidth + startPos.x; x++)
			{
				x0++;
				float perlinValue = PerlinNoise(x - halfWidth, y - halfHeight, scale, octaves, persistance, lacunarity);
				noiseMap[x0, y0] = perlinValue;

				if (perlinValue > maxNoiseValue)
					maxNoiseValue = perlinValue;
				if (perlinValue <= minNoiseValue)
					minNoiseValue = perlinValue;
			}
		}



		y = startPos.y;
		x = startPos.x;
		x0 = -1;
		y0 = -1;
		// normalization 
		for (y = startPos.y; y < mapHeight + startPos.y; y++)
		{
			y0++; x0 = -1;
			for (x = startPos.x; x < mapWidth + startPos.x; x++)
			{
				x0++;
				noiseMap[x0, y0] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x0, y0]);

			}
		}

	
		return noiseMap;
	}

	private static float PerlinNoise(int x, int y, float scale, int octaves, float persistance, float lacunarity)
	{
		float amplitude = 1;
		float frequency = 1;
		float noiseValue = 0;
		float revScale = 1 / scale;

		for (int i = 0; i < octaves; ++i)
		{
			float xCoord = x * revScale * frequency + octaveOffsets[i].x;
			float yCoord = y * revScale * frequency + octaveOffsets[i].y;

			float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
			noiseValue += perlinValue * amplitude;

			amplitude *= persistance;
			frequency *= lacunarity;
		}

		return noiseValue;
	}
}
