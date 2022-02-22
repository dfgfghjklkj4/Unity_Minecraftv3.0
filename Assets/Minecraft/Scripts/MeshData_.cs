using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;




    public struct Vertex
    {
        public Vector3 vertice;
        public Vector3 normal;
        public Vector2 uv;
    }

    public class MeshData_
    {
        //public Vector3[] vertices;
        // public Vector3[] normals;
           //public Vector2[] uv;
        //    public Color[] _colors;
        public Vertex[] vertexDate;
      //  public int  startIndex ;
       // public static int[] triangles;
      public static NativeArray<byte> triangles;
        public static int trianglIndexSize = 2;//(byte[])uint16 2  (int[])int 4
       public MeshData_(int c)
        {

            vertexDate = new Vertex[c];

        }






   
        public int TriangleCount;
        public int vertexCount;

        public static int maxCacah = 512;

        static VertexAttributeDescriptor[] tempvd = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor
                (VertexAttribute.Position, VertexAttributeFormat.Float32, 3,0),
                 new VertexAttributeDescriptor
                (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3,0),
                    new VertexAttributeDescriptor
                (VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2,0)
        };




    public static void Combine(MeshData_ md, Mesh combinedMesh)
    {
        //  MeshData_ md = BlockType.NameToFullMeshDate[block.blockName];
        //使用新的网格工具来合并
        int totolVerticle = md.vertexCount;
        int totolTriangel = md.vertexCount / 2 * 3; ;


        MeshDataArray dataArray = Mesh.AllocateWritableMeshData(1);
        //  MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(combinedMesh);
        MeshData data = dataArray[0];
        data.SetVertexBufferParams(totolVerticle,
       tempvd

      );


        NativeArray<Vertex> vd = data.GetVertexData<Vertex>();
        NativeArray<Vertex>.Copy(md.vertexDate, vd, totolVerticle);


        data.SetIndexBufferParams(totolTriangel, IndexFormat.UInt16);
        var ib = data.GetIndexData<byte>();
         NativeArray<byte>.Copy(MeshData_.triangles, ib, totolTriangel * 2);
      //  unsafe
      //  {
       //     UnsafeUtility.MemCpy(ib.GetUnsafePtr(), MeshData_.triangles.GetUnsafeReadOnlyPtr(), totolTriangel * 2);
       // }



        data.subMeshCount = 1;


        SubMeshDescriptor sm = new SubMeshDescriptor(0, totolTriangel, MeshTopology.Triangles);

        data.SetSubMesh(0, sm, MeshUpdateFlags.DontNotifyMeshUsers);

        Mesh.ApplyAndDisposeWritableMeshData(dataArray, combinedMesh, MeshUpdateFlags.Default);

        //Mesh.ApplyAndDisposeWritableMeshData(dataArray, combinedMesh, MeshUpdateFlags.Default);
        combinedMesh.RecalculateBounds();
        //  combinedMesh.bounds = WorldChunk.bound;
        ib.Dispose();
          vd.Dispose();

        //    combinedMesh.SetVertices(chunk.coubinMD.vertices, 0, totolVerticle);
        //     combinedMesh.SetNormals(chunk.coubinMD.normals, 0, totolVerticle);
        //    combinedMesh.SetUVs(0, chunk.coubinMD.uv, 0, totolVerticle);


        // combinedMesh.SetTriangles(MeshData_.triangles, 0, totolTriangel, 0);

        return;

    }


 

  

    public static float tiem;





        ///////////////////////////////////////////

        public static ConcurrentQueue<MeshData_> MDPoolMax;

        public static Queue<MeshData_> MDPool24;

        public static SortedDictionary<int, Queue<MeshData_[]>> MeshDataArrayPool = new SortedDictionary<int, Queue<MeshData_[]>>();
        public static void InitCacah()
        {
            if (trianglIndexSize==2)
            {
                triangles = new NativeArray<byte>(65532 * trianglIndexSize * 2, Allocator.Persistent);
              
                int index = 0;
                int iii = 65532 * trianglIndexSize / 6;
                byte[] intBuff;
                for (int i = 0; i < iii; i++)
                {
                    //  /*byte数组存放索引
                    UInt16 tri = (UInt16)(i * 4);
                    intBuff = BitConverter.GetBytes(tri);
                    for (int j = 0; j < intBuff.Length; j++)
                    {
                        triangles[index] = intBuff[j];
                        index++;
                    }

                    tri = (UInt16)(i * 4 + 1);
                    intBuff = BitConverter.GetBytes(tri);
                    for (int j = 0; j < intBuff.Length; j++)
                    {
                        triangles[index] = intBuff[j];
                        index++;
                    }
                    tri = (UInt16)(i * 4 + 2);
                    intBuff = BitConverter.GetBytes(tri);
                    for (int j = 0; j < intBuff.Length; j++)
                    {
                        triangles[index] = intBuff[j];
                        index++;
                    }
                    tri = (UInt16)(i * 4);
                    intBuff = BitConverter.GetBytes(tri);
                    for (int j = 0; j < intBuff.Length; j++)
                    {
                        triangles[index] = intBuff[j];
                        index++;
                    }
                    tri = (UInt16)(i * 4 + 2);
                    intBuff = BitConverter.GetBytes(tri);
                    for (int j = 0; j < intBuff.Length; j++)
                    {
                        triangles[index] = intBuff[j];
                        index++;
                    }
                    tri = (UInt16)(i * 4 + 3);
                    intBuff = BitConverter.GetBytes(tri);
                    for (int j = 0; j < intBuff.Length; j++)
                    {
                        triangles[index] = intBuff[j];
                        index++;
                    }
                 

                }
            }
           else if (trianglIndexSize == 4)
            { /*
                       triangles = new int[750000];
                int index = 0;
                for (int i = 0; i < 12500; i++)
                {
                  
                   
                  // int 数组存放索引
                       triangles[index] = i * 4;
                    triangles[index + 1] = i * 4 + 1;
                    triangles[index + 2] = i * 4 + 2;
                    triangles[index + 3] = i * 4;
                    triangles[index + 4] = i * 4 + 2;
                    triangles[index + 5] = i * 4 + 3;
                 
                    index += 6;
                }
                     */

            }

            MDPoolMax = new ConcurrentQueue<MeshData_>();
            MDPool24 = new Queue<MeshData_>(maxCacah);
            for (int i = 0; i < maxCacah; i++)
            {

                MDPool24.Enqueue(new MeshData_(24));
            }
            for (int i = 0; i < 512; i++)
            {

                MDPoolMax.Enqueue(new MeshData_(maxVertices));
            }
        }

        public static int maxVertices = 65000;





        public static MeshData_ GetMax()
        {

            if (MDPoolMax.Count > 0)
            { MeshData_ md;
                if (!MDPoolMax.TryDequeue(out md))
                {
                        return new MeshData_(maxVertices);
                }
               
                md.TriangleCount = 0;
                md.vertexCount = 0;
                // Debug.Log(  verticeCount + " item.Value.Count  "+ item.Value.Count);
                return md;
            }

            else
            {
                Debug.Log("null  Max");
                
                return new MeshData_(maxVertices);
            }


        }

        public static void ReturnMax(MeshData_ md)
        {

if (md!=null)
{
      MDPoolMax.Enqueue(md);
            md=null;
}
          
        }

        public static MeshData_ Get24()
        {

            if (MDPool24.Count > 0)
            {
                MeshData_ md = MDPool24.Dequeue();
                md.TriangleCount = 0;
                md.vertexCount = 0;
                // Debug.Log(  verticeCount + " item.Value.Count  "+ item.Value.Count);
                return md;
            }

            else
            {
                Debug.Log("null  24");
                return new MeshData_(24);
            }


        }

        public static void Return24(MeshData_ md)
        {


            MDPool24.Enqueue(md);
        }




        /////////////////////////////////////////////////


        public static MeshData_[] GetArray(int arrCount)
        {
            //  MeshData md;
            if (MeshDataArrayPool.ContainsKey(arrCount))
            {

                //  if (item.Key>=size&& item.Value.Count > 0)
                if (MeshDataArrayPool[arrCount].Count > 0)
                {

                    MeshData_[] md = MeshDataArrayPool[arrCount].Dequeue();

                    //  Debug.Log(  verticeCount + " item.Value.Count  "+ item.Value.Count);
                    return md;
                }

                //   Debug.Log("kong  ");
                return new MeshData_[arrCount];


            }
            else
            {
                //  Debug.Log("null");
                return new MeshData_[arrCount];
            }


        }

        public static void Return(MeshData_[] md, int Len)
        {

            //   Array.Clear(md, 0, Len);
            if (MeshDataArrayPool.ContainsKey(md.Length))
            {
                MeshDataArrayPool[md.Length].Enqueue(md);
            }
            else
            {
                Queue<MeshData_[]> q = new Queue<MeshData_[]>();
                q.Enqueue(md);
                MeshDataArrayPool.Add(md.Length, q);


            }


        }
    }



