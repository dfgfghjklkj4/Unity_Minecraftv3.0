using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;


public enum BlockFlag{
    defailt,grassTop,hillTop,grass2water,grass2hill
}



public enum BlockNameEnum
{
    Air,
    Water,
    Bedrock,
    Grass,
    Grass2,
    Grass3,
    GrassHill,
    GrassEdge,
    Cobblestone,
    Daisy,
    Diamond_Ore,
    Dirt,
    Gravel,
    Iron_Ore,
    Orange_Tulip,
    Pink_Tulip,
    Plank,
    Red_Tulip,
    Sand,
 
    Sandstone,
    Stone,
    HillStone,
    Tall_Grass,
    Tall_Grass02,
    Tall_Grass03,
    Tall_Grass04,
    Tall_Grass05,
    Yellow_Flower,
    Tree1,
    treeTrunk01,treeTrunk02,treeTrunk03,
    leave01,leave02,leave03,
        Road,Road2,Road3,
}

[CreateAssetMenu(fileName = "New Block Type", menuName = "Block Type")]
public class BlockType : ScriptableObject
{


    public static Vector2[] CubeUv = new Vector2[]{ new Vector2(0,0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
    ,new Vector2(0,0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
           ,new Vector2(0,0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
             ,new Vector2(0,0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
            ,new Vector2(0,0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
             ,new Vector2(0,0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
    };

    //  public static Vector2[] CubeUv = new Vector2[]{ new Vector2(0,0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
    //  ,new Vector2(0,0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
    //        ,new Vector2(0,0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
    //       ,new Vector2(0,0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
    //       ,new Vector2(0,0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
    //       ,new Vector2(0,0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
    // };

    /*
     
     */
    public static BlockType Air;
      public static BlockType Stone;
       public static BlockType Sand;
    public static BlockType Water;
    public static BlockType Bedrock;
    public static BlockType Grass;
    public static BlockType Grass2;
    public static BlockType Grass3;
    public static BlockType GrassHill;
    public static BlockType GrassEdge;
      public static BlockType HillStone;
       public static BlockType Dirt;
         public static BlockType Plank;
       


  
    public static BlockType Tree1;


    /*------------------------ MEMBER ------------------------*/
    // Up, Down, Front, Back, Left, Right
    public BlockNameEnum blockName;
    public BlockFlag flag;

    // Position of each of the 6 faces in the block atlas.
    // Top, Bottom, Front, Back, Left, Right
    public bool siTrunk;
    public bool isLeave;
    //public Mesh mesh;
    public Vector2Int[] atlasPositions;
    public bool isTree;
    public bool isTransparent;
    public bool isPlant = false;
    public bool isBillboard = false;
    public bool affectedByGravity = false;
    public bool isSourceBlock = false;
    public bool isWater = false;
    public bool mustBeOnGrassBlock = false;
    public AudioClip digClip = null;
    public AudioClip[] stepClips;
    public ParticleSystem.MinMaxGradient breakParticleColors;
    public int blockBlastResistance = 0;
    public bool isAir = false;
    [HideInInspector]
    public bool side;
    // public Texture2D Up, Down, Front, Back, Left, Right;
    //   public Texture2D NUp, NDown, NFront, NBack, NLeft, NRight;
    //   public Texture2D MUp, MDown, MFront, MBack, MLeft, MRight;

    public Texture2D Up, Down, Side;
    public Texture2D Up_N, Down_N, Side_N;
    public Texture2D Up_M, Down_M, Side_M;
    
    /*------------------------ STATIC ------------------------*/

    // Maps the names of block types to their corresponding object.
    public static Dictionary<BlockNameEnum, BlockType> NameToBlockType = new Dictionary<BlockNameEnum, BlockType>();
    public static Dictionary<BlockNameEnum, Transform> NameToFullMeshDate = new Dictionary<BlockNameEnum, Transform>();
    public static Dictionary<Texture2D, Rect> RectDic = new Dictionary<Texture2D, Rect>();
    // [HideInInspector]
    // public Vector2[] uvs;


    [HideInInspector]
    public MeshData_ md;
    public static void LoadBlockTypes()
    {
        List<Texture2D> lt = new List<Texture2D>();
        Texture2D altas = new Texture2D(2048, 2048);
        altas.filterMode = FilterMode.Point;

        List<Texture2D> ltn = new List<Texture2D>();
  
        Texture2D altasn =  Texture2D. normalTexture;

        List<Texture2D> ltm = new List<Texture2D>();
        Texture2D altasm = new Texture2D(2048, 2048);
        altasm.filterMode = FilterMode.Bilinear;

        // Load all the BlockType assets from the Resources folder.
        BlockType[] typeArray = Resources.LoadAll<BlockType>("Block Types");
        bool[] visible = new bool[6];

        for (int i = 0; i < visible.Length; i++)
        {
            visible[i] = true;
        }

        for (int i = 0; i < typeArray.Length; i++)

        {

            BlockType type = typeArray[i];


            if (type.blockName==BlockNameEnum.Tall_Grass)
            {
                TallGrassBlocks.Add(type);
                TallGrassBlocks.Add(type);
                TallGrassBlocks.Add(type);
            }
            if (type.blockName == BlockNameEnum.Tall_Grass02)
            {
                TallGrassBlocks.Add(type);
                TallGrassBlocks.Add(type);
           
            }
            if (type.blockName == BlockNameEnum.Tall_Grass03)
            {
                TallGrassBlocks.Add(type);
                TallGrassBlocks.Add(type);

            }
            if (type.isPlant)
            {
                PlantBlocks.Add(type);
            }
            if (type.Up != null)
            {
                if (!lt.Contains(type.Up))
                {
                    lt.Add(type.Up);
                    if (type.Up_N != null)
                    {
                        if (!ltn.Contains(type.Up_N))
                        {
                            ltn.Add(type.Up_N);
                        }
                    }
                    else
                    {
                        type.Up_N = new Texture2D(type.Up.width, type.Up.height);
                        if (!ltn.Contains(type.Up_N))
                        {
                            ltn.Add(type.Up_N);
                        }
                    }

                }
            }

            if (type.Down != null)
            {
              
                if (!lt.Contains(type.Down))
                {
                    lt.Add(type.Down);
                    if (type.Down_N != null)
                    {
                        if (!ltn.Contains(type.Down_N))
                        {
                            ltn.Add(type.Down_N);
                        }
                    }
                    else
                    {
                        type.Down_N = new Texture2D(type.Down.width,    type.Down.height);
                       
                        if (!ltn.Contains(type.Down_N))
                        {
                            ltn.Add(type.Down_N);
                        }
                    }
                }
            }

            if (type.Side != null)
            {
               
                if (!lt.Contains(type.Side))
                {
                    lt.Add(type.Side);
                    if (type.Side_N != null)
                    {
                        if (!ltn.Contains(type.Side_N))
                        {
                            ltn.Add(type.Side_N);
                        }
                    }
                    else
                    {
                        type.Side_N = new Texture2D(type.Side.width, type.Side.height);

                        if (!ltn.Contains(type.Side_N))
                        {
                            ltn.Add(type.Side_N);
                        }
                    }
                }
            }
            /////////////////////////////////
       
            ////////////////////////////

        }
      
        // var rc = altas.PackTextures(lt.ToArray(), 0, 1024, true);
        var rc = altas.PackTextures(lt.ToArray(), 0);
         var rcn = altasn.PackTextures(ltn.ToArray(), 0);
        //   var rcm = altasm.PackTextures(ltm.ToArray(), 0, 1024, true);
        //  RectDic = new Dictionary<Texture2D, Rect>();
        for (int i = 0; i < lt.Count; i++)
        {
       if (!RectDic.ContainsKey(lt[i]))
            {
            RectDic.Add(lt[i], rc[i]);
            }

        }

        for (int i = 0; i < ltn.Count; i++)
        {
            if (!RectDic.ContainsKey(ltn[i]))
            {
                RectDic.Add(ltn[i], rcn[i]);
            }

        }
  

        Texture2D altas2 = new Texture2D(altas.width, altas.height);
        altas2.filterMode = FilterMode.Point;
        altas2.SetPixels32(altas.GetPixels32());
        altas2.Apply();

        Texture2D altasn2 = new Texture2D(altasn.width, altasn.height);
        altasn2.filterMode = FilterMode.Point;
        altasn2.SetPixels32(altasn.GetPixels32());
        altasn2.Apply();

        ChunkManager.Instance.blockAtlas = altas2;
        ChunkManager.Instance.blockAtlas_N = altasn;
   
        ChunkManager.Instance.Opaque.mainTexture = ChunkManager.Instance.blockAtlas;
    ChunkManager.Instance.Opaque.SetTexture("_BumpMap", altasn) ;
      ChunkManager.Instance.Opaque.SetFloat("_BumpScale", 1.2f) ;


        ChunkManager.Instance.Opaquelod2.mainTexture = ChunkManager.Instance.blockAtlas;
        ChunkManager.Instance.Opaquelod2.SetTexture("_BumpMap", altasn);
            ChunkManager.Instance.Opaquelod2.SetFloat("_BumpScale", 1.2f) ;


        ChunkManager.Instance.Foliage.mainTexture = ChunkManager.Instance.blockAtlas;
        ChunkManager.Instance.Foliagelod2.mainTexture = ChunkManager.Instance.blockAtlas;

        ChunkManager.Instance.TreeMat.mainTexture = ChunkManager.Instance.blockAtlas;
         // ChunkManager.Instance.Opaque.SetTexture("_BumpMap", altasn2) ;
        //   ChunkManager.Instance.Opaque.SetTexture("_MetallicGlossMap", altasm);
      //  AtlasReader ar = new AtlasReader(ChunkManager.Instance.blockAtlas, 8);
      //  Debug.Log(lt.Count + " ** " + rc.Length);
        for (int i = 0; i < typeArray.Length; i++)

        {
            BlockType type = typeArray[i];
          

            // Debug.Log(type.name);
            if (NameToBlockType.ContainsKey(type.blockName))
            {
                Debug.LogError(type.blockName + "  加载错误 ");
            }
            else
            {

                type.md = MeshData_.Get24();
                NameToBlockType.Add(type.blockName, type);


                for (int u = 0; u < 24; u++)
                {
                    type.md.vertexDate[u].uv = CubeUv[u];
                }

                var index = 0;
                type.GenerateMeshInit(visible, ref index);
               

            }

        }

        Air = NameToBlockType[BlockNameEnum.Air];
        Water = NameToBlockType[BlockNameEnum.Water];

        Bedrock = NameToBlockType[BlockNameEnum.Bedrock];
   Stone = NameToBlockType[BlockNameEnum.Stone];
    Sand = NameToBlockType[BlockNameEnum.Sand];
        Grass = NameToBlockType[BlockNameEnum.Grass];
        Grass2 = NameToBlockType[BlockNameEnum.Grass2];
        Grass3 = NameToBlockType[BlockNameEnum.Grass3];
        GrassHill = NameToBlockType[BlockNameEnum.GrassHill];
        GrassEdge = NameToBlockType[BlockNameEnum.GrassEdge];
      
        Dirt = NameToBlockType[BlockNameEnum.Dirt];
       
        HillStone = NameToBlockType[BlockNameEnum.HillStone];

      Plank = NameToBlockType[BlockNameEnum.Plank];
        Tree1 = NameToBlockType[BlockNameEnum.Tree1];



    }

    public static void SaveTextureToFile(string file, Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        SaveToFile(file, bytes);
    }

    public static void SaveToFile(string file, byte[] data)
    {
        FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
        fs.Write(data, 0, data.Length);
        fs.Flush();
        fs.Close();
        fs.Dispose();
    }

   
    public static BlockType GetBlockType(BlockNameEnum name)
    {
        return NameToBlockType[name];
    }


    public static List<BlockType> PlantBlocks=new List<BlockType>();
    public static List<BlockType> TallGrassBlocks=new List<BlockType>();
    public static BlockType GetPlantBlockTypes()
    {
        
          //  return PlantBlocks[UnityEngine.Random.Range(0, PlantBlocks.Count)]; 
       
             return PlantBlocks[ChunkManager.rd.Next(0, PlantBlocks.Count)];
           
        

   
    }

    public Transform go;
    public Transform GetGo(Vector3 pos, Quaternion rot)
    {
        if (NameToFullMeshDate.TryGetValue(blockName,out go))
        {

           
        }
        else
        {
             go = new GameObject().transform;
           var  mr = go.gameObject.AddComponent<MeshRenderer>();
          var    mf = go.gameObject.AddComponent<MeshFilter>();
              Mesh m = new Mesh();
            MeshData_.Combine(md,m);
            mf.sharedMesh = m;
            if (isPlant)
            {
                mr.material = ChunkManager.Instance.Foliage;
            }
            if (!isTransparent&&!isPlant)
            {
                mr.material = ChunkManager.Instance.Opaque;
            }
            NameToFullMeshDate.Add(blockName,go);
          
        }
        return ObjPool.GetComponent(go,pos,rot);
    }
    public static BlockType GetTallGrassBlock()
    {
       
           // return TallGrassBlocks[UnityEngine.Random.Range(0, TallGrassBlocks.Count)];
              return TallGrassBlocks[ChunkManager.rd.Next(0, TallGrassBlocks.Count)];
        

     
    }


    public static bool IsAirBlock(BlockType block)
    {
        if (block == null)
            return true;
        else
        {
            if (block.blockName == BlockNameEnum.Air)
                return true;
            else
                return false;
        }



    }
    /////////////////////////////////////////////////////
    /*------------------------ STATIC VARIABLES ------------------------*/

    public static readonly Vector3[] FACE_DIRECTIONS = {
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    public int GenerateMesh(bool[] faceIsVisible, MeshData_ md  )
    {

     
        if (isBillboard)
        {

            return GenerateBillboardFaces(  md );

        }
        else
        {

            return GenerateCubeFaces(faceIsVisible,  md );

        }
    }



    void GenerateMeshInit(bool[] faceIsVisible, ref int startIndex)
    {
       
        if (isBillboard)
        {

             GenerateBillboardFacesInit( ref startIndex);

        }
        else
        {

             GenerateCubeFacesInit(faceIsVisible, ref startIndex);

        }
    }


    static Vector3[] baseVertices =
      {

            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),
       new Vector3(-0.5f, -0.5f, 0.5f),

         new Vector3(0.5f, -0.5f, 0.5f),
   new Vector3(0.5f, 0.5f, 0.5f),
   new Vector3(-0.5f, 0.5f, -0.5f),
    new Vector3(-0.5f, -0.5f, -0.5f),

   
        };
    static Color[] baseColors = {
            Color.black,
            Color.red,
            Color.red,
            Color.black,
        };
    static Quaternion[] rotations =
      {
            Quaternion.AngleAxis(45f, Vector3.up),
            Quaternion.AngleAxis(-45f, Vector3.up),
            Quaternion.AngleAxis(135f, Vector3.up),
            Quaternion.AngleAxis(-135f, Vector3.up),

              Quaternion.AngleAxis(45f, Vector3.up),
            Quaternion.AngleAxis(-45f, Vector3.up),
            Quaternion.AngleAxis(135f, Vector3.up),
            Quaternion.AngleAxis(-135f, Vector3.up)
        };
    static int[] triangles = {
            0, 1, 2, 0, 2, 3,
            0+4, 1+4, 2+4, 0+4, 2+4, 3+4,
            0+8, 1+8, 2+8, 0+8, 2+8, 3+8,
            0+12, 1+12, 2+12, 0+12, 2+12, 3+12
        };



    public int GenerateBillboardFacesInit(  ref int startIndex)
    {



        md.vertexCount = 8;

        //  Quaternion rotation = rotations[i];
        for (int j = 0; j < 8; j++)
        {
            md.vertexDate[j].vertice = baseVertices[j];
        }



        Vector3 normal = new Vector3(0f, 0f, 1f);
        for (int i = 0; i < 8; i++)
        {
            //  md.normals.Add(normal);
            md.vertexDate[startIndex + i].normal = normal;
        }

        Vector2Int atlasIndex = atlasPositions[0];

        int indextemp = startIndex;
        for (int i = 0; i < rotations.Length; i++)
        {
            //   atlasReader.GetUVs(atlasIndex.x, atlasIndex.y, ref md, indextemp);
            indextemp += 4;
        }
        var tempTex = Up;
        if (tempTex != null)
        //  if (false)
        {
           Rect rc = RectDic[tempTex];
           // Rect rc = new Rect(0, 0, 0, 0);


            md.vertexDate[0].uv.x = rc.x + md.vertexDate[0].uv.x * rc.width;
            md.vertexDate[0].uv.y = rc.y + md.vertexDate[0].uv.y * rc.height;

            md.vertexDate[1].uv.x = rc.x + md.vertexDate[1].uv.x * rc.width;
            md.vertexDate[1].uv.y = rc.y + md.vertexDate[1].uv.y * rc.height;

            md.vertexDate[2].uv.x = rc.x + md.vertexDate[2].uv.x * rc.width;
            md.vertexDate[2].uv.y = rc.y + md.vertexDate[2].uv.y * rc.height;

            md.vertexDate[3].uv.x = rc.x + md.vertexDate[3].uv.x * rc.width;
            md.vertexDate[3].uv.y = rc.y + md.vertexDate[3].uv.y * rc.height;

            //////////////////////////
            md.vertexDate[4].uv.x = rc.x + md.vertexDate[4].uv.x * rc.width;
            md.vertexDate[4].uv.y = rc.y + md.vertexDate[4].uv.y * rc.height;

            md.vertexDate[5].uv.x = rc.x + md.vertexDate[5].uv.x * rc.width;
            md.vertexDate[5].uv.y = rc.y + md.vertexDate[5].uv.y * rc.height;

            md.vertexDate[6].uv.x = rc.x + md.vertexDate[6].uv.x * rc.width;
            md.vertexDate[6].uv.y = rc.y + md.vertexDate[6].uv.y * rc.height;

            md.vertexDate[7].uv.x = rc.x + md.vertexDate[7].uv.x * rc.width;
            md.vertexDate[7].uv.y = rc.y + md.vertexDate[7].uv.y * rc.height;

            //////////////////////////
            md.vertexDate[8].uv.x = rc.x + md.vertexDate[8].uv.x * rc.width;
            md.vertexDate[8].uv.y = rc.y + md.vertexDate[8].uv.y * rc.height;

            md.vertexDate[9].uv.x = rc.x + md.vertexDate[9].uv.x * rc.width;
            md.vertexDate[9].uv.y = rc.y + md.vertexDate[9].uv.y * rc.height;

            md.vertexDate[10].uv.x = rc.x + md.vertexDate[10].uv.x * rc.width;
            md.vertexDate[10].uv.y = rc.y + md.vertexDate[10].uv.y * rc.height;

            md.vertexDate[11].uv.x = rc.x + md.vertexDate[11].uv.x * rc.width;
            md.vertexDate[11].uv.y = rc.y + md.vertexDate[11].uv.y * rc.height;

            //////////////////////////
            md.vertexDate[12].uv.x = rc.x + md.vertexDate[12].uv.x * rc.width;
            md.vertexDate[12].uv.y = rc.y + md.vertexDate[12].uv.y * rc.height;

            md.vertexDate[13].uv.x = rc.x + md.vertexDate[13].uv.x * rc.width;
            md.vertexDate[13].uv.y = rc.y + md.vertexDate[13].uv.y * rc.height;

            md.vertexDate[14].uv.x = rc.x + md.vertexDate[14].uv.x * rc.width;
            md.vertexDate[14].uv.y = rc.y + md.vertexDate[14].uv.y * rc.height;

            md.vertexDate[15].uv.x = rc.x + md.vertexDate[15].uv.x * rc.width;
            md.vertexDate[15].uv.y = rc.y + md.vertexDate[15].uv.y * rc.height;






            int c = 0;
        }




        startIndex += 16;
        //   this.md = md;
        return 16;
    }



    public int GenerateBillboardFaces(   MeshData_ md)
    {



        Array.Copy(this.md.vertexDate, 0, md.vertexDate, md.vertexCount, 8);


        md.vertexCount += 8;
        return 8;
    }







    public int GenerateCubeFaces(bool[] faceIsVisible, MeshData_ md)
    {

        int vc = 0;
        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
            if (faceIsVisible[i] == false)
            {
                continue; // Don't bother making a mesh for a face that can't be seen.
            }

            // faceIsVisible[i] = true;
            int iiii = i * 4;
            md.vertexDate[md.vertexCount] = this.md.vertexDate[iiii];
            md.vertexDate[md.vertexCount + 1] = this.md.vertexDate[iiii + 1];
            md.vertexDate[md.vertexCount + 2] = this.md.vertexDate[iiii + 2];
            md.vertexDate[md.vertexCount + 3] = this.md.vertexDate[iiii + 3];
            //  Array.Copy(this.md.vertexDate, iiii, md.vertexDate, startIndex,4);
            // Buffer.BlockCopy(this.md.vertexDate, iiii, md.vertexDate, startIndex, 32);
            md.vertexCount += 4;
            vc += 4;




        }


        return vc;
    }



    public int GenerateWaterFaces(bool[] faceIsVisible, MeshData_ md  )
    {

        int vc = 0;
        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
            //  if (faceIsVisible[i] == false)
            //   {
            //       continue; // Don't bother making a mesh for a face that can't be seen.
            //   }

            // faceIsVisible[i] = true;
            int iiii = i * 4;
            md.vertexDate[md.vertexCount] = this.md.vertexDate[iiii];
            md.vertexDate[md.vertexCount + 1] = this.md.vertexDate[iiii + 1];
            md.vertexDate[md.vertexCount + 2] = this.md.vertexDate[iiii + 2];
            md.vertexDate[md.vertexCount + 3] = this.md.vertexDate[iiii + 3];
            //  Array.Copy(this.md.vertexDate, iiii, md.vertexDate, startIndex,4);
            // Buffer.BlockCopy(this.md.vertexDate, iiii, md.vertexDate, startIndex, 32);
            md.vertexCount += 4;
            vc += 4;

            return vc;


        }


        return vc;
    }

    /// <summary>
    /// ////////////////////

    int GenerateCubeFacesInit(bool[] faceIsVisible , ref int startIndex)
    {

        int vc = 0;
        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {


            Vector2Int[] atlasPositions = this.atlasPositions;
            // int index = atlasPositions.Length == 1 ? 0 : i;

            int index01 = startIndex;
            int index02 = startIndex + 1;
            int index03 = startIndex + 2;
            int index04 = startIndex + 3;
            // GenerateBlockFace(FACE_DIRECTIONS[i], c, ref md);
            ////////////////////////////////////////////////////////////////////////



            Vector3 direction = FACE_DIRECTIONS[i];


            md.vertexDate[index01].normal = direction;
            md.vertexDate[index02].normal = direction;
            md.vertexDate[index03].normal = direction;
            md.vertexDate[index04].normal = direction;
            Texture2D tempTex = null;
            Texture2D tempTexn = null;
            if (direction == Vector3.up)
            {
                tempTex = Up;
                tempTexn = Up_N;
                md.vertexDate[index01].vertice = new Vector3(-0.5f, 0.5f, -0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, 0.5f, 0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, 0.5f, 0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, 0.5f, -0.5f);





            }
            else if (direction == Vector3.down)
            {
                tempTex = Down;
                tempTexn = Down_N;
                md.vertexDate[index01].vertice = new Vector3(-0.5f, -0.5f, 0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, -0.5f, -0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, -0.5f, -0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, -0.5f, 0.5f);
            }
            else if (direction == Vector3.right)
            {
                tempTex = Side;
                tempTexn = Side_N;
                md.vertexDate[index01].vertice = new Vector3(0.5f, -0.5f, -0.5f);
                md.vertexDate[index02].vertice = new Vector3(0.5f, 0.5f, -0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, 0.5f, 0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, -0.5f, 0.5f);
            }
            else if (direction == Vector3.left)
            {
                tempTex = Side;
                tempTexn = Side_N;
                md.vertexDate[index01].vertice = new Vector3(-0.5f, -0.5f, 0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, 0.5f, 0.5f);
                md.vertexDate[index03].vertice = new Vector3(-0.5f, 0.5f, -0.5f);
                md.vertexDate[index04].vertice = new Vector3(-0.5f, -0.5f, -0.5f);
            }
            else if (direction == Vector3.forward)
            {
                tempTex = Side;
                tempTexn = Side_N;
                md.vertexDate[index01].vertice = new Vector3(0.5f, -0.5f, 0.5f);
                md.vertexDate[index02].vertice = new Vector3(0.5f, 0.5f, 0.5f);
                md.vertexDate[index03].vertice = new Vector3(-0.5f, 0.5f, 0.5f);
                md.vertexDate[index04].vertice = new Vector3(-0.5f, -0.5f, 0.5f);
            }
            else if (direction == Vector3.back)
            {
                tempTex = Side;
                tempTexn = Side_N;
                md.vertexDate[index01].vertice = new Vector3(-0.5f, -0.5f, -0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, 0.5f, -0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, 0.5f, -0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, -0.5f, -0.5f);
            }


            int index2 = atlasPositions.Length == 1 ? 0 : i;

            if (!isWater)
            {
            

                if (blockName == BlockNameEnum.Grass || blockName == BlockNameEnum.Sand)
                {
                   
                }

                if (tempTex != null)
                //  if (false)
                {
                 Rect rc = RectDic[tempTex];

                  //  Rect rc =new Rect(0,0,0,0);

                    md.vertexDate[index01].uv.x = rc.x + md.vertexDate[index01].uv.x * rc.width;
                    md.vertexDate[index01].uv.y = rc.y + md.vertexDate[index01].uv.y * rc.height;

                    md.vertexDate[index02].uv.x = rc.x + md.vertexDate[index02].uv.x * rc.width;
                    md.vertexDate[index02].uv.y = rc.y + md.vertexDate[index02].uv.y * rc.height;

                    md.vertexDate[index03].uv.x = rc.x + md.vertexDate[index03].uv.x * rc.width;
                    md.vertexDate[index03].uv.y = rc.y + md.vertexDate[index03].uv.y * rc.height;

                    md.vertexDate[index04].uv.x = rc.x + md.vertexDate[index04].uv.x * rc.width;
                    md.vertexDate[index04].uv.y = rc.y + md.vertexDate[index04].uv.y * rc.height;

                    int c = 0;
                }




            }
            else
            {//对水进行特殊处理 水使用自己单独的材质和贴图 
                md.vertexDate[0].uv = new Vector2(0, 0);
                md.vertexDate[0 + 1].uv = new Vector2(0, 1);
                md.vertexDate[0 + 2].uv = new Vector2(1, 0);
                md.vertexDate[0 + 3].uv = new Vector2(1, 1);
            }




            startIndex += 4;
            vc += 4;




        }
        md.vertexCount = 24;
        //   this.md = md;
        return vc;
    }




    /*------------------------ STATIC METHODS ------------------------*/






}
