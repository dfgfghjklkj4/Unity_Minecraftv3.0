using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Random = System.Random;
/*
 * PerlinNoise生成工具 
 */

public static class PerlinNoise
{

    [DllImport("NoiseLab", EntryPoint = "Noise0")]
    private static extern float Noise0(float a, float b);

    public enum NormalizeMode { Local, Global};

    /*
     * Input: 地图宽，高，scale
     * perlinNoise 生成的是伪随机，通过scale生成不同的结果
     */
    public static float[,] GenerateNoiseMap0(int mapWidth,
                                            int mapHeight,
                                            float scale,
                                            int seed,
                                            int octaves,
                                            float persistance,
                                            float lacunarity,
                                            Vector2 offset,
                                            NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;

        float[,] perlinNoiseMap = new float[mapWidth, mapHeight];

        System.Random pseudoRandomNumber = new System.Random(seed);
        Vector2[] octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);

        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = ((x - mapWidth / 2f) + octaveOffsets[i].x) / scale * frequency;
                    float yCoord = ((y - mapHeight / 2f) + octaveOffsets[i].y) / scale * frequency;
                   float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                   // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 

                   
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
                minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
                perlinNoiseMap[x, y] = noiseHeight;
            }  

        }

        // normalization 
        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                if(normalizeMode == NormalizeMode.Local)
                    perlinNoiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, perlinNoiseMap[x, y]);
                else
                {
                    float nomalizedHeight = (perlinNoiseMap[x, y] + 1) / (2f * maxGlobalHeight / 2f);
                    perlinNoiseMap[x, y] = Mathf.Clamp01(nomalizedHeight);
                }

            }
        }
        return perlinNoiseMap;
    }




    public static float[,] GenerateNoiseMap(int mapWidth,
                                           int mapHeight,
                                           float scale,
                                           int seed,
                                           int octaves,
                                           float persistance,
                                           float lacunarity,
                                           Vector2 offset,
                                           NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;

        float[,] perlinNoiseMap = new float[mapWidth, mapHeight];

        System.Random pseudoRandomNumber = new System.Random(seed);
        Vector2[] octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = ((x - mapWidth / 2f) + octaveOffsets[i].x) / scale * frequency;
                    float yCoord = ((y - mapHeight / 2f) + octaveOffsets[i].y) / scale * frequency;

                 //   xCoord = (x / scale * frequency) + octaveOffsets[i].x;
                //    yCoord = (y / scale * frequency) + octaveOffsets[i].y;

                 float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2-1;
                   // float perlinValue = Noise0(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                    // noiseHeight += perlinValue ;
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

               maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
               minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
                perlinNoiseMap[x, y] = noiseHeight;
            }

        }
       // return perlinNoiseMap;
        // normalization 
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                    perlinNoiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, perlinNoiseMap[x, y]);
                else
                {
                    float nomalizedHeight = (perlinNoiseMap[x, y] + 1) / (2f * maxGlobalHeight / 2f);
                    perlinNoiseMap[x, y] = Mathf.Clamp01(nomalizedHeight);
                }

            }
        }
        return perlinNoiseMap;
    }



    [DllImport("NoiseLab", EntryPoint = "octaves2D2")]
    private static extern float octaves2D2(float a, float b,int o,float f,float p);

    public static float[,] GenerateNoiseMap22(int mapWidth,
                                         int mapHeight,
                                         float scale,
                                         int seed,
                                         int octaves,
                                         float persistance,
                                         float lacunarity,
                                         Vector2 offset,
                                         NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;

        float[,] perlinNoiseMap = new float[mapWidth, mapHeight];

        System.Random pseudoRandomNumber = new System.Random(seed);
        Vector2[] octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
              //  float xCoord = ((x - mapWidth / 2f) ) / scale;
              //  float yCoord = ((y - mapHeight / 2f)) / scale;

                perlinNoiseMap[x, y]= octaves2D2(x, y, octaves, lacunarity, persistance);


                for (int i = 0; i < octaves; i++)
                {
                   // float xCoord = ((x - mapWidth / 2f) + octaveOffsets[i].x) / scale * frequency;
                  //  float yCoord = ((y - mapHeight / 2f) + octaveOffsets[i].y) / scale * frequency;

                    //   xCoord = (x / scale * frequency) + octaveOffsets[i].x;
                    //    yCoord = (y / scale * frequency) + octaveOffsets[i].y;

                  //  float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Noise0(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                 //   noiseHeight += perlinValue * amplitude;
                 //   amplitude *= persistance;
                 //   frequency *= lacunarity;
                }

               // maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
             //   minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
              //  perlinNoiseMap[x, y] = noiseHeight;
            }

        }
      
        return perlinNoiseMap;
    }

   public static Vector2[] octaveOffsets;
    static float  maxGlobalHeight;

    public static Vector2[] f1(int octaves,int seed, Vector2 offset, float persistance)
    {
       
        float amplitude_ = 1;
        
        System.Random pseudoRandomNumber = new System.Random(seed);
        octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

      //  float maxLocalNoiseHeight = float.MinValue;
      //  float minLocalNoiseHeight = float.MaxValue;
         maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude_, persistance);

        return octaveOffsets;
    }
    

    

    public static float[,] GenerateNoiseMap200(Vector2Int startPos, int mapWidth,
                                           int mapHeight,
                                           float scale,
                                           int seed,
                                           int octaves,
                                           float persistance,
                                           float lacunarity,
                                           Vector2 offset,
                                           NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;

        float[,] perlinNoiseMap = new float[mapWidth, mapHeight];

        System.Random pseudoRandomNumber = new System.Random(seed);
       octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);

        //  float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);
        int y = startPos.y;
        int x = startPos.x;
        int x0 = -1;
        int y0 = -1;
        for ( y = startPos.y; y < mapHeight+ startPos.y; y++)
        {
            y0++;
            x0=-1;
            for ( x = startPos.x; x < mapWidth+ startPos.x; x++)
            {
                x0++;
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = ((x - mapWidth / 2f) + octaveOffsets[i].x) / scale * frequency;
                    float yCoord = ((y - mapHeight / 2f) + octaveOffsets[i].y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
                minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
                perlinNoiseMap[x0, y0] = noiseHeight;
            }

        }
         y = startPos.y;
         x = startPos.x;
         x0 = -1;
         y0 = -1;
        // normalization 
        for ( y = startPos.y; y < mapHeight+ startPos.y; y++)
        {
            y0++; x0 = -1;
            for ( x = startPos.x; x < mapWidth+ startPos.x; x++)
            {
                x0++;
                if (normalizeMode == NormalizeMode.Local)
                    perlinNoiseMap[x0, y0] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, perlinNoiseMap[x0, y0]);
                else
                {
                    float nomalizedHeight = (perlinNoiseMap[x0, y0] + 1) / (2f * maxGlobalHeight / 2f);
                    perlinNoiseMap[x0, y0] = Mathf.Clamp01(nomalizedHeight);
                }

            }
        }
        return perlinNoiseMap;
    }




    public static float[,] GenerateNoiseMap20(float[,] perlinNoiseMap, Vector2Int startPos, int mapWidth,
                                        int mapHeight,
                                        float scale,
                                        int seed,
                                        int octaves,
                                        float persistance,
                                        float lacunarity,
                                        Vector2 offset,
                                        NormalizeMode normalizeMode,float f1,float f2, float f3, float f4)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;
     //   startPos = new Vector2Int(-startPos.x, -startPos.y);
     //   float[,] perlinNoiseMap = new float[mapWidth, mapHeight];

        System.Random pseudoRandomNumber = new System.Random(seed);
        octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);

        //  float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);
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
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = ((x  ) + octaveOffsets[i].x) / scale * frequency;
                    float yCoord = ((y  ) + octaveOffsets[i].y) / scale * frequency;
                 //   float perlinValue = Mathf.PerlinNoise(xCoord, yCoord)*2-1 ;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * f1 - f3;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
                minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
                perlinNoiseMap[x0, y0] = noiseHeight;
            }

        }
        y = startPos.y;
        x = startPos.x;
        x0 = -1;
        y0 = -1;
     //   /*
           for (y = startPos.y; y < mapHeight + startPos.y; y++)
        {
            y0++; x0 = -1;
            for (x = startPos.x; x < mapWidth + startPos.x; x++)
            {
                x0++;
                if (normalizeMode == NormalizeMode.Local)
                    perlinNoiseMap[x0, y0] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, perlinNoiseMap[x0, y0]);
                else
                {
                    float h = perlinNoiseMap[x0, y0];
                    float nomalizedHeight = (h + f2) / (2f * maxGlobalHeight / 2f);
                   h = Mathf.Clamp01(nomalizedHeight);
                    if (h>0.98f)
                    {
                     //   h -= Random.Range(0.01f,0.1f);
                    }
                    h -= 1;
                    h = -h;
                    perlinNoiseMap[x0, y0]=h;
                }

            }
        }
       //  */
        // normalization 

        return perlinNoiseMap;
    }

    public static float GenerateNoiseMap20(Vector2Int startPos,
                                     float scale,
                                     int seed,
                                     int octaves,
                                     float persistance,
                                     float lacunarity,
                                     Vector2 offset,
                                     NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;
        //   startPos = new Vector2Int(-startPos.x, -startPos.y);

        System.Random pseudoRandomNumber = new System.Random(seed);
        octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);

        //  float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);
   
      
      
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = (startPos.x + octaveOffsets[i].x) / scale * frequency;
                    float yCoord = (startPos.y + octaveOffsets[i].y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
                minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
           
      
        // normalization 
        
           
                if (normalizeMode == NormalizeMode.Local)
                    noiseHeight = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseHeight);
                else
                {
                    float nomalizedHeight = (noiseHeight + 1) / (2f * maxGlobalHeight / 2f);
                    noiseHeight = Mathf.Clamp01(nomalizedHeight);
                }

       
        return noiseHeight;
    }

   public static System.Random pseudoRandomNumber ;

 public static float GenerateNoiseMapSingel(  int mapWidth,
                                         int mapHeight,
                                          int x,int y ,
                                         float scale,
                                         int seed,
                                         int octaves,
                                         float persistance,
                                         float lacunarity,
                                         Vector2 offset,
                                         NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;
   float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);
  
      // System.Random pseudoRandomNumber = new System.Random(seed);
      //  Vector2[] octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
      
       
      
           
                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = ((x - mapWidth / 2f) + octaveOffsets[i].x) / scale * frequency;
                    float yCoord = ((y - mapHeight / 2f) + octaveOffsets[i].y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
                minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
           
            

     
                if (normalizeMode == NormalizeMode.Local)
                   noiseHeight = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight,noiseHeight);
                else
                {
                   
                    float nomalizedHeight = (noiseHeight+ 1) / (2f * maxGlobalHeight / 2f);
                  noiseHeight= Mathf.Clamp01(nomalizedHeight);
                }

        return noiseHeight;
    }

    public static float[,] GenerateNoiseMap2( float[,] perlinNoiseMap, int mapWidth,
                                         int mapHeight,
                                          Vector2Int startPos,
                                         float scale,
                                         int seed,
                                         int octaves,
                                         float persistance,
                                         float lacunarity,
                                         Vector2 offset,
                                         NormalizeMode normalizeMode)
    {
        float frequency = 1;
        float amplitude = 1;
        float noiseHeight = 0;


     //   System.Random pseudoRandomNumber = new System.Random(seed);
     //  Vector2[] octaveOffsets = GenerateOffsetsOnSeeds(octaves, seed, offset);
 
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float maxGlobalHeight = GetGlobalMaxHeight(octaves, amplitude, persistance);
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
                frequency = 1;
                amplitude = 1;
                noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                { 
                    float xCoord = ((x - mapWidth / 2f) +octaveOffsets[i].x) / scale * frequency;
                    float yCoord = ((y - mapHeight / 2f) + octaveOffsets[i].y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    // float perlinValue = Mathf.PerlinNoise((x + offset .x) / (float)mapWidth * frequency, (y + offset .y) / (float)mapHeight * frequency) *2 - 1; 


                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = maxLocalNoiseHeight > noiseHeight ? maxLocalNoiseHeight : noiseHeight;
                minLocalNoiseHeight = minLocalNoiseHeight < noiseHeight ? minLocalNoiseHeight : noiseHeight;
                perlinNoiseMap[x0, y0] = noiseHeight;
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
                if (normalizeMode == NormalizeMode.Local)
                    perlinNoiseMap[x0, y0] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, perlinNoiseMap[x0, y0]);
                else
                {
                    float nomalizedHeight = (perlinNoiseMap[x0, y0] + 1) / (2f * maxGlobalHeight / 2f);
                    perlinNoiseMap[x0, y0] = Mathf.Clamp01(nomalizedHeight);
                }

            }
        }
        return perlinNoiseMap;
    }


    public static float[,] GenerateHeights(float[,] _heights, int _width, int _height, Vector2Int startPos, float scale,
                                     int seed,
                                     int octaves,
                                     float Frequency,
                                     float FrequencyMultiplier,
                                      float Amplitude,
                                     float AmplitudeMultiplier
                                    )
    {
        // float[] _heights = new float[_width * _height];
        scale *= 200;
        var _random = new Random(seed);

        Vector2 _offcet = new Vector2(_random.Next(-100000, 100000), _random.Next(-100000, 100000));

        float _maxHeight = (Amplitude * (1 - Mathf.Pow(AmplitudeMultiplier, octaves))) / (1 - AmplitudeMultiplier);
        // int y = startPos.y;
        //  int x = startPos.x;
        int x0 = -1;
        int y0 = -1;
     
        for (int y = startPos.y; y < _height + startPos.y; y++)
        {
            y0++;
            x0 = -1;
            for (int x = startPos.x; x < _width + startPos.x; x++)
            {
                x0++;
                float _resultHeight = 0;

                float _currentFrequency = Frequency;
                float _currentAmplitude = Amplitude;

                for (int i = 0; i < octaves; i++)
                {
                    float _intermediateHeight = 0;

                    float _xResult = (x + _offcet.x) / (256 / _currentFrequency);
                    float _yResult = (y + _offcet.y) / (256 / _currentFrequency);
                    // float _xResult = (x + _offcet.x) / scale*(256 / _currentFrequency);
                    // float _yResult = (y + _offcet.y) / scale*(256 / _currentFrequency);

                    _intermediateHeight += Mathf.PerlinNoise(_xResult, _yResult);

                    _intermediateHeight -= 0.5f;
                    _intermediateHeight *= 2;

                    _intermediateHeight *= _currentAmplitude;


                    _currentAmplitude *= AmplitudeMultiplier;
                    _currentFrequency *= FrequencyMultiplier;

                    _resultHeight += _intermediateHeight;
                }

         //   _resultHeight /= _maxHeight;
           _resultHeight = Mathf.Clamp(_resultHeight, -1, 1);

                _resultHeight /= 2;
                _resultHeight += 0.5f;

                _heights[x0, y0] = _resultHeight;
            }
        }

        return _heights;
    }



    /*
     * 根据seed随机生成 offset
     * 将offset 添加到Coord中
     */
    public static Vector2[] GenerateOffsetsOnSeeds(int octaves, int seed, Vector2 offset)
    {
        System.Random pseudoRandomNumber = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float xOffset = pseudoRandomNumber.Next(-100000, 100000) + offset.x;
            float yOffset = pseudoRandomNumber.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(xOffset, yOffset);
        }

        return octaveOffsets;
    }
  public static Vector2[] GenerateOffsetsOnSeeds(int octaves,  System.Random pseudoRandomNumber, Vector2 offset)
    {
      //  System.Random pseudoRandomNumber = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float xOffset = pseudoRandomNumber.Next(-100000, 100000) + offset.x;
            float yOffset = pseudoRandomNumber.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(xOffset, yOffset);
        }

        return octaveOffsets;
    }
    private static float GetGlobalMaxHeight(int octaves, float amplitude, float persistance)
    {
        float maxGlobalHeight = 0;
        while (octaves > 0)
        {
            maxGlobalHeight += amplitude;
            amplitude *= persistance;
            octaves--;
        }
        return maxGlobalHeight;
    }

}
