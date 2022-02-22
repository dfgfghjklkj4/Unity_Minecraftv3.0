using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedrockChunk : MonoBehaviour
{
     public  WorldChunk chunk;
    // Start is called before the first frame update
  public  void Awake0()
    {
          chunk = new WorldChunk();
        chunk.computedTerrainDate = true;
        chunk.isEmperty = false;
        chunk.isAir = false;
        chunk.NeighborDown = chunk;
        chunk.Initialized = true;
        chunk.isBedrockC=true;
        chunk. hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
          chunk. Blocks = new BlockType[ChunkManager.Instance.chunkSize.x,ChunkManager.Instance.chunkSize.y, ChunkManager.Instance.chunkSize.z];
        int h= ChunkManager.Instance. chunkSize.y-1;
        for (int x = 0; x < ChunkManager.Instance. chunkSize.x; x++)
        {
            for (int z = 0; z <ChunkManager.Instance.  chunkSize.z;z++)
            {
                  chunk.hightmap[x, z] = h;
                for (int y = 0; y < ChunkManager.Instance. chunkSize.y; y++)
                {
                    chunk.Blocks[x, y, z] = BlockType.Bedrock;
                }
            }
        }
    }
}
