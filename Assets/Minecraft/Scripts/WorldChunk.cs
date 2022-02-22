using System.Runtime.CompilerServices;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
//using System.Linq;
//using System.Numerics;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System.Collections.Concurrent;

public struct NoiseJob : IJobParallelFor
{
    [ReadOnly] public int2 data;
    public NativeArray<float> result;
    public NoiseDate noiseMap2;

    public void Execute(int i)
    {

        // result[i] = Mathf.Sqrt(item.x * item.x + item.y * item.y + item.z * item.z);

        for (int x1 = 0; x1 < ChunkManager.Instance.chunkSize.x; x1++)
        {
            for (int z1 = 0; z1 < ChunkManager.Instance.chunkSize.z; z1++)
            {
                noiseMap2.noiseMap[x1, z1] = MapGenerator.Instanc.GenerateMapData2(Vector2.zero, data.x + x1, data.y + z1, 4);
            }
        }

    }
}






public class treeDate
{
    public int treeID;
    public Dictionary<Vector3, BlockType> treeDic;
}


public class NeighborBlocks
{
    public WorldChunk chunks;
    public BlockType blocks;
    // public WorldChunk chunkUp, chunkDown, chunkRight, chunkLeft, chunkForward, chunkBack;
    //  public BlockType blockUp, blockDown, blockRight, blockLeft, blockForward, blockBack;
}

public class NoiseDate
{
public Vector2Int mapid;
public float addTime;//回收时按获取时间排序 先计算的应该最先放回对象池
    public float[,] noiseMap;
    public int[,] hightMap;
    public BlockType[,] topBlock;

    public int maxHight, minHight;


    public bool ING;
    public void NoiseCompute(int x, int z)
    {
        if (ING)
        {
            Debug.Log("PerlinCompute");
            return;
        }

        ING = true;
        //  float t3=Time.realtimeSinceStartup;

        //  Debug.Log(x + "  " + z);
        maxHight = 0;
        minHight = 500;
        if (noiseMap == null)
        {
            hightMap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
            noiseMap = new float[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
            topBlock = new BlockType[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];

        }
        // PerlinNoise.GenerateNoiseMap(noiseMap, new Vector2Int(x, z), ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z);
        noiseMap = MapGenerator.Instanc.GenerateMapData(noiseMap, Vector2.zero, new Vector2Int(x, z), ChunkManager.Instance.chunkSize.x);
        //    noiseMap = md.noiseMap;
        for (int x1 = 0; x1 < ChunkManager.Instance.chunkSize.x; x1++)
        {
            for (int z1 = 0; z1 < ChunkManager.Instance.chunkSize.z; z1++)
            {

                float currentHeight = 0;
                //   Debug.Log(x0+"  "+z0);
                currentHeight = noiseMap[x1, z1];

                float h = MapGenerator.Instanc.heightCurve2.Evaluate(currentHeight) * 150;

                int intH = (int)h;


                if (intH >= maxHight)
                    maxHight = intH;

                if (intH < minHight)
                    minHight = intH;

                hightMap[x1, z1] = intH;
                ///////
                BlockType type = BlockType.Air;
                if (intH <= 30)
                {
                    type = BlockType.Dirt;
                }
                else if (intH > 30 && intH <= 33)
                {

                    type = BlockType.Sand;
                }

                else if (intH > 33 && intH <= 38)
                {

                    type = BlockType.GrassEdge;
                }

                else if (intH > 38 && intH <= 67)
                {
                    type = BlockType.Grass;
                }
                else if (intH > 67 && intH <= 82)
                {

                    type = BlockType.GrassHill;



                }
                else if (intH > 82 && intH <= 125)
                {
                    type = BlockType.GrassHill;

                }
                else if (intH > 125 && intH <= 142)
                {
                    type = BlockType.GrassHill;
                    // type = BlockType.HillStone;
                }
                else if (intH > 142)
                {
                    type = BlockType.HillStone;
                }
                topBlock[x1, z1] = type;
            }

        }
        ING = false;
        //   float t4=Time.realtimeSinceStartup;
        //  ChunkManager.Instance.ttt+=t4-t3;
        return;

        //   */

    }



}


[Serializable]
public class WorldChunk
{

    public bool unload;

    public WorldChunk()
    {

        AddBlocks = new HashSet<Vector3Int>();
        DestroyBlocks = new List<Vector3Int>();
    }

    public static int blockCountPerChunk;
    //上下 右左 前后顺序六个块
    public static NeighborBlocks[] NeiBlocks;
    public Vector3Int ID;
    public List<Vector3> treeslistINDEXPos = new List<Vector3>();
    public List<int> treeslistINDEX = new List<int>();
    public bool isAir;
    public bool isBedrockC;

    [SerializeField]
    public WorldChunk NeighborUp;
    public WorldChunk NeighborDown;
    public WorldChunk NeighborLeft;
    public WorldChunk NeighborRight;
    public WorldChunk NeighborForward;
    public WorldChunk NeighborBack;


    public bool Initialized;


    public bool computedTerrainDate;

    public bool isEmperty = false;
    public bool isModify = false;


    public bool buildMesh;
    public bool IsLoaded;


    public ChunkMeshDate meshDate;
    public BlockType[,,] Blocks;//三维数组的写比读慢了10倍


    public int[,] hightmap;

    public int indexOfExternalBlockArray;

    // public int meshesIndex;


    // public static bool[] visibility = new bool[6];

    public static Bounds bound;

    public static Vector3 boundCenter;
    public static Vector3Int _size;
    public static Vector3Int _sizeSmallOne;

    public HashSet<Vector3Int> AddBlocks;
    public List<Vector3Int> DestroyBlocks;


    public static ConcurrentQueue<NoiseDate> PerlinDatePool = new ConcurrentQueue<NoiseDate>();

    public static Dictionary<Vector2Int, NoiseDate> hightMapDic = new Dictionary<Vector2Int, NoiseDate>();


    // static Vector3Int[] tempmd = new Vector3Int[256];


    public void UnUseSet()
    {
       // try
      //  {
            if (!unload)
            {
              //  return;
            }
            this.blockCount = 0;
            creating = false;
            useing = false;
            biulding = false;
            if (!isAir && !isBedrockC)
            {
                unload = false;
                indexOfExternalBlockArray = 0;
                // meshesIndex = 0;
                isModify = false;
                LOD = 10000;
                haveTree = false;
             
             if (treeslist2.Count>0)
             {
                  for (int i = 0; i < treeslistINDEX.Count; i++)
                {
                    if (i>=treeslist2.Count)
                    {
                        Debug.Log(i+" treeslist2 "+treeslist2.Count+"  "+treeslistINDEX.Count);
                    }
                   
                    var t=treeslist2[i];
                    var index=treeslistINDEX[i];
                      if (index>=ChunkManager.Instance.trees.Count)
                    {
                        Debug.Log(i+" ChunkManager.Instance.trees "+ChunkManager.Instance.trees.Count);
                    }
                    var t2=ChunkManager.Instance.trees[index];
                    ObjPool.ReturnGO<Transform>(t, t2);

                  //  ObjPool.ReturnGO<Transform>(t, ChunkManager.Instance.trees[treeslistINDEX[i]]);

                }
             }
               
                treeslist2.Clear();
                treeslistINDEX.Clear();
                treeslistINDEXPos.Clear();




                Initialized = false;


                computedTerrainDate = false;


                isEmperty = false;

                if (Blocks != null)
                {
                    if (!shareBlocks)
                    {



                        BlocksPool.Push(Blocks);



                    }
                    else
                    {
                        shareBlocks = false;
                    }
                    Blocks = null;
                }


                buildMesh = false;
                IsLoaded = false;
                RemoveNeighbors();


              
                ChunkManager._chunks.Remove(ID);
                if (meshDate != null)
                {
                    meshDate.RetrunPool();
                    meshDate = null;
                }
                if (noiseDate != null)
                {
                

                    noiseDate = null;
                }
                pool.Push(this);
                return;
               


            }
            else
            {
                ChunkManager._chunks.Remove(ID);
                return;

            }
      
    }

    void RemoveNeighbors()
    {
        if (NeighborUp != null)
        {
            NeighborUp.NeighborDown = null;
            NeighborUp = null;
        }
        if (NeighborDown != null)
        {
            NeighborDown.NeighborUp = null;
            NeighborDown = null;
        }
        if (NeighborLeft != null)
        {
            NeighborLeft.NeighborRight = null;
            NeighborLeft = null;
        }
        if (NeighborRight != null)
        {
            NeighborRight.NeighborLeft = null;
            NeighborRight = null;
        }
        if (NeighborForward != null)
        {
            NeighborForward.NeighborBack = null;
            NeighborForward = null;
        }
        if (NeighborBack != null)
        {
            NeighborBack.NeighborForward = null;
            NeighborBack = null;
        }
    }
    public void StartBuildMesh()
    {
         
         try
         {
         
        if (isEmperty)
        {


            return;
        }

        if (shareBlocks)
        {  
            return;
        }
        loadNeighborsBlocks();
     

        if (isEmperty)
        {   
            return;
        }

        if (shareBlocks)
        { 
            return;
        }

     
            BuildOpaqueMesh();
      
        buildMesh = true;
         }
         catch (System.Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e.StackTrace);
        }
     


    }
    public bool IsRebuildOpaqueMesh, IsRebuildWaterMesh, IsRebuildFoliageMesh;
    public bool haveTree;
    public void ReBuildMesh()
    {


       try
         {
         
        if (isEmperty)
        {


            return;
        }

        if (shareBlocks)
        {  
            return;
        }
        loadNeighborsBlocks();
     

        if (isEmperty)
        {   
            return;
        }

        if (shareBlocks)
        { 
            return;
        }

     
            BuildOpaqueMesh();
      
        buildMesh = true;
         }
         catch (System.Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e.StackTrace);
        }

    }

    void Initialize(Vector3Int minCorner)
    {

        if (Initialized)

            return;

        ID = minCorner;

        GetLOD();


        isEmperty = false;




     

        Initialized = true;

        Vector3Int wp = new Vector3Int(minCorner.x, minCorner.y + ChunkManager.Instance.chunkSize.y, minCorner.z);

        if (NeighborUp == null)
        {
            NeighborUp = ChunkManager.GetChunk(wp);
        }

        NeighborUp.NeighborDown = this;

        ChunkManager._chunks.TryAdd(minCorner, this);

        return;
    }


    public void GetLOD()
    {

        // Vector2 v2 = new Vector2(ID.x, ID.z);
        Vector2 playerChunk = new Vector2(ChunkManager.Instance.currentChunkPos.x, ChunkManager.Instance.currentChunkPos.z);
        float xdis = Mathf.Abs(ID.x - ChunkManager.Instance.currentChunkPos.x) / ChunkManager.Instance.chunkSize.x;
        float zdis = Mathf.Abs(ID.z - ChunkManager.Instance.currentChunkPos.z) / ChunkManager.Instance.chunkSize.z;
        if (xdis <= 0.001f && zdis <= 0.001f)
        {
            LOD = 0;
        }
        else if (xdis <= 1.001f && zdis <= 1.001f || zdis <= 1.001f && xdis <= 1.001f)
        {
            LOD = 1;
        }
        else if (xdis <= 2.001f && zdis <= 2.001f || zdis <= 2.001f && xdis <= 2.001f)
        {
            LOD = 2;
        }
        else if (xdis <= 3.001f && zdis <= 3.001f || zdis <= 3.001f && xdis <= 3.001f)
        {
            LOD = 3;
        }
        else if (xdis <= 4.001f && zdis <= 4.001f || zdis <= 4.001f && xdis <= 4.001f)
        {
            LOD = 4;
        }
        else
        {
            LOD = 10000;
        }
    }

    public void InitializeNeighbors()
    {
     
            Vector3Int wp;





            if (NeighborDown == null)
            {
                wp = new Vector3Int(ID.x, ID.y - ChunkManager.Instance.chunkSize.y, ID.z);

                NeighborDown = ChunkManager.GetChunk(wp);

                //   NeighborDown.Initialize(wp);


            }
            //  NeighborDown.GetNoiseDate();
            NeighborDown.NeighborUp = this;
            //    */


            if (NeighborForward == null)
            {
                wp = new Vector3Int(ID.x, ID.y, ID.z + ChunkManager.Instance.chunkSize.z);

                NeighborForward = ChunkManager.GetChunk(wp);

                // NeighborForward.Initialize(wp);


            }
            // NeighborForward.GetNoiseDate();
            NeighborForward.NeighborBack = this;
            if (NeighborBack == null)
            {
                wp = new Vector3Int(ID.x, ID.y, ID.z - ChunkManager.Instance.chunkSize.z);


                NeighborBack = ChunkManager.GetChunk(wp);



            }
            // NeighborBack.GetNoiseDate();
            NeighborBack.NeighborForward = this;
            if (NeighborLeft == null)
            {
                wp = new Vector3Int(ID.x - ChunkManager.Instance.chunkSize.x, ID.y, ID.z);

                NeighborLeft = ChunkManager.GetChunk(wp);

                //  NeighborLeft.Initialize(wp);


            }
            // NeighborLeft.GetNoiseDate();
            NeighborLeft.NeighborRight = this;

            if (NeighborRight == null)
            {
                wp = new Vector3Int(ID.x + ChunkManager.Instance.chunkSize.x, ID.y, ID.z);

                NeighborRight = ChunkManager.GetChunk(wp);



            }
            // NeighborRight.GetNoiseDate();
            NeighborRight.NeighborLeft = this;


            return;
      

        //不能放在初始化方法里 会造成死循环

    }


    public int blockCount;

    public bool siCreateTerrainDating;
    public NoiseDate noiseDate;

    public bool CreateTerrainDatePre()
    {
        if (noiseDate == null)
        {
            Vector2Int noiseId = new Vector2Int(ID.x, ID.z);
            if (hightMapDic.ContainsKey(noiseId))
            {
                // Debug.Log("hightMapDic");
                noiseDate = hightMapDic[noiseId];
            }
            else
            {  // Debug.Log("hightMapDic000000000000000000");
               // GetNoiseDate();
                return false;
            }

        }
        // else
        {
            if (ID.y < noiseDate.minHight - _size.y)

            {
                shareBlocks = true;

                return false;
            }


            if (ID.y > noiseDate.maxHight && ID.y > 30)
            {
                isEmperty = true;

                return false;
            }
            return true;
        }

        // GetNoiseDate();

    }
    public bool useing;
    public bool creating;
    public void CreateTerrainDate()
    {


        if (computedTerrainDate)

            return;
             if (isAir || isBedrockC)
        {

            Debug.Log("isAir || isBedrockC");
            return;
        }
        if (creating)
        {
         //   Debug.Log("creating");
            return;
        }
        creating = true;
       
        if (unload)
        {
            unload = false;
            Debug.Log("waitUnload   " + isAir);

        }

     
        Vector2Int mapid = new Vector2Int(ID.x, ID.z);
       

        if (noiseDate == null)
        {
            GetNoiseDate();
          
        }
        if (ID.y < noiseDate.minHight - _size.y)
       
        {
           
            isEmperty = false;
            computedTerrainDate = true;
            Blocks = StoneBlocks;

            int h = ChunkManager.Instance.chunkSize.y - 1;
            for (int x = 0; x < ChunkManager.Instance.chunkSize.x; x++)
            {
                for (int z = 0; z < ChunkManager.Instance.chunkSize.z; z++)
                {
                    hightmap[x, z] = h;

                }
            }
            siCreateTerrainDating = false;
            shareBlocks = true;
            creating = false;
            return;
        }


        if (ID.y > noiseDate.maxHight && ID.y > 30)
      
        {
           
            isEmperty = true;
            computedTerrainDate = true;

            Blocks = AirBlocks;
            for (int x = 0; x < ChunkManager.Instance.chunkSize.x; x++)
            {
                for (int z = 0; z < ChunkManager.Instance.chunkSize.z; z++)
                {
                    hightmap[x, z] = 0;

                }
            }
            siCreateTerrainDating = false;
            shareBlocks = true;
            // Debug.Log(666);
            creating = false;

            return;
        }
  

        bool eee = false;
        isEmperty = false;




     Blocks = GetBlocks();

rd= new System.Random((ID.x+ID.y+ID.z)+(ID.x*ID.z));
    

        blockCount = 0;
        for (int x = 0; x < _size.x; x++)
        {
            //  int wpx = x + ID.x;
            for (int z = 0; z < _size.z; z++)
            {
                // int wpz = z + ID.z;
                int h = 0;
                int mapHeight = noiseDate.hightMap[x, z];
                var topType = noiseDate.topBlock[x, z];
                dirtThickness = NeighborUp.dirtThickness;
                for (int y = _sizeSmallOne.y; y > -1; y--)
                //for (int y = 0; y < _size.y; y++)
                {
                    // float t = Time.realtimeSinceStartup; 
                    BlockType type = GetBlockType(x, y, z, mapHeight, topType);
                    if (!type.isAir)
                    {//if (isEmperty)
                          //      isEmperty = false;
                        if (!type.isTransparent)
                            
                        {

                            // blockCount++;
                            if (!eee)
                            {
                                h = y;
                                eee = true;

                            }


                        }
                        if (type == null)
                        {
                            Debug.Log("xxxxxxxxxxxxxxxxxxxxxxxxxxxx");
                        }
                       
                    }
                   
                     if (type != Blocks[x, y, z])
                        {
                            Blocks[x, y, z] = type;
                         
                        }
                   



                }
                if (eee)
                {
                    //发现了不透明的方块 
                    eee = false;
                    hightmap[x, z] = h;
                  
                    // h = 0;

                }
                else
                {
                    hightmap[x, z] = 0;
                    //如果整个竖排从下到上都没有不透明的方块 就不能完全遮住下面的chunk


                }


            }
        }

    
        computedTerrainDate = true;
        siCreateTerrainDating = false;
        creating = false;
     

    }





 public System.Random rd ;
    int dirtThickness;
    BlockType GetBlockType(int x, int y, int z, int mapHeight, BlockType topType)
    {

        //float t3 = Time.realtimeSinceStartup;

        int worldY = ID.y + y;
        BlockType type = BlockType.Air;
        // MapData md = pnpc.md;

        if (worldY > mapHeight)
        {
            if (worldY <= 30)
            {
                //type = BlockType.Water;

                return BlockType.Water;
            }
            else
            {

                return BlockType.Air;
            }
        }
        else if (worldY == mapHeight)
        {
            type = topType;
           //return topType;
            //最上面一层不做判断 麻烦
            if (y < _sizeSmallOne.y)
            {
                // BlockType type = BlockType.Air;
                BlockType topBlock = null;
               // float plantProbability = Random.value;
               // float p = Random.value;
             var plantProbability= rd.NextDouble();
               var p=  rd.NextDouble();
              

                if (type.flag == BlockFlag.grassTop)
                {



                    if (plantProbability < 0.2)
                    {



                        topBlock = BlockType.GetTallGrassBlock();
                        if (p < 0.2)
                        {
                            topBlock = BlockType.GetPlantBlockTypes();

                        }
                        else if (p >= 0.45 && p < 0.452 || p >= 0.5 && p < 0.501)
                        {

                            topBlock = BlockType.Tree1;
                        }

                    }
                }

                else if (type.flag == BlockFlag.grass2water)
                {



                    if (plantProbability < 0.2)
                    {


                        topBlock = BlockType.GetTallGrassBlock();
                        //  treePosDic2.Add(new Vector3Int(x, y - ID.y, z), tree02);
                        if (p < 0.2)
                        {
                            topBlock = BlockType.GetPlantBlockTypes();

                        }
                        else if (p >= 0.45 && p < 0.49 || p >= 0.5 && p < 0.51)
                        // else if (p >= 0.25f && p < 0.255f )
                        {

                            topBlock = BlockType.Tree1;
                        }
                        //水周边有更多的植被





                    }
                }
                if (topBlock != null)
                {
                    Blocks[x, y + 1, z] = topBlock;
                }
               
            }

            return type;
        }
        else
        {

            if (worldY > 0)
            {

                if (mapHeight <= 30)
                {
                    if (dirtThickness < 5)
                    {
                        dirtThickness++;
                        return BlockType.Dirt;

                    }
                    else
                    {
                        return BlockType.Stone;
                    }

                }
                else if (mapHeight > 30 && mapHeight <= 33)
                {
                    if (topType == BlockType.Sand)
                    {
                        return BlockType.Sand;
                    }
                    else
                    {
                        if (dirtThickness < 5)
                        {
                            dirtThickness++;
                            return BlockType.Dirt;

                        }
                        else
                        {
                            return BlockType.Stone;
                        }
                    }

                }

                else if (mapHeight > 33 && mapHeight <= 40)
                {

                    if (dirtThickness < 5)
                    {
                        dirtThickness++;
                        return BlockType.Dirt;

                    }
                    else
                    {
                        return BlockType.Stone;
                    }
                }

                else if (mapHeight > 40 && mapHeight <= 67)
                {
                    if (dirtThickness < 5)
                    {
                        dirtThickness++;
                        return BlockType.Dirt;

                    }
                    else
                    {
                        return BlockType.Stone;
                    }
                }
                else
                {

                    return BlockType.Dirt;
                    //   type = BlockType.Grass;


                }

            }
            else if (worldY <= -10)
            {
                return BlockType.Bedrock;
            }
            else if (worldY > -10 && worldY <= 0)
            {
                return BlockType.Stone;
            }

        }
        //  float t4 = Time.realtimeSinceStartup;
        //  ChunkManager.Instance.ttt += t4 - t3;

        ///*

        return BlockType.Air;

    }

    //public static Dictionary<Vector3Int, BlockType> treePosDic = new Dictionary<Vector3Int, BlockType>();
    public bool biulding;

    public MeshData_ OpaqueMeshMD;
    public MeshData_ WaterMeshMD;
    public MeshData_ FoliageMeshMD;

    public void BuildMeshInMainThread()
    {
        var chunk = this;
        if (chunk.meshDate==null)
        {
               chunk.meshDate = ChunkMeshDate.GetMeshDate(chunk);
        }
     

        if (chunk.OpaqueMeshMD.vertexCount > 0)
        {

            if (!chunk.IsLoaded)
                chunk.IsLoaded = true;

            Mesh mesh = chunk.meshDate.OpaqueMeshFilter.sharedMesh;
            MeshData_.Combine(chunk.OpaqueMeshMD, mesh);


            chunk.meshDate.OpaqueMeshCol.sharedMesh = chunk.meshDate.OpaqueMeshFilter.sharedMesh;

            MeshData_.ReturnMax(chunk.OpaqueMeshMD);
            chunk.OpaqueMeshMD = null;

          //  if (chunk.LOD <= 3)
            {
                //   OpaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.On;

                chunk.meshDate.  OpaqueMeshRenderer.receiveShadows = true;

                 chunk.meshDate. OpaqueMeshCol.enabled = true;
                //  OpaqueMeshRenderer.sharedMaterial = ChunkManager.Instance.Opaque;
            }
           // else
            {
                //  OpaqueMeshRenderer.sharedMaterial = ChunkManager.Instance.Opaque;
                // OpaqueMeshCol.enabled = false;
            }


            if (chunk.meshDate.OpaqueMeshRenderer.renderingLayerMask == 0)
                chunk.meshDate.OpaqueMeshRenderer.renderingLayerMask = 1;
        }
        else
        {
            chunk.meshDate.OpaqueMeshRenderer.renderingLayerMask = 0;
            MeshData_.ReturnMax(chunk.OpaqueMeshMD);
            chunk.OpaqueMeshMD = null;
        }
        if (chunk.FoliageMeshMD.vertexCount > 0)
        {
            if (!chunk.IsLoaded)
                chunk.IsLoaded = true;
            Mesh mesh = chunk.meshDate.FoliageMeshFilter.sharedMesh;
            // MeshData_.Combine(coubinMD, mesh);
            MeshData_.Combine(chunk.FoliageMeshMD, mesh);

         chunk.meshDate.FoliageMeshCol.sharedMesh = mesh;

            MeshData_.ReturnMax(chunk.FoliageMeshMD);
            chunk.FoliageMeshMD = null;

  // if (chunk.LOD <= 3)
            {
                chunk.meshDate.FoliageMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
                chunk.meshDate.FoliageMeshRenderer.receiveShadows = true;

                chunk.meshDate.FoliageMeshCol.enabled = true;
                chunk.meshDate.FoliageMeshRenderer.sharedMaterial = ChunkManager.Instance.Foliage;
            }
          //  else
            {
             //   chunk.meshDate.FoliageMeshRenderer.sharedMaterial = ChunkManager.Instance.Foliage;
            }

         


            if (chunk.meshDate.FoliageMeshRenderer.renderingLayerMask == 0)
                chunk.meshDate.FoliageMeshRenderer.renderingLayerMask = 1;


               for (int i = 0; i <  chunk.treeslistINDEXPos.Count; i++)
               {
         chunk.treeslist2.Add(ObjPool.GetComponent<Transform>(ChunkManager.Instance.trees[ chunk.treeslistINDEX[i]],  chunk.treeslistINDEXPos[i], Quaternion.identity));

               }
        }
        else
        {
            chunk.meshDate.FoliageMeshRenderer.renderingLayerMask = 0;
            MeshData_.ReturnMax(chunk.FoliageMeshMD);
            chunk.FoliageMeshMD = null;
        }
        if (chunk.WaterMeshMD.vertexCount > 0)
        {
            if (!chunk.IsLoaded)
                chunk.IsLoaded = true;
            Mesh mesh = chunk.meshDate.WaterMeshFilter.sharedMesh;
            MeshData_.Combine(chunk.WaterMeshMD, mesh);
            chunk.meshDate.WaterMeshCol.sharedMesh = mesh;

            MeshData_.ReturnMax(chunk.WaterMeshMD);
            chunk.WaterMeshMD = null;

          //  if (chunk.LOD <= 3)
            {
                chunk.meshDate.WaterMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
              //  chunk.meshDate.WaterMeshRenderer.receiveShadows = true;
                chunk.meshDate.WaterMeshRenderer.sharedMaterial = ChunkManager.Instance.Water;
              //  chunk.meshDate.WaterMeshCol.enabled = true;
            }
          //  else
            {
              //  chunk.meshDate.WaterMeshRenderer.sharedMaterial = ChunkManager.Instance.Water;
              //  chunk.meshDate.WaterMeshCol.enabled = false;
            }
            if (chunk.meshDate.WaterMeshRenderer.renderingLayerMask == 0)
                chunk.meshDate.WaterMeshRenderer.renderingLayerMask = 1;
        }
        else
        {
          //  chunk.meshDate.WaterMeshRenderer.renderingLayerMask = 0;
            MeshData_.ReturnMax(chunk.WaterMeshMD);
            chunk.WaterMeshMD = null;
        }


        //   Profiler.EndSample();

        // float t2=Time.realtimeSinceStartup;
        //  ChunkManager.Instance.ttt+=t2-t;
        //  Debug.Log(3333333333333333333);

        chunk.biulding = false;
        chunk.buildMesh = true;



    }

    public void BuildOpaqueMesh()
    {

        if (biulding)
        {
            Debug.Log("BuildOpaqueMesh");
            return;
        }
        biulding = true;
    
            OpaqueMeshMD = MeshData_.GetMax();
            FoliageMeshMD = MeshData_.GetMax();
            WaterMeshMD = MeshData_.GetMax();
            if (isEmperty)
            {
                Debug.Log("xxxxxxxxxxxxxxxx");
            }

            int ccc = 0;


            bool[] visibility2 = new bool[6];
            for (int x = 0; x < _size.x; x++)
            {
                for (int z = 0; z < _size.z; z++)
                {
                    int maxy = hightmap[x, z];
                    int miniy = GetMiny(x, z, maxy);

                  // for (int y = miniy; y < _size.y; y++)
                    // for (int y = 0; y < _size.y; y++)
                   for (int y = _sizeSmallOne.y; y > -1; y--)
                    {

                        BlockType block = Blocks[x, y, z];

                        if (block.isTransparent)
                        {
                            if (block.isWater)
                            {
                                // continue;
                                bool quauCount = GetVisibilityWater(x, y, z, visibility2, block);
                                if (quauCount)
                                {

                                    int index = WaterMeshMD.vertexCount;
                                    int vc = block.GenerateWaterFaces(visibility2, WaterMeshMD);
                                    for (int ii = 0; ii < vc; ii++)
                                    {
                                        int newIndex = index + ii;
                                        Vector3 vp = WaterMeshMD.vertexDate[newIndex].vertice;
                                        vp.Set(vp.x + x, vp.y + y, vp.z + z);
                                        WaterMeshMD.vertexDate[newIndex].vertice = vp;

                                    }

                                }
                            }
                            else
                            {
                                if (block == BlockType.Tree1 && !haveTree)
                                {
                                      // int treeindex0 = Random.Range(0, ChunkManager.Instance.trees.Count);
                                      int treeindex0 = rd.Next(0, ChunkManager.Instance.trees.Count);
                                    treeslistINDEX.Add(treeindex0);
                                    treeslistINDEXPos.Add(new Vector3(ID.x + x, ID.y + y - 0.7f, ID.z + z));
                                    //  treeslist2.Add(ObjPool.GetComponent<Transform>(ChunkManager.Instance.trees[treeindex0], new Vector3(ID.x + x, ID.y + y - 0.7f, ID.z + z), Quaternion.identity));
                                    continue;

                                }

                                if (!block.isBillboard)
                                    continue;


                                int index = FoliageMeshMD.vertexCount;
                                int vc = block.GenerateBillboardFaces(FoliageMeshMD);
                                for (int ii = 0; ii < vc; ii++)
                                {
                                    int newIndex = index + ii;
                                    Vector3 vp = FoliageMeshMD.vertexDate[newIndex].vertice;
                                    vp.Set(vp.x + x, vp.y + y, vp.z + z);
                                    FoliageMeshMD.vertexDate[newIndex].vertice = vp;
                                }
                            }
                        }
                        else
                        {
                            bool quauCount = GetVisibility002(x, y, z, visibility2);

                            if (quauCount)
                            {
                                int index = OpaqueMeshMD.vertexCount;

                                int vc = block.GenerateMesh(visibility2, OpaqueMeshMD);
                                for (int ii = 0; ii < vc; ii++)
                                {
                                    int newIndex = index + ii;
                                    Vector3 vp = OpaqueMeshMD.vertexDate[newIndex].vertice;
                                    vp.Set(vp.x + x, vp.y + y, vp.z + z);
                                    OpaqueMeshMD.vertexDate[newIndex].vertice = vp;

                                }

                            }
                        }




                        //  }



                    }
                }
            }

            ChunkManager.LoadChunkQueue.Enqueue(this);
            return;
         
    }

    public int LOD = -1;
    public List<Transform> treeslist2 = new List<Transform>();




    //判断一个方块四面的高度进行对比 可以减少一部分判断计算 如果中间的方块比四周的方块高出一格 那么中间的方块就不用从上到下全部做剔除操作了 只要最上面的一个方块做测试就行了 下面的都被挡住了 是看不到的

    protected int GetMiny(int x, int z, int maxy)
    {


        int miny = maxy;
        int temp;

        if (x == 0 || z == 0 || x == _sizeSmallOne.x || z == _sizeSmallOne.z)
        {
            bool x0 = true; bool z0 = true;
            bool x1 = true; bool z1 = true;

            if (x == 0)
            {
                x1 = false;
                temp = NeighborLeft.hightmap[_sizeSmallOne.x, z];
                if (miny > temp)
                    miny = temp;

            }
            if (x == _sizeSmallOne.x)
            {
                x0 = false;
                temp = NeighborRight.hightmap[0, z];
                if (miny > temp)
                    miny = temp;
            }

            if (z == 0)
            {
                z1 = false;
                temp = NeighborBack.hightmap[x, _sizeSmallOne.z];
                if (miny > temp)
                    miny = temp;
            }
            if (z == _sizeSmallOne.z)
            {
                z0 = false;
                temp = NeighborForward.hightmap[x, 0];
                if (miny > temp)
                    miny = temp;
            }


            if (x0)
            {
                temp = hightmap[x + 1, z];
                if (miny > temp)
                    miny = temp;
            }
            if (x1)
            {
                temp = hightmap[x - 1, z];
                if (miny > temp)
                    miny = temp;
            }
            if (z0)
            {
                temp = hightmap[x, z + 1];
                if (miny > temp)
                    miny = temp;
            }
            if (z1)
            {
                temp = hightmap[x, z - 1];
                if (miny > temp)
                    miny = temp;

            }

            return miny;
        }
        else
        {
            temp = hightmap[x + 1, z];
            if (miny > temp)
                miny = temp;
            temp = hightmap[x - 1, z];
            if (miny > temp)
                miny = temp;
            temp = hightmap[x, z + 1];
            if (miny > temp)
                miny = temp;
            temp = hightmap[x, z - 1];
            if (miny > temp)
                miny = temp;
            return miny;
        }

    }






    protected bool GetVisibility002(int x, int y, int z, bool[] visibility_)
    {


        if (visibility_ == null)
        {
            Debug.LogError("visibility_==null");
            visibility_ = new bool[6];
        }

        if (NeighborUp.Blocks[x, 0, z] == null)
        {
            Debug.LogError(" NeighborUp.Blocks[x, 0, z]");
            //  visibility_=new bool[6];
        }
        //chunk 外围的方块
        // if(block.side)
        try
        {
            if (y == _sizeSmallOne.y)
            {
                if (!NeighborUp.isEmperty)
                {
                    visibility_[0] = NeighborUp.Blocks[x, 0, z].isTransparent;

                }
                else

                    visibility_[0] = true;


                visibility_[1] = Blocks[x, y - 1, z].isTransparent;

            }
            else if (y == 0)
            {



                visibility_[0] = Blocks[x, y + 1, z].isTransparent;


                if (!NeighborDown.isEmperty)
                    visibility_[1] = NeighborDown.Blocks[x, _sizeSmallOne.y, z].isTransparent;
                else
                    visibility_[1] = false;




            }
            else
            {
                visibility_[0] = Blocks[x, y + 1, z].isTransparent;

                visibility_[1] = Blocks[x, y - 1, z].isTransparent;
            }

            ////////////////////
            if (x == _sizeSmallOne.x)
            {


                if (NeighborRight.computedTerrainDate)
                    visibility_[2] = NeighborRight.Blocks[0, y, z].isTransparent;

                else
                    visibility_[2] = true;

                visibility_[3] = Blocks[x - 1, y, z].isTransparent;


            }

            else if (x == 0)
            {


                visibility_[2] = Blocks[x + 1, y, z].isTransparent;

                if (NeighborLeft.computedTerrainDate)
                    visibility_[3] = NeighborLeft.Blocks[_sizeSmallOne.x, y, z].isTransparent;

                else
                    visibility_[3] = true;

            }
            else
            {
                visibility_[2] = Blocks[x + 1, y, z].isTransparent;

                visibility_[3] = Blocks[x - 1, y, z].isTransparent;
            }
            if (z == _sizeSmallOne.z)
            {



                if (NeighborForward.computedTerrainDate)
                    visibility_[4] = NeighborForward.Blocks[x, y, 0].isTransparent;

                else
                    visibility_[4] = true;


                visibility_[5] = Blocks[x, y, z - 1].isTransparent;

            }
            else if (z == 0)
            {

                visibility_[4] = Blocks[x, y, z + 1].isTransparent;

                if (NeighborBack.computedTerrainDate)
                    visibility_[5] = NeighborBack.Blocks[x, y, _sizeSmallOne.z].isTransparent;

                else
                    visibility_[5] = true;



            }
            else
            {

                visibility_[4] = Blocks[x, y, z + 1].isTransparent;

                visibility_[5] = Blocks[x, y, z - 1].isTransparent;

            }




            for (int ni = 0; ni < 6; ni++)
            {
                if (visibility_[ni])
                    return true;
            }

            return false;
        }
        catch (System.Exception e)
        {

            Debug.LogError(e.Message);
            Debug.LogError(e.StackTrace);
            return false;
        }




    }



    protected bool GetVisibilityWater(int x, int y, int z, bool[] visibility_, BlockType block)
    {

        if (y == _sizeSmallOne.y)
        {
            if (NeighborUp.computedTerrainDate)
            {
                var temp2 = NeighborUp.Blocks[x, 0, z];
                if (temp2.isWater)

                    return false;
                else
                {
                    return true;
                }



            }
            else
            {
                return true;

            }

        }


        var temp = Blocks[x, y + 1, z];
        if (temp.isWater)

            return false;
        else
        {
            return true;
        }



    }



    public void loadNeighborsBlocks()
    {


        InitializeNeighbors();



        if (!computedTerrainDate)
            CreateTerrainDate();



        //  if (!NeighborRight.computedTerrainDate)
        NeighborRight.CreateTerrainDate();

        //  if (!NeighborLeft.computedTerrainDate)
        NeighborLeft.CreateTerrainDate();

        //    if (!NeighborForward.computedTerrainDate)
        NeighborForward.CreateTerrainDate();

        //    if (!NeighborBack.computedTerrainDate)
        NeighborBack.CreateTerrainDate();

        //if (!NeighborUp.computedTerrainDate)
        NeighborUp.CreateTerrainDate();


        //  if (NeighborDown)
        //   {
        // if (!NeighborDown.computedTerrainDate)
        NeighborDown.CreateTerrainDate();
        //    }




        //  block.side = false;
        // BlockType ntype;





        return;

    }




    public Vector3Int WorldToLocalPosition(Vector3Int worldPos)
    {
        return worldPos - ID;
    }

    private bool LocalPositionIsInRange(Vector3Int localPos)
    {
        //  return localPos.x >= 0 && localPos.z >= 0 && localPos.x < _size.x && localPos.z < _size.z && localPos.y >= 0 && localPos.y < _size.y;
        if (localPos.x > _sizeSmallOne.x || localPos.y > _sizeSmallOne.y || localPos.z > _sizeSmallOne.z || localPos.x < 0 || localPos.y < 0 || localPos.z < 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //chunk坐标
    public BlockType GetBlockAtWorldPosition(Vector3Int worldPos)
    {


        if (isAir)
        {
            return BlockType.Air;
        }

        Vector3Int localPos = WorldToLocalPosition(worldPos);
        return GetBlockAtChunkPos(localPos.x, localPos.y, localPos.z);
    }


    public BlockType GetBlockAtChunkPos(int x, int y, int z)
    {



        if (!computedTerrainDate)
        {
            CreateTerrainDate();
        }
        return Blocks[x, y, z];
    }



    public void PlaceBlock(Vector3Int loclpPos, BlockType block)
    {

        if (isEmperty)
        {
            shareBlocks = false;
            isEmperty = false;
            if (shareBlocks)
            {
                SwitchBlocks();

            }



        }
        if (!isModify)
            isModify = true;


        if (!block.isTransparent)
        {
            // if (lpos.y != 0)
            var h = hightmap[loclpPos.x, loclpPos.z];
            if (loclpPos.y > h)
            {
                hightmap[loclpPos.x, loclpPos.z] = loclpPos.y + 1;
            }

        }
        Blocks[loclpPos.x, loclpPos.y, loclpPos.z] = block;



        hightmap[loclpPos.x, loclpPos.z]++;
        ChunkManager.addChunks.Add(this);

    }




    public void GetNeighborBlocks(int x, int y, int z)
    {

        if (y == _sizeSmallOne.y)
        {
            NeiBlocks[0].chunks = NeighborUp;
            if (!NeighborUp.isEmperty)
                NeiBlocks[0].blocks = NeighborUp.Blocks[x, 0, z];
            else
                NeiBlocks[0].blocks = BlockType.Air;



            NeiBlocks[1].chunks = this;
            NeiBlocks[1].blocks = Blocks[x, y - 1, z];

        }
        ////////////////////
        else if (y == 0)
        {

            NeiBlocks[0].chunks = this;
            NeiBlocks[0].blocks = Blocks[x, y + 1, z];
            NeiBlocks[1].chunks = NeighborDown;
            if (!NeighborDown.isEmperty)
                NeiBlocks[1].blocks = NeighborDown.Blocks[x, _sizeSmallOne.y, z];
            else
                NeiBlocks[1].blocks = BlockType.Air;

        }
        else
        {
            NeiBlocks[0].chunks = this;
            NeiBlocks[0].blocks = Blocks[x, y + 1, z];

            NeiBlocks[1].chunks = this;
            NeiBlocks[1].blocks = Blocks[x, y - 1, z];
        }

        ////////////////////
        if (x == _sizeSmallOne.x)
        {
            NeiBlocks[2].chunks = NeighborRight;
            if (!NeighborRight.isEmperty)
                NeiBlocks[2].blocks = NeighborRight.Blocks[0, y, z];
            else
                NeiBlocks[2].blocks = BlockType.Air;

            NeiBlocks[3].chunks = this;
            NeiBlocks[3].blocks = Blocks[x - 1, y, z];


        }

        else if (x == 0)
        {

            NeiBlocks[2].chunks = this;
            NeiBlocks[2].blocks = Blocks[x + 1, y, z];
            NeiBlocks[3].chunks = NeighborLeft;
            if (!NeighborLeft.isEmperty)
                NeiBlocks[3].blocks = NeighborLeft.Blocks[_sizeSmallOne.x, y, z];
            else
                NeiBlocks[3].blocks = BlockType.Air;

        }
        else
        {
            NeiBlocks[2].chunks = this;
            NeiBlocks[2].blocks = Blocks[x + 1, y, z];
            NeiBlocks[3].chunks = this;
            NeiBlocks[3].blocks = Blocks[x - 1, y, z];



        }
        if (z == _sizeSmallOne.z)

        {

            NeiBlocks[4].chunks = NeighborForward;
            if (!NeighborForward.isEmperty)
                NeiBlocks[4].blocks = NeighborForward.Blocks[x, y, 0];
            else
                NeiBlocks[4].blocks = BlockType.Air;
            NeiBlocks[5].chunks = this;
            NeiBlocks[5].blocks = Blocks[x, y, z - 1];



        }
        else if (z == 0)
        {

            NeiBlocks[4].chunks = this;
            NeiBlocks[4].blocks = Blocks[x, y, z + 1];
            NeiBlocks[5].chunks = NeighborBack;
            if (!NeighborBack.isEmperty)
                NeiBlocks[5].blocks = NeighborBack.Blocks[x, y, _sizeSmallOne.z];
            else
                NeiBlocks[5].blocks = BlockType.Air;


        }
        else
        {
            NeiBlocks[4].chunks = this;
            NeiBlocks[4].blocks = Blocks[x, y, z + 1];

            NeiBlocks[5].chunks = this;
            NeiBlocks[5].blocks = Blocks[x, y, z - 1];
        }

        return;
    }



    public void DesttroyBlocksFun()
    {

        // public static BlockType[,,] bedrockBlocks;
        if (!isModify)
            isModify = true;
        if (shareBlocks)
        {
            SwitchBlocks();

        }

        foreach (var lpos in DestroyBlocks)
        {
            BlockType block = Blocks[lpos.x, lpos.y, lpos.z];
            if (block.isTree)
            {
                //  Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Air;
                continue;
            }
            if (block.isPlant)
            {

            }
            bool haveWaterAround = false;//周围有没有水

            int x = lpos.x; int y = lpos.y; int z = lpos.z;

            GetNeighborBlocks(x, y, z);
            for (int i = 0; i < NeiBlocks.Length; i++)
            {
                var n = NeiBlocks[i];
                if (n.chunks.shareBlocks)
                    n.chunks.SwitchBlocks();

                if (!haveWaterAround)
                {
                    if (i != 1)//下面的水不会跑到上面来 
                    {
                        if (n.blocks.isWater)
                            haveWaterAround = true;
                    }

                }
                ChunkManager.TouchedChunks.Add(n.chunks);


            }




            if (block.isBillboard)
            {
                IsRebuildFoliageMesh = true;
            }
            if (block.isWater)
            {
                IsRebuildWaterMesh = true;
            }
            if (!block.isTransparent)
            {
                IsRebuildOpaqueMesh = true;
                // if (lpos.y != 0)
                int h = hightmap[lpos.x, lpos.z];
                //   Debug.Log(lpos+" zzzzzz "+h );
                if (h >= lpos.y)
                {

                    h = lpos.y - 1;
                    if (h == -1)
                        h = 0;
                    hightmap[lpos.x, lpos.z] = h;

                }


            }


            if (haveWaterAround)
            {
                Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Water;
                IsRebuildWaterMesh = true;
                //还要考虑四周是不是有空方块
                WaterFlowCheck(lpos.x, lpos.y, lpos.z);
            }
            else
            {
                Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Air;
                if (block.blockName == BlockNameEnum.Grass)
                {
                    if (lpos.y < _sizeSmallOne.y)
                    {
                        if (Blocks[lpos.x, lpos.y + 1, lpos.z].isPlant)
                        {
                            Blocks[lpos.x, lpos.y + 1, lpos.z] = BlockType.Air;
                        }
                    }
                    else
                    {
                        if (NeighborUp.Blocks[lpos.x, 0, lpos.z].isPlant)
                        {
                            NeighborUp.Blocks[lpos.x, 0, lpos.z] = BlockType.Air;
                        }
                    }
                }
            }

        }

        ChunkManager.TouchedChunks.Add(this);

        DestroyBlocks.Clear();

        return;
    }



    public void WaterFlowCheck(int x, int y, int z)
    {


        if (y == 0)
        {
            if (NeighborDown.shareBlocks)
                NeighborDown.SwitchBlocks();
            if (NeighborDown.Blocks[x, _sizeSmallOne.y, z].isAir)
            {
                NeighborDown.Blocks[x, _sizeSmallOne.y, z] = BlockType.Water;
                NeighborDown.WaterFlowCheck(x, _sizeSmallOne.y, z);
            }


            ChunkManager.TouchedChunks.Add(NeighborDown);

        }
        else
        {

            if (Blocks[x, y - 1, z].isAir)
            {
                Blocks[x, y - 1, z] = BlockType.Water;
                WaterFlowCheck(x, y - 1, z);
            }
        }

        if (x == 0)
        {
            if (NeighborLeft.shareBlocks)
                NeighborLeft.SwitchBlocks();
            if (NeighborLeft.Blocks[_sizeSmallOne.x, y, z].isAir)
            {

                NeighborLeft.Blocks[_sizeSmallOne.x, y, z] = BlockType.Water;
                NeighborLeft.WaterFlowCheck(_sizeSmallOne.x, y, z);
            }

            ChunkManager.TouchedChunks.Add(NeighborLeft);
        }
        else if (x == _sizeSmallOne.x)
        {
            if (NeighborRight.shareBlocks)
                NeighborRight.SwitchBlocks();
            if (NeighborRight.Blocks[0, y, z].isAir)
            {
                NeighborRight.Blocks[0, y, z] = BlockType.Water;
                NeighborRight.WaterFlowCheck(0, y, z);
            }




            ChunkManager.TouchedChunks.Add(NeighborRight);
        }
        else
        {
            if (Blocks[x + 1, y, z].isAir)
            {
                Blocks[x + 1, y, z] = BlockType.Water;
                WaterFlowCheck(x + 1, y, z);
            }
            if (Blocks[x - 1, y, z].isAir)
            {
                Blocks[x - 1, y, z] = BlockType.Water;
                WaterFlowCheck(x - 1, y, z);
            }
        }


        if (z == 0)
        {
            if (NeighborBack.shareBlocks)
                NeighborBack.SwitchBlocks();
            if (NeighborBack.Blocks[x, y, _sizeSmallOne.z].isAir)
            {
                NeighborBack.Blocks[x, y, _sizeSmallOne.z] = BlockType.Water;
                NeighborBack.WaterFlowCheck(x, y, _sizeSmallOne.z);
            }


            ChunkManager.TouchedChunks.Add(NeighborBack);

        }
        else if (z == _sizeSmallOne.z)
        {
            if (NeighborForward.shareBlocks)
                NeighborForward.SwitchBlocks();
            if (NeighborForward.Blocks[x, y, 0].isAir)
            {
                NeighborForward.Blocks[x, y, 0] = BlockType.Water;
                NeighborForward.WaterFlowCheck(x, y, 0);
            }


            ChunkManager.TouchedChunks.Add(NeighborForward);
        }
        else
        {
            if (Blocks[x, y, z + 1].isAir)
            {
                Blocks[x, y, z + 1] = BlockType.Water;
                WaterFlowCheck(x, y, z + 1);
            }
            if (Blocks[x, y, z - 1].isAir)
            {
                Blocks[x, y, z - 1] = BlockType.Water;
                WaterFlowCheck(x, y, z - 1);
            }
        }



    }


    public static ConcurrentStack<WorldChunk> pool = new ConcurrentStack<WorldChunk>(new Stack<WorldChunk>(4096));
    public static WorldChunk GetChunk(Vector3Int chunkID)
    {
        WorldChunk chunk;

        chunk = new WorldChunk();

        chunk.hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
        chunk.Initialize(chunkID);
        return chunk;


        if (pool.Count > 0)
        {
            if (!pool.TryPop(out chunk))
            {
                chunk = new WorldChunk();
                ChunkManager.Instance.c5++;
                chunk.hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
                chunk.Initialize(chunkID);
            }
            else
            {
                if (!chunk.Initialized)
                    chunk.Initialize(chunkID);

                if (chunk.meshDate != null)
                {
                    Debug.Log("chunk.meshDate!=null");
                    chunk.meshDate = null;
                }
                //  chunk.hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
            }

        }
        else
        {
            chunk = new WorldChunk();
            ChunkManager.Instance.c5++;
            chunk.hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
            chunk.Initialize(chunkID);

            //                Debug.Log("GetChunk");
        }
        return chunk;


    }



    public static BlockType[,,] StoneBlocks;
    public static BlockType[,,] bedrockBlocks;
    public static BlockType[,,] AirBlocks;
    public bool shareBlocks;


    public void SwitchBlocks()
    {
        shareBlocks = false;

        if (Blocks == StoneBlocks)
        {
            Blocks = null;
            Blocks = GetBlocks();
            for (int x = 0; x < ChunkManager.Instance.chunkSize.x; x++)
            {
                for (int z = 0; z < ChunkManager.Instance.chunkSize.z; z++)
                {
                    hightmap[x, z] = _sizeSmallOne.y;
                    for (int y = 0; y < ChunkManager.Instance.chunkSize.y; y++)
                    {
                        Blocks[x, y, z] = BlockType.Stone;
                    }
                }
            }
        }

        if (Blocks == bedrockBlocks)
        {
            Blocks = null;
            Blocks = GetBlocks();
            for (int x = 0; x < ChunkManager.Instance.chunkSize.x; x++)
            {
                for (int z = 0; z < ChunkManager.Instance.chunkSize.z; z++)
                {
                    hightmap[x, z] = _sizeSmallOne.y;
                    for (int y = 0; y < ChunkManager.Instance.chunkSize.y; y++)
                    {
                        Blocks[x, y, z] = BlockType.Bedrock;
                    }
                }
            }
        }
        if (Blocks == AirBlocks)
        {
            Blocks = null;
            Blocks = GetBlocks();
            for (int x = 0; x < ChunkManager.Instance.chunkSize.x; x++)
            {
                for (int z = 0; z < ChunkManager.Instance.chunkSize.z; z++)
                {
                    for (int y = 0; y < ChunkManager.Instance.chunkSize.y; y++)
                    {
                        Blocks[x, y, z] = BlockType.Air;
                    }
                }
            }
        }
    }
    public static ConcurrentStack<BlockType[,,]> BlocksPool = new ConcurrentStack<BlockType[,,]>();


    public static BlockType[,,] GetBlocks()
    {
        BlockType[,,] block;
        if (BlocksPool.Count > 0)
        {
            if (!BlocksPool.TryPop(out block))
            {
                block = new BlockType[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.y, ChunkManager.Instance.chunkSize.z];
           
           }
        }
        else
        {
            block = new BlockType[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.y, ChunkManager.Instance.chunkSize.z];
         
            //            Debug.Log("GetBlocks");

        }
      
        return block;
    }

    public static ConcurrentStack<List<WorldChunk>> chunkListPool = new ConcurrentStack<List<WorldChunk>>();



    public static List<WorldChunk> GetchunkList()
    {// return new List<WorldChunk>(64);
        List<WorldChunk> chunklist;
        if (chunkListPool.Count > 0)
        {
            if (!chunkListPool.TryPop(out chunklist))
            {
                chunklist = new List<WorldChunk>(64);

            }
        }
        else
        {
            chunklist = new List<WorldChunk>(64);

        }
        if (chunklist.Count > 0)
        {
            chunklist.Clear();
        }
        return chunklist;
    }

    public void GetNoiseDate()
    {
        if (noiseDate != null)
            return;
        noiseDate = GetNoiseDate(ID.x, ID.z);
    }


    public static NoiseDate GetNoiseDate(int x, int z)
    {
        Vector2Int mapid = new Vector2Int(x, z);
        NoiseDate noiseDateTemp;


        if (hightMapDic.ContainsKey(mapid))
        {
            if (!hightMapDic.TryGetValue(mapid, out noiseDateTemp))
            {
                if (PerlinDatePool.Count > 0)
                {

                    PerlinDatePool.TryDequeue(out noiseDateTemp);


                }
                else
                {
                    noiseDateTemp = new NoiseDate();


                }
                if (noiseDateTemp == null)
                    noiseDateTemp = new NoiseDate();

                noiseDateTemp.NoiseCompute(x, z);
                // WorldChunk.hightMapDic.TryAdd(mapid, pnpc);

            }
        }
        else
        {
            if (PerlinDatePool.Count > 0)
            {

                PerlinDatePool.TryDequeue(out noiseDateTemp);


            }
            else
            {
                noiseDateTemp = new NoiseDate();


            }
            if (noiseDateTemp == null)
                noiseDateTemp = new NoiseDate();
                noiseDateTemp.addTime=Time.realtimeSinceStartup;
                noiseDateTemp.mapid=mapid;
            WorldChunk.hightMapDic.TryAdd(mapid, noiseDateTemp);
            noiseDateTemp.NoiseCompute(x, z);

        }


        return noiseDateTemp;

    }


}

