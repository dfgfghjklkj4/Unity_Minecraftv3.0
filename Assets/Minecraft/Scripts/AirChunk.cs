using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirChunk : MonoBehaviour
{
       public  WorldChunk chunk;
    // Start is called before the first frame update
   public void Awake0()
    {
          chunk = new WorldChunk();
        chunk.computedTerrainDate = true;
        chunk.isEmperty = true;
        chunk.isAir = true;
        chunk.NeighborUp = chunk;
        chunk.Initialized = true;
        chunk. hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
          chunk. Blocks = new BlockType[ChunkManager.Instance.chunkSize.x,ChunkManager.Instance.chunkSize.y, ChunkManager.Instance.chunkSize.z];
        for (int x = 0; x < ChunkManager.Instance. chunkSize.x; x++)
        {
            for (int y = 0; y <ChunkManager.Instance.  chunkSize.y; y++)
            {
                for (int z = 0; z < ChunkManager.Instance. chunkSize.z; z++)
                {
                    chunk.Blocks[x, y, z] = BlockType.Air;
                }
            }
        }
    }

   
}
