using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;

public class TNT : MonoBehaviour
{
    public static int key;
    public static readonly float FUSE_TIME = 3;
    public static readonly float LAUNCH_VELOCITY = 20.0f;
    public static readonly float Explode_distance = 5;
    public ParticleSystem explosionEffect;

    public Rigidbody rb;

    private float timer ;
    public bool flashing;
    // private ChunkManager _chunkManager;

    public Material mat;
    public MeshRenderer render;
    public void init()
    {
        gameObject.SetActive(true);
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            // rb.WakeUp();
        }
        timer = 0;
       isExplode = false;
        flashing = false;
        // col.enabled=true;
        if (render.material != mat)
        {

            render.material = mat;

        }
    }
    public void Launch(Vector3 direction)
    {
      
       rb.velocity = direction.normalized * LAUNCH_VELOCITY;

    }

    public void FireTNT()
    {
     
        flashing = true;
        render.material = TNTController.instanc.flashMaterial;
        TNTController.isFlashing = true;
        timer = FUSE_TIME+Time.time;
        TntToExplo.Add(this);
       // StartCoroutine(UpdateTNT_());
    }

   


  //  string _EmissionColor = "_EmissionColor";

    public Collider col;
    public AudioClip ac;
    int blastRadius = 5;
    //   HashSet<WorldChunk> ExplodeWC = new HashSet<WorldChunk>();
    public void Explode()
    {
        if (isExplode)
        {
            ObjPool.ReturnGO<TNT>(this,PlayerController.Instance.tntPrefab);
            return;
        }

        Vector3Int center = Vector3Int.RoundToInt(this.transform.position);
        WorldChunk chunk = null;
        BlockType block = ChunkManager.Instance.GetBlockAtPosition(center, ref chunk);

        if (!BlockType.IsAirBlock(block) && block.blockName == BlockNameEnum.Water)
        {
            // Don't cause damage when in water
        }
        else
        {


            int nonAirBlockCount = 0;

            for (int x = -blastRadius; x <= blastRadius; x++)
            {
                for (int y = -blastRadius; y <= blastRadius; y++)
                {
                    for (int z = -blastRadius; z <= blastRadius; z++)
                    {

                        Vector3Int pos = new Vector3Int(x, y, z) + center;

                        chunk = ChunkManager.GetNearestChunk(pos);
                        block = ChunkManager.Instance.GetBlockAtPosition(pos, ref chunk);

                        if (!BlockType.IsAirBlock(block))
                        {

                            float dist = Mathf.Sqrt(x * x + y * y + z * z);
                            if (dist <= blastRadius)
                            {
                                float blastPower = 2f / dist * blastRadius;
                                if (blastPower >= block.blockBlastResistance)
                                {

                                    Vector3Int localPos = chunk.WorldToLocalPosition(pos);

                                    if (!chunk.DestroyBlocks.Contains(localPos))
                                    {
                                        chunk.DestroyBlocks.Add(localPos);
                                    }
                                    ChunkManager.destroyChunks.Add(chunk);
                                }
                            }
                        }

                    }
                }
            }

            MonitorController mc = ManagerCenter.Instance.monitor;
            mc.OnBlocksExploded(nonAirBlockCount);

        }


        ObjPool.GetComponent<ParticleSystem>(explosionEffect, center, Quaternion.identity, 0.5f).gameObject.SetActive(true);


        isExplode = true;
        //col.enabled=false;

        ObjPool.ReturnGO<TNT>(this,PlayerController.Instance.tntPrefab);
        ExplodeCheckTNT();

    }

    public bool isExplode = false;

    public static List<TNT> TntToExplo = new List<TNT>(64);
    public static Queue<Collider[]> collidersQueuu = new Queue<Collider[]>();

    public static void AddColQueue(int count_)
    {
        int c = (int)TNTController.Explode_distance;
        c *= 2;
        c = c * c * c;
        for (int i = 0; i < count_; i++)
        {
            TNTController.collidersQueuu.Enqueue(new Collider[24]);
        }
    }

    //  Collider[] cols = new Collider[24];
    void ExplodeCheckTNT()

    {
        Collider[] cols;
        if (collidersQueuu.Count > 0)
        {
            cols = collidersQueuu.Dequeue();
        }
        else
        {
            int c = (int)TNTController.Explode_distance;
            c *= 2;
            c = c * c * c;
            cols = new Collider[24];
        }


        int count = Physics.OverlapSphereNonAlloc(transform.position, Explode_distance, cols,TNTController.instanc. layer);
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                TNT tc = cols[i].GetComponent<TNT>();


                if (!tc.isExplode)
                {
                    if (!TntToExplo.Contains(tc))
                    {
                        // tc.Explode();
                        //  tc.UpdateTNT(UnityEngine.Random.Range(1f,1.5f));
                        tc.timer = Time.time + UnityEngine.Random.Range(0.2F, 1.5f);
                        TntToExplo.Add(tc );
                    }
                }
            }
        }
        //
        collidersQueuu.Enqueue(cols);
        //Array.Clear(cols,0,cols.Length);

    }

   

    public static void TntExploedCheck()
    {
        if (TntToExplo.Count > 0)
        {
          

            int c = TntToExplo.Count - 1;

            int maxExp = 0;
             for (int i = c; i > -1; i--)
            {
                TNT tempTNT = TntToExplo[i];
                if (tempTNT.timer <= Time.time)
                {
                    maxExp++;
                    tempTNT.Explode();
                    TntToExplo.Remove(tempTNT);
                }
            }
          
          


        }
    }

}
