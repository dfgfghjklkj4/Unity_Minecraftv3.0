using System.Linq;
//using System.Numerics;
using System.Net.WebSockets;
using System.Xml;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using System.Threading;
using UnityEngine.Profiling;
using System.Threading.Tasks;
using System.Collections.Concurrent;


public class PosChange
{

    public Vector3Int preChunkPos;
    public Vector3Int currentChunkPos;
}
public class ChunkManager : MonoBehaviour
{
    public static System.Random rd = new System.Random();
    public int c4, c5;
    public int chunkCount;
    public AirChunk airChunk;
    public BedrockChunk bedrockChunk;
    public static float groundHight;
    public static float maxHight = 180;///82


    public bool useRandomSeed;
    public int seed;

    public Transform player;
    public Vector3Int loadDistance;
    Vector3Int loadDistancebigone;//loadDistancebigone比加载范围（loadDistance）大一圈的是预加载做剔除判断
    public Vector3Int chunkSize;
    public Texture2D blockAtlas;
    public Texture2D blockAtlas_N;
    public Material Opaque, Water, Foliage, TreeMat, Opaquelod2, WaterLod2, Foliagelod2;
    public ChunkMeshDate ChunkPrefab;


    public static Dictionary<Vector3Int, WorldChunk> _chunks; // All chunks that have been initialized
    public static Dictionary<Vector3Int, WorldChunk> loadingChunks; // All chunks that have been load

    public static Vector3Int ChunkSizeStatic;

    public static ConcurrentQueue<WorldChunk> LoadChunkQueue = new ConcurrentQueue<WorldChunk>();


    public static ChunkManager Instance;
    public bool loading;
    public Transform waterPlan;



    public bool b1, b2;

    private void Awake()

    {
        // Accelerometer Frequency
        Instance = this;
        MeshData_.InitCacah();

        BlockType.LoadBlockTypes();
        airChunk.Awake0();
        bedrockChunk.Awake0();
        WorldChunk.blockCountPerChunk = chunkSize.x * chunkSize.y * chunkSize.z;
        WorldChunk.NeiBlocks = new NeighborBlocks[6];
        for (int i = 0; i < WorldChunk.NeiBlocks.Length; i++)
        {
            WorldChunk.NeiBlocks[i] = new NeighborBlocks();
        }

        WorldChunk.bedrockBlocks = new BlockType[chunkSize.x, chunkSize.y, chunkSize.z];
        WorldChunk.StoneBlocks = new BlockType[chunkSize.x, chunkSize.y, chunkSize.z];


        WorldChunk.AirBlocks = new BlockType[chunkSize.x, chunkSize.y, chunkSize.z];

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int z = 0; z < chunkSize.z; z++)
            {

                for (int y = 0; y < chunkSize.y; y++)
                {
                    WorldChunk.bedrockBlocks[x, y, z] = BlockType.Bedrock;
                    WorldChunk.AirBlocks[x, y, z] = BlockType.Air;
                    WorldChunk.StoneBlocks[x, y, z] = BlockType.Stone;
                }
            }
        }




        ChunkSizeStatic = chunkSize;
        WorldChunk._size = ChunkSizeStatic;
        int lw = (loadDistance.x * 2 * 2) * (loadDistance.z * 2 * 2);


        for (int i = 0; i < 1024; i++)
        {
            var b = new BlockType[chunkSize.x, chunkSize.y, chunkSize.z];

            WorldChunk.BlocksPool.Push(b);
        }

        WorldChunk._sizeSmallOne = new Vector3Int(chunkSize.x - 1, chunkSize.y - 1, chunkSize.z - 1);
        WorldChunk._size = chunkSize;
        WorldChunk.boundCenter = new Vector3(chunkSize.x / 2 - 0.5f, chunkSize.y / 2 - 0.5f, chunkSize.z / 2 - 0.5f);
        WorldChunk.bound = new Bounds(WorldChunk.boundCenter, chunkSize);




    }

    private void OnDestroy()
    {

        WorldChunk.pool.Clear();
        WorldChunk.pool = null;

        WorldChunk.BlocksPool.Clear();
        WorldChunk.BlocksPool = null;

        WorldChunk.PerlinDatePool.Clear();
        WorldChunk.PerlinDatePool = null;

        // WorldChunk.PerlinDatePool.Clear();
        //   WorldChunk.PerlinDatePool=null;

        WorldChunk.chunkListPool.Clear();
        WorldChunk.chunkListPool = null;

        WorldChunk.hightMapDic.Clear();
        WorldChunk.hightMapDic = null;

        ChunkMeshDate.pool.Clear();
        ChunkMeshDate.pool = null;


        Resources.UnloadUnusedAssets();

        GC.Collect();
        Debug.Log(1);
    }
    private void Start()
    {

        loadDistancebigone = new Vector3Int(loadDistance.x + 1, loadDistance.y + 1, loadDistance.z + 1);

        Initialize();
        currentChunkPos = GetNearestChunkPosition(GetPlayerPosition());
        preChunkPos = currentChunkPos;
    }

    //  public WorldChunk preChunk, currentChunk;
    public Vector3Int preChunkPos;
    public Vector3Int currentChunkPos;

    public static bool ChangeChunk;

    public static ConcurrentQueue<Action> loadEventQueueFinish = new ConcurrentQueue<Action>(); //消息队列
    public static Queue<PosChange> PosQueue = new Queue<PosChange>(); //玩家所在chunk变化更新新的chunk
    public int cccccc;
    private void Update()
    {

        if (LoadChunkQueue.Count > 0)
        {
            Queue<WorldChunk> LoadChunkQueuetemp = new Queue<WorldChunk>();
            int c = 0;
            // lock (loadEventQueue)
            //  {


            foreach (var item in LoadChunkQueue)
            {
                WorldChunk act;
                LoadChunkQueue.TryDequeue(out act);
                LoadChunkQueuetemp.Enqueue(act);
                c++;
                if (c > 2)
                {
                    c = 0;
                    //  break;

                }
            }
            //  }
            while (LoadChunkQueuetemp.Count > 0)
            {
                // Action act;
                WorldChunk chunk = LoadChunkQueuetemp.Dequeue(); //在主线程里处理子线程抛过来的消息，当然也可以根据需要使用其他方法处理，例如协程？FixUpdate？或者线程同步信号之类的
                                                                 //    Debug.Log(9898989888);
                if (chunk != null)
                {
                    chunk.BuildMeshInMainThread();

                }
            }

        }



        else
        {
            if (loadEventQueueFinish.Count > 0)
            {

                while (loadEventQueueFinish.Count > 0)
                {
                    Action act;
                    loadEventQueueFinish.TryDequeue(out act); //在主线程里处理子线程抛过来的消息，当然也可以根据需要使用其他方法处理，例如协程？FixUpdate？或者线程同步信号之类的
                                                              //    Debug.Log(9898989888);
                    if (act != null)
                    {

                        act();

                    }
                }



            }


            if (PosQueue.Count > 0 && !loading && loadEventQueueFinish.Count == 0)
            {

                UpdateLoadedChunks(PosQueue.Dequeue());

            }
        }





        chunkCount = _chunks.Count;

        if (ChangeChunk)
            ChangeChunk = false;


        currentChunkPos = GetNearestChunkPosition(GetPlayerPosition());
        //if (LoadChunkQueue.Count > 0)
        //   {
        //     LoadChunksYield();

        //   }



    }
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            foreach (var item in lg)
            {
                Destroy(item);
            }
            lg.Clear();
        }
        if (currentChunkPos != preChunkPos)
        {
            waterPlan.position = new Vector3(player.position.x, waterPlan.position.y, player.position.z);
            int distanceX = Mathf.Abs(currentChunkPos.x - preChunkPos.x);//X z要一样大
            int distanceY = Mathf.Abs(currentChunkPos.y - preChunkPos.y);
            int distanceZ = Mathf.Abs(currentChunkPos.z - preChunkPos.z);
            if (distanceX > chunkSize.x || distanceZ > chunkSize.z || distanceY > chunkSize.y)
            {
                if (distanceX <= chunkSize.x * 2 || distanceZ <= chunkSize.z * 2 || distanceY <= chunkSize.y * 2)//中间跳过了一个地形块
                {
                    Vector3Int v = (currentChunkPos + preChunkPos) / 2;
                    Debug.Log(currentChunkPos + "  " + preChunkPos + "   v" + v + "  " + (currentChunkPos.z - preChunkPos.z));

                    PosChange pc0 = new PosChange();
                    pc0.currentChunkPos = v;
                    pc0.preChunkPos = preChunkPos;
                    PosQueue.Enqueue(pc0);

                    PosChange pc2 = new PosChange();
                    pc2.currentChunkPos = currentChunkPos;
                    pc2.preChunkPos = v;
                    PosQueue.Enqueue(pc2);
                }
                else
                {
                    //全部从新加载
                    Debug.Log("全部从新加载");
                }

            }
            else
            {
                PosChange pc = new PosChange();
                pc.currentChunkPos = currentChunkPos;
                pc.preChunkPos = preChunkPos;
                PosQueue.Enqueue(pc);
            }
            ChangeChunk = true;



            if (!loading && LoadChunkQueue.Count == 0 && loadEventQueueFinish.Count == 0)
            {
                // ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateLoadedChunks), pc);
                UpdateLoadedChunks(PosQueue.Dequeue());
                // LoadChunksYield(loadChunks, chunks3);
            }




        }
        ///////////////////////////////////
        if (destroyChunks.Count > 0 || addChunks.Count > 0)
        {
            ModifyAndUpdateBlocks();
        }





        if (currentChunkPos != preChunkPos)
        {

            preChunkPos = currentChunkPos;
        }








    }



    List<NoiseDate> noiseReturnPool = new List<NoiseDate>(128);
    private void FixedUpdate()
    {

        //  return;









        int c = WorldChunk.hightMapDic.Count - 512;


        if (c > 52 && LoadChunkQueue.Count == 0)
        {

            //List<NoiseDate> values;
            noiseReturnPool = WorldChunk.hightMapDic.Values.ToList();

            Debug.Log(c + " ccc  " + WorldChunk.hightMapDic.Count);
            noiseReturnPool.Sort((a, b) => a.addTime.CompareTo(b.addTime));
            Vector2 pos = new Vector2(player.position.x, player.position.z);
            // Vector2Int pos=new Vector2Int(currentChunkPos.x,currentChunkPos.z);
            int cc = 0;
            foreach (var item in noiseReturnPool)
            {
                Vector2Int key = item.mapid;
                // float dis = Vector2.Distance(key, pos);
                //  if (dis>500)
                //  {
                var p = WorldChunk.hightMapDic[key];
                WorldChunk.PerlinDatePool.Enqueue(p);
                WorldChunk.hightMapDic.Remove(key);
                //  GameObject go = Instantiate(ggg, new Vector3(key.x, 50, key.y), Quaternion.identity);
                // lg.Add(go);
                cc++;
                if (cc == c)
                {
                    break;
                }
                //  }

            }
        }
        if (noiseReturnPool.Count > 0)
        {

        }
    }


    private void OnDisable()
    {
        MeshData_.triangles.Dispose();
        GC.Collect();
    }
    public static Random.State prevState;
    public static Vector2Int offset;
    public void Initialize()
    {


        var v = GetPlayerPosition();

        currentChunkPos = GetNearestChunkPosition(v);




        if (useRandomSeed)
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
        prevState = Random.state;
        UnityEngine.Random.InitState(seed);
        offset = new Vector2Int(Random.Range(-100, 100), Random.Range(-100, 100));

        _chunks = new Dictionary<Vector3Int, WorldChunk>(2048); // All chunks that have been initialized
        loadingChunks = new Dictionary<Vector3Int, WorldChunk>(2048); // All chunks that have been load



        TouchedChunks = new HashSet<WorldChunk>();

        addChunks = new HashSet<WorldChunk>();

        modifyChunks = new HashSet<WorldChunk>();

        destroyChunks = new HashSet<WorldChunk>();

        newChunks = new HashSet<WorldChunk>();

        preChunkPos = Vector3Int.one * int.MaxValue;

        var loadPos = GetInRangeChunkPositions(currentChunkPos, loadDistance);

        LoadChunksImmediately(loadPos);

    }
    public int totolVc;

    private void LoadChunksImmediately(List<Vector3Int> loadPos)
    {
        List<WorldChunk> chunks = new List<WorldChunk>();
        MeshData_.tiem = 0;
        int c = loadDistance.x * loadDistance.z * loadDistance.y;
        int cc = 0;
        for (int i = 0; i < 256; i++)
        {

            WorldChunk chunk = new WorldChunk();
            chunk.hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];

            WorldChunk.pool.Push(chunk);


        }

        float f = Time.realtimeSinceStartup;
        foreach (var item in loadPos)


        {
            Vector3Int chunkID = item;

            WorldChunk chunk;
            if (_chunks.TryGetValue(chunkID, out chunk))
            {

            }
            else
            {

                chunk = GetChunk(chunkID);
                chunk.GetNoiseDate();
                chunks.Add(chunk);
                //  chunk = WorldChunk.GetChunk();
            }



        }

        HashSet<WorldChunk> chunks3 = new HashSet<WorldChunk>();

        foreach (var item in chunks)
        {
            item.InitializeNeighbors();
            item.CreateTerrainDate();


            if (item.isAir || item.isBedrockC || item.isEmperty || item.shareBlocks)
            {

                continue;
            }


            item.StartBuildMesh();


            if (LoadChunkQueue.Count > 0)
            {

                while (LoadChunkQueue.Count > 0)
                {
                    WorldChunk chunk;
                    // Action act;
                    LoadChunkQueue.TryDequeue(out chunk); //在主线程里处理子线程抛过来的消息，当然也可以根据需要使用其他方法处理，例如协程？FixUpdate？或者线程同步信号之类的
                                                          //    Debug.Log(9898989888);
                    if (chunk != null)
                    {
                        //GameObject go = Instantiate(gp, item.ID, Quaternion.identity);
                        chunk.BuildMeshInMainThread();

                    }
                }



            }

        }





        loadPos.Clear();
        chunks.Clear();
        f = Time.realtimeSinceStartup - f;
        // PlayerController.Instance.tm.text = f.ToString();
        Debug.Log(f + "   总时间  " + loadingChunks.Count + " vc " + totolVc + "   " + ttt2);
        //  Debug.Log(MeshData_.tiem);

    }





    //  private void LoadChunksYield(List<WorldChunk> chunks, HashSet<WorldChunk> chunks3)
    private void LoadChunksYield(List<WorldChunk> chunks, HashSet<WorldChunk> chunks3)
    {





        if (loading)
        {
            Debug.Log("loading");
        }
        loading = true;
        //  List<WorldChunk> chunks = (List<WorldChunk>)obj;
        var chunks2 = WorldChunk.GetchunkList();
        List<Task> tasks = new List<Task>();

        int cc = 0;


        int cb = 0;
        tasks.Clear();
        foreach (var item in chunks3)
        {
            //  item.flag = LoadFlag.waitLoad;
            if (item.buildMesh || item.isAir || item.isBedrockC || item.shareBlocks)




                continue;

            if (!item.computedTerrainDate && !item.creating)
            {
                cb++;
                Task task2 = Task.Factory.StartNew(() =>
                         {

                             item.CreateTerrainDate();

                         });
                tasks.Add(task2);
                continue;
                chunks2.Add(item);
            }
            continue;
            cc++;
            if (cc > 4)
            {

                Task task2 = Task.Factory.StartNew(() =>
                       {
                           // LoadNextChunk3(chunks2);
                           foreach (var chunk in chunks2)
                           {

                               chunk.CreateTerrainDate();
                           }
                       });
                tasks.Add(task2);
                chunks2 = null;
                // ThreadPool.QueueUserWorkItem(new WaitCallback(LoadNextChunk2), chunks2);
                chunks2 = WorldChunk.GetchunkList();
                // chunks2 = new List<WorldChunk>();
                chunks2.Clear();
                cc = 0;
            }
        }
        /*
          Task task3 = Task.Factory.StartNew(() =>
                                   {
                                       // LoadNextChunk3(chunks2);
                                       foreach (var chunk in chunks2)
                                       {
                                           chunk.CreateTerrainDate();
                                       }
                                   });

        tasks.Add(task3);
        */


        //Debug.Log(cb+"  chunks "+chunks.Count);
        Task.WhenAll(tasks).ContinueWith((t) =>
     {
         tasks.Clear();
         //   Debug.Log(456789 + "   " + chunks.Count);
         cc = 0;
         chunks2 = new List<WorldChunk>();
         HashSet<List<WorldChunk>> ll = new HashSet<List<WorldChunk>>();
         foreach (var item in chunks)
         {
             //  if (item.buildMesh)
             // {
             //   Debug.Log("item.buildMesh");
             //  continue;
             // }
             //    if (item.isAir || item.isBedrockC || item.shareBlocks)
             //    {
             //      continue;
             //   }

             // Task task01 =  Task.Factory.StartNew(() =>
             //         {

             //       item.StartBuildMesh();

             //   });
             Task task01 = Task.Factory.StartNew(() =>
                                 {

                                     item.StartBuildMesh();

                                 });
             tasks.Add(task01);
         }




         Task.WhenAll(tasks).ContinueWith((t) =>
                            {
                                // tasks.Clear();

                                loadEventQueueFinish.Enqueue(() =>    //计算完毕将结果通过委托压入消息队列等待主线程执行
                                {




                                    foreach (var item in chunks3)
                                    {
                                        // if (item.meshDate != null)
                                        // if (true)

                                        //  {
                                        //    item.meshDate.RetrunPool();
                                        //  item.meshDate = null;
                                        //  }


                                    }
                                    foreach (var item in chunks)
                                    {
                                        item.NeighborBack.useing = true;
                                        item.NeighborDown.useing = false;
                                        item.NeighborForward.useing = false;
                                        item.NeighborLeft.useing = false;
                                        item.NeighborRight.useing = false;
                                        item.NeighborUp.useing = false;
                                    }
                                    //foreach (var item in ll)
                                    //{
                                    //   WorldChunk.chunkListPool.Push(item);
                                    // }

                                    chunks.Clear();
                                    WorldChunk.chunkListPool.Push(chunks);
                                    loading = false;
                                }
                                 );


                            });


         // ThreadPool.QueueUserWorkItem(new WaitCallback(LoadNextChunk2), chunks2);
     });




    }




    public float ttt;
    public float ttt2;
    public int cc0;
    /// <summary>
    /// (105, 69, -111)
    /// Load the next chunk in the chunk load queue.
    /// </summary>
    public static WorldChunk GetChunk(Vector3Int chunkID)
    {
        WorldChunk chunk;
        try
        {


            if (_chunks.ContainsKey(chunkID))
            {
            a: if (!_chunks.TryGetValue(chunkID, out chunk))
                {

                    goto a;

                }
            }
            else
            {
                if (chunkID.y > ChunkManager.maxHight)
                {
                    chunk = Instance.airChunk.chunk;

                }
                else if (chunkID.y < -59)
                {
                    chunk = Instance.bedrockChunk.chunk;
                }
                else
                {

                    chunk = WorldChunk.GetChunk(chunkID);


                }
            }

            //  b : if(!_chunks.TryAdd(chunkID,chunk)) 
            {
                //goto b;
            }


            return chunk;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);

            chunk = WorldChunk.GetChunk(chunkID);
            return chunk;
            // We'll never get here!

        }




    }

    //   public static CustomWorldChunkComparer cwc = new CustomWorldChunkComparer();
    public static HashSet<WorldChunk> TouchedChunks;
    public static HashSet<WorldChunk> addChunks;
    public static HashSet<WorldChunk> modifyChunks;
    public static HashSet<WorldChunk> destroyChunks;
    public static HashSet<WorldChunk> newChunks;


    public List<GameObject> lg = new List<GameObject>();
 

    //当前更新只支持每帧只能移动一个chunk大小的距离 
    //暂时都放在一帧里处理
    public void UpdateLoadedChunks(object obj)
    {
        rd = new System.Random(Guid.NewGuid().GetHashCode());
        PosChange pc = (PosChange)obj;

        var loadChunks = WorldChunk.GetchunkList();
        var unloadChunks = WorldChunk.GetchunkList();

        int c = 0;
        //
        //    print(preChunkPos+"  " + currentChunkPos);
        for (int dx = -loadDistancebigone.x; dx <= loadDistancebigone.x; dx += 1)
        {
            for (int dz = -loadDistancebigone.z; dz <= loadDistancebigone.z; dz += 1)

            {
                //最外面的一圈
                if (dx == -loadDistancebigone.x || dx == loadDistancebigone.x || dz == -loadDistancebigone.z || dz == loadDistancebigone.z)
                {
                    for (int dy = loadDistancebigone.y; dy >= -loadDistancebigone.y; dy -= 1)
                    {

                        float y = dy * chunkSize.y;
                        if (y > maxHight || y < -59)
                        {
                            continue;
                        }
                        Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        Vector3Int pos = pc.preChunkPos + Vector3Int.RoundToInt(offset);

                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {



                            unloadChunks.Add(chunk);
                            chunk.unload = true;


                        }

                    }

                }//关掉变化前位置的chunk lod设置（离相机最近3圈chunk）
                else if ((Mathf.Abs(dx) == 3 && Mathf.Abs(dz) <= 3) || (Mathf.Abs(dz) == 3 && Mathf.Abs(dx) <= 3))

                {

                    /*
               for (int dy = loadDistance.y; dy >= -loadDistance.y; dy -= 1)
                    {
                        float y = dy * chunkSize.y;
                        if (y > maxHight || y < -59)
                        {
                            continue;
                        }
                        Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        Vector3Int pos = pc.preChunkPos + Vector3Int.RoundToInt(offset);

                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {

                            // chunk.LOD = 1;
                            if (chunk.meshDate != null)
                            {
                                if (chunk.meshDate.OpaqueMeshFilter.sharedMesh.vertexCount > 0 && chunk.meshDate.OpaqueMeshCol.enabled)
                                {
                                    //   chunk.OpaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                                    //   chunk.OpaqueMeshRenderer.receiveShadows = false;
                                    //   chunk.OpaqueMeshCol.enabled = false;
                                    //   chunk.OpaqueMeshRenderer.sharedMaterial = Opaquelod2;
                                }

                                if (chunk.meshDate.FoliageMeshFilter.sharedMesh.vertexCount > 0 && chunk.meshDate.FoliageMeshCol.gameObject.activeSelf)
                                {
                                    //  chunk.FoliageMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                                    //  chunk.FoliageMeshRenderer.receiveShadows = false;
                                    // chunk.FoliageMeshRenderer.sharedMaterial = Foliagelod2;
                                    // chunk.FoliageMeshCol.enabled = false;
                                    //    chunk.FoliageMeshCol.gameObject.SetActive(false);
                                }
                                if (chunk.meshDate.WaterMeshFilter.sharedMesh.vertexCount > 0 && chunk.meshDate.WaterMeshCol.enabled)
                                {

                                    chunk.meshDate.WaterMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                                    chunk.meshDate.WaterMeshRenderer.receiveShadows = false;
                                    chunk.meshDate.WaterMeshRenderer.sharedMaterial = WaterLod2;
                                    chunk.meshDate.WaterMeshCol.enabled = false;
                                }
                            }





                        }


                    }
                    */


                }
            }
        }




        ////////////////////////////////////////////////////////////////////
        for (int dx = -loadDistancebigone.x; dx <= loadDistancebigone.x; dx += 1)
        {
            for (int dz = -loadDistancebigone.z; dz <= loadDistancebigone.z; dz += 1)

            {


                for (int dy = loadDistance.y; dy >= -loadDistance.y; dy -= 1)
                {
                    float y = dy * chunkSize.y;
                    if (y > maxHight || y < -59)
                    {
                        continue;
                    }
                    Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                    Vector3Int pos = pc.currentChunkPos + Vector3Int.RoundToInt(offset);


                    NoiseDate nd = WorldChunk.GetNoiseDate(pos.x, pos.z);
                    if (dx == -loadDistancebigone.x || dx == loadDistancebigone.x || dz == -loadDistancebigone.z || dz == loadDistancebigone.z)
                    {


                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {
                            if (chunk.noiseDate == null)
                                chunk.noiseDate = nd;
                            //这里大多是已经加载了地形数据的邻居块
                            if (chunk.unload)
                                chunk.unload = false;
                            if (unloadChunks.Contains(chunk))
                            {

                                unloadChunks.Remove(chunk);

                            }
                            // 

                        }




                    }

                    //以当前chunk位置为中心 加载到最外边的chunk也就是变化的chunk（规定一帧最多加载一个chunk 所以变化的也就是最外边的一圈chunk）
                    else if (dx == -loadDistance.x || dx == loadDistance.x || dz == -loadDistance.z || dz == loadDistance.z)


                    {

                        //  float y = dy * chunkSize.y;

                        // Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        //  Vector3Int pos = pc.currentChunkPos + Vector3Int.RoundToInt(offset);
                        // NoiseDate nd = WorldChunk.GetNoiseDate(pos.x, pos.z);

                        WorldChunk chunk = LoadSingleChunk(pos, nd);
                        if (chunk != null)
                        {
                            if (chunk.unload)
                                chunk.unload = false;
                            if (unloadChunks.Contains(chunk))
                            {
                                unloadChunks.Remove(chunk);
                            }
                            loadChunks.Add(chunk);
                        }

                    }


                    //离相机最近的3圈chunk
                    else if (Mathf.Abs(dx) <= 2 && Mathf.Abs(dz) <= 2)
                    {
                        /*
                         // for (int dy = loadDistance.y; dy >= -loadDistance.y; dy -= 1)
                                                {
                                                    //  float y = dy * chunkSize.y;

                                                    Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                                                    Vector3Int pos = pc.currentChunkPos + Vector3Int.RoundToInt(offset);
                                                    //  GameObject.Instantiate(red, pos, Quaternion.identity);

                                                    if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                                                    {
                                                        if (chunk.IsLoaded && chunk.meshDate != null)
                                                        {
                                                            // chunk.LOD = 0;





                                                            if (chunk.meshDate.OpaqueMeshFilter.sharedMesh.vertexCount > 0 && !chunk.meshDate.OpaqueMeshCol.enabled)
                                                            {
                                                                // chunk.OpaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
                                                                // chunk.OpaqueMeshRenderer.receiveShadows = true;
                                                                // chunk.OpaqueMeshCol.enabled = true;
                                                                // chunk.OpaqueMeshRenderer.sharedMaterial = Opaque;
                                                            }

                                                            if (chunk.meshDate.FoliageMeshFilter.sharedMesh.vertexCount > 0 && !chunk.meshDate.FoliageMeshCol.gameObject.activeSelf)
                                                            {
                                                                // chunk.FoliageMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
                                                                //  chunk.FoliageMeshRenderer.receiveShadows = true;
                                                                //  chunk.FoliageMeshCol.enabled = true;
                                                                //  chunk.FoliageMeshRenderer.sharedMaterial = Foliage;
                                                                //   chunk.FoliageMeshCol.gameObject.SetActive(true);
                                                            }
                                                            if (chunk.meshDate.WaterMeshFilter.sharedMesh.vertexCount > 0 && !chunk.meshDate.WaterMeshCol.enabled)
                                                            {

                                                                // chunk.WaterMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
                                                                chunk.meshDate.WaterMeshRenderer.receiveShadows = true;
                                                                chunk.meshDate.WaterMeshCol.enabled = true;
                                                                chunk.meshDate.WaterMeshRenderer.sharedMaterial = Water;
                                                            }




                                                        }
                                                    }
                                                }
                        */


                        if (dy == -loadDistance.y || dy == loadDistance.y)
                        {

                            WorldChunk chunk = LoadSingleChunk(pos, nd);
                            if (chunk != null)
                            {
                                if (chunk.unload)
                                    chunk.unload = false;
                                if (unloadChunks.Contains(chunk))
                                {
                                    unloadChunks.Remove(chunk);
                                }
                                loadChunks.Add(chunk);
                            }
                        }
                    }

                }


            }
        }



        if (loadChunks.Count > 0)
        {

            HashSet<WorldChunk> chunks3 = new HashSet<WorldChunk>(128);
            for (int i = 0; i < loadChunks.Count; i++)
            {
                var item = loadChunks[i];



                item.InitializeNeighbors();


                if (item.NeighborUp == null)
                {
                    Debug.Log("XXXXXXXXXXXXXXXXXX");
                    var wp = new Vector3Int(item.ID.x, item.ID.y + ChunkManager.Instance.chunkSize.y, item.ID.z);

                    item.NeighborUp = ChunkManager.GetChunk(wp);

                    //   NeighborDown.Initialize(wp);
                    item.NeighborUp.NeighborDown = item;

                }
                //  NeighborDown.GetNoiseDate();



                if (item.NeighborBack.unload)
                {
                    item.NeighborBack.unload = false;
                    if (unloadChunks.Contains(item.NeighborBack))
                    {
                        unloadChunks.Remove(item.NeighborBack);
                    }

                }
                if (item.NeighborDown.unload)
                {
                    item.NeighborDown.unload = false;
                    if (unloadChunks.Contains(item.NeighborDown))
                    {
                        unloadChunks.Remove(item.NeighborDown);
                    }

                }
                if (item.NeighborForward.unload)
                {
                    item.NeighborForward.unload = false;
                    if (unloadChunks.Contains(item.NeighborForward))
                    {
                        unloadChunks.Remove(item.NeighborForward);
                    }

                }
                if (item.NeighborLeft.unload)
                {
                    item.NeighborLeft.unload = false;
                    if (unloadChunks.Contains(item.NeighborLeft))
                    {
                        unloadChunks.Remove(item.NeighborLeft);
                    }

                }
                if (item.NeighborRight.unload)
                {
                    item.NeighborRight.unload = false;
                    if (unloadChunks.Contains(item.NeighborRight))
                    {
                        unloadChunks.Remove(item.NeighborRight);
                    }

                }
                if (item.NeighborUp.unload)
                {
                    item.NeighborUp.unload = false;
                    if (unloadChunks.Contains(item.NeighborUp))
                    {
                        unloadChunks.Remove(item.NeighborUp);
                    }

                }
                chunks3.Add(item);


                item.NeighborBack.useing = true;
                item.NeighborDown.useing = true;
                item.NeighborForward.useing = true;
                item.NeighborLeft.useing = true;
                item.NeighborRight.useing = true;
                item.NeighborUp.useing = true;

                chunks3.Add(item.NeighborBack);
                chunks3.Add(item.NeighborDown);
                chunks3.Add(item.NeighborForward);
                chunks3.Add(item.NeighborLeft);
                chunks3.Add(item.NeighborRight);
                chunks3.Add(item.NeighborUp);
            }


            Task task = Task.Factory.StartNew(() =>
              {

                  foreach (var item in chunks3)
                  {

                      if (item.unload)
                      {
                          item.unload = false;

                      }


                      if (item.noiseDate != null)
                      {
                          continue;
                      }
                      Vector2Int noiseID = new Vector2Int(item.ID.x, item.ID.z);
                      if (WorldChunk.hightMapDic.ContainsKey(noiseID))
                      {
                          item.noiseDate = WorldChunk.hightMapDic[noiseID];
                          continue;
                      }

                      //   Task task2 = Task.Factory.StartNew(() =>
                      //   {
                      item.GetNoiseDate();

                      //  });

                      //tasks.Add(task2);
                  }


                  int p = 0;

                  LoadChunksYield(loadChunks, chunks3);
                  //  }





              }

            );
        }
        if (unloadChunks.Count > 0)
        {

            foreach (var item in unloadChunks)
            {

                item.UnUseSet();
            }
        }
        WorldChunk.chunkListPool.Push(unloadChunks);






    }


    public WorldChunk LoadSingleChunk(Vector3Int pos, NoiseDate nd)
    {
        WorldChunk chunk;
        if (_chunks.TryGetValue(pos, out chunk))
        {//这里大多是已经加载了地形数据的邻居块

            if (chunk.buildMesh)
                return null;

            // else
            {
                if (chunk.noiseDate == null)
                    chunk.noiseDate = nd;


                chunk.CreateTerrainDatePre();
                // {

                if (!chunk.isAir && !chunk.isBedrockC && !chunk.isEmperty && !chunk.shareBlocks)

                {


                    return chunk;

                }

            }

        }
        else
        {

            //新加载的chunk
            // var newChunk  = WorldChunk.GetChunk();
            var newChunk = GetChunk(pos);
            if (newChunk.noiseDate == null)
                newChunk.noiseDate = nd;

            newChunk.CreateTerrainDatePre();
            //  {
            // newChunk.CreateTerrainDate();
            // newChunk.pnpc = nd;
            if (!newChunk.isAir && !newChunk.isBedrockC && !newChunk.isEmperty && !newChunk.shareBlocks)
            {

                return newChunk;


            }






        }
        return null;

    }


    private List<Vector3Int> GetInRangeChunkPositions(Vector3Int centerChunkPos, Vector3Int radius)
    {
        List<Vector3Int> loadPos = new List<Vector3Int>(1024);
        // float maxDistSqrd = Mathf.Pow(radius.x * chunkSize.x, 2f);

        for (int dx = -radius.x; dx <= radius.x; dx += 1)
        {
            for (int dz = -radius.z; dz <= radius.z; dz += 1)

            {
                // for (int dy = -radius.y; dy <= radius.y; dy += 1)
                //从上向下加载
                for (int dy = radius.y; dy >= -radius.y; dy -= 1)
                {
                    if (dy * chunkSize.y < -49)
                    {
                        //  print(dy * chunkSize.y + centerChunkPos.y);
                        break;
                    }
                    if (dy * chunkSize.y + centerChunkPos.y > maxHight)
                    {
                        continue;
                    }
                    Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                    Vector3Int pos = centerChunkPos + Vector3Int.RoundToInt(offset);

                    if (!_chunks.ContainsKey(pos))
                    {
                        loadPos.Add(pos);
                    }
                    else
                    {
                        WorldChunk chunk = _chunks[pos];
                        if (!chunk.buildMesh)
                        {
                            loadPos.Add(pos);
                        }

                    }
                }
            }
        }
        return loadPos;
    }




    static Vector3Int offset2 = new Vector3Int(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2);
    public static Vector3Int GetNearestChunkPosition(Vector3Int pos)
    {
        //   Vector3Int offset = new Vector3Int(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2);
        pos += offset2;

        int xi = pos.x / ChunkSizeStatic.x;
        int yi = pos.y / ChunkSizeStatic.y;
        int zi = pos.z / ChunkSizeStatic.z;

        int x = xi * ChunkSizeStatic.x;
        int y = yi * ChunkSizeStatic.y;
        int z = zi * ChunkSizeStatic.z;

        return new Vector3Int(x, y, z) - offset2;
    }


    public static WorldChunk GetNearestChunk(Vector3Int pos)
    {
        Vector3Int nearestPos = GetNearestChunkPosition(pos);
        //  WorldChunk chunk;
        return GetChunk(nearestPos);


    }



    public BlockType GetBlockAtPosition(Vector3Int pos, ref WorldChunk chunk)
    {
        chunk = GetNearestChunk(pos);
        if (chunk == null)
        {
            Debug.LogError("加载了错误的chunk");
            return null;
        }
        return chunk.GetBlockAtWorldPosition(pos);
    }


    Vector3Int GetPlayerPosition()
    {
        Vector3Int pos = Vector3Int.CeilToInt(player.transform.position);
        if (pos.y > maxHight)
            pos.y = (int)maxHight;
        return pos;
    }
    public static Vector3Int GetPlayerPosition(Transform tf)
    {
        //   Vector3Int pos = Vector3Int.CeilToInt(tf.position);
        Vector3Int pos = Vector3Int.RoundToInt(tf.position);
        if (pos.y > maxHight)
            pos.y = (int)maxHight;
        return pos;
    }

    public List<Transform> trees;
    public List<Transform> trees2;
    public GameObject gp;

    public GameObject ggg;
    public GameObject ggg2;
    void ModifyAndUpdateBlocks()
    {
        foreach (var chunk in destroyChunks)
        {
            //  print(23);
            chunk.loadNeighborsBlocks();
            chunk.DesttroyBlocksFun();
            chunk.DestroyBlocks.Clear();

        }
        foreach (var chunk in TouchedChunks)
        {
            if (!chunk.isModify)
                chunk.isModify = true;
            if (chunk.buildMesh)
            {
                chunk.ReBuildMesh();
            }
            else
            {
                //   print(chunk.ID);
                chunk.CreateTerrainDate();
                chunk.ReBuildMesh();
            }

        }

        foreach (var chunk in addChunks)
        {
            if (chunk.buildMesh)
            {
                chunk.CreateTerrainDate();
                chunk.ReBuildMesh();
            }
            else
            {


                chunk.ReBuildMesh();
            }

        }
        destroyChunks.Clear();
        TouchedChunks.Clear();
        addChunks.Clear();
    }


}
