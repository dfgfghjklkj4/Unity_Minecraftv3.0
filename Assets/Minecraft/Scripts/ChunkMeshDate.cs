using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static UnityEngine.Mesh;

public class ChunkMeshDate : MonoBehaviour
{
       public MeshFilter OpaqueMeshFilter, WaterMeshFilter, FoliageMeshFilter, TreeMeshFilter;
    public MeshRenderer OpaqueMeshRenderer, WaterMeshRenderer, FoliageMeshRenderer, TreeMeshRenderer;
    public MeshCollider OpaqueMeshCol, WaterMeshCol, FoliageMeshCol, TreeMeshCol;
    public WorldChunk chunk;
  
  public bool active;


     public  void Active()
    {
        if (!active)
        {
         



            // gameObject.name = ID.ToString();
            Mesh m1 = new Mesh();
            OpaqueMeshFilter.sharedMesh = m1;

           // OpaqueMeshCol.sharedMesh = OpaqueMeshFilter.sharedMesh;

if (WaterMeshFilter!=null)
{
     Mesh m2 = new Mesh();
            WaterMeshFilter.sharedMesh = m2;
            // WaterMeshCol = WaterMeshFilter.GetComponent<MeshCollider>();
            WaterMeshCol.sharedMesh = WaterMeshFilter.sharedMesh;
}
           

            Mesh m3 = new Mesh();
            FoliageMeshFilter.sharedMesh = m3;
            //  FoliageMeshCol = FoliageMeshFilter.GetComponent<MeshCollider>();
          //  FoliageMeshCol.sharedMesh = FoliageMeshFilter.sharedMesh;



            active = true;
        }

    }


        public static ConcurrentStack<ChunkMeshDate> pool = new ConcurrentStack<ChunkMeshDate>();


public void RetrunPool()
{
   if ( OpaqueMeshRenderer.renderingLayerMask  > 0)
            {
                OpaqueMeshCol.enabled = false;
              OpaqueMeshRenderer.renderingLayerMask = 0;
               OpaqueMeshFilter.sharedMesh.Clear();
            }
           if ( WaterMeshRenderer.renderingLayerMask> 0)
            {
               WaterMeshCol.enabled = false;
               WaterMeshRenderer.renderingLayerMask = 0;
               WaterMeshFilter.sharedMesh.Clear();
            }
            if (   FoliageMeshRenderer.renderingLayerMask > 0)
            {
               FoliageMeshCol.enabled = false;
              FoliageMeshRenderer.renderingLayerMask = 0;
               FoliageMeshFilter.sharedMesh.Clear();
            }
chunk=null;
    pool.Push(this);
}
    public static ChunkMeshDate GetMeshDate(WorldChunk C)
    {
      
        ChunkMeshDate MD;
        if (pool.Count > 0)
        {
            if (!pool.TryPop( out MD))
            
            {ChunkManager.Instance.c4++;
                 MD =Instantiate<ChunkMeshDate>(ChunkManager.Instance.ChunkPrefab,C.ID,Quaternion.identity);
              MD.chunk=C;
           MD. Active();
          //  Debug.Log("gggggggggggg");
            }else{
  
            MD.chunk=C;
            MD.transform.position=C.ID;
            }
         
        }
        else
        {ChunkManager.Instance.c4++;
            MD =Instantiate<ChunkMeshDate>(ChunkManager.Instance.ChunkPrefab,C.ID,Quaternion.identity);
              MD.chunk=C;
           MD. Active();
          //  Debug.Log("GetMeshDate");
        }
        C.meshDate=MD;
        return MD;
    }
}
