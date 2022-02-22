

using System.Collections.Generic;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class TestJob : MonoBehaviour
{
    public WorldChunk chunk;
    public List<float> f1=new List<float>();
     public List<float> f2=new List<float>();
     public  Vector2[] octaveOffsets;
    public int DataCount;

    private NativeArray<float3> m_JobDatas;

    private NativeArray<float> m_JobResults;

    private Vector3[] m_NormalDatas;
    private float3[] m_NormalDatas2;
    
    private float[] m_NormalResults;
    

// Job adding two floating point values together
    [BurstCompile]
    public struct MyParallelJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> data;
        public NativeArray<float> result;

        public void Execute(int i)
        {
            Vector3 item = data[i];
           // result[i] = Mathf.Sqrt(item.x * item.x + item.y * item.y + item.z * item.z);
             result[i] = Mathf.PerlinNoise(item.x , item.z);
        }
    }
 Thread thread1 ;
   void Thread1()
         {
           // while (true)
            {
            // Debug.Log(chunk.name);

            }
         }

         private void Start() {
               thread1 = new Thread(new ThreadStart(Thread1));
              //调用Start方法执行线程
            thread1.Start();
         }
    private void Awake0()
    {
     
    
        m_JobDatas = new NativeArray<float3>(DataCount, Allocator.Persistent);
        m_JobResults = new NativeArray<float>(DataCount,Allocator.Persistent);
        
        m_NormalDatas = new Vector3[DataCount];
         m_NormalDatas2 = new float3[DataCount];
        m_NormalResults = new float[DataCount];
        
        for (int i = 0; i < DataCount; i++)
        {
            m_JobDatas[i] = new float3(1, 1, 1);
            m_NormalDatas[i] = new Vector3(1, 1, 1);
             m_NormalDatas2[i] = new float3(1, 1, 1);
        }
    }

 private void Start0() {
    
     PerlinNoise.pseudoRandomNumber = new System.Random(11);
      PerlinNoise. octaveOffsets =PerlinNoise. GenerateOffsetsOnSeeds(4,  PerlinNoise.pseudoRandomNumber , Vector2.zero);
       octaveOffsets= PerlinNoise. octaveOffsets ;
       for (int x1 = 0; x1 < 4; x1++)
        {
            for (int z1 = 0; z1 < 4; z1++)
            {
             f1.Add( MapGenerator.Instanc.GenerateMapData2( Vector2.zero,x1, z1,4)) ;
            }
        }
        var     noiseMap = new float[4, 4];
         noiseMap = MapGenerator.Instanc.GenerateMapData(noiseMap, Vector2.zero, new Vector2Int(0, 0), 4);
          for (int x1 = 0; x1 < 4; x1++)
        {
            for (int z1 = 0; z1 < 4; z1++)
            {
             f2.Add( noiseMap[x1,z1]) ;
            }
        }
 }
    // Update is called once per frame
    void Update0()
    {


      //  if (Input.GetKeyDown(KeyCode.Space))
        { 
            Debug.Log(Time.frameCount+"      11111111");
            float f1=Time.realtimeSinceStartup;
              MyParallelJob jobData = new MyParallelJob();
        jobData.data = m_JobDatas;
        jobData.result = m_JobResults;
         JobHandle sheduleJobDependency = new JobHandle();

// Schedule the job with one Execute per index in the results array and only 1 item per processing batch
   // JobHandle handle  = jobData.Schedule(DataCount, 32);

// Wait for the job to complete
       // handle.Complete();
         Debug.Log(Time.frameCount+"    222");
         //   float f1=Time.realtimeSinceStartup-;
        Profiler.BeginSample("NormalCalculate");
        
        //正常数据运算
       for(var i = 0; i < DataCount; i++)
        {
           var item = m_NormalDatas[i];
     // m_NormalResults[i] = Mathf.Sqrt(item.x * item.x + item.y * item.y + item.z * item.z);

     // var item = m_NormalDatas[i];
      // m_JobResults[i] = Mathf.Sqrt(item.x * item.x + item.y * item.y + item.z * item.z);
       Mathf.PerlinNoise(item.x , item.z);
        }
        
        Profiler.EndSample();
        }

       // if (!handle.IsCompleted)
        {
         //      handle.Complete();
             //  handle.IsCompleted=false;
        //        Debug.Log(Time.frameCount);
        }
        //Job部分
      
    }

    public void OnDestroy()
    {
        thread1.Abort();
        m_JobDatas.Dispose();
        m_JobResults.Dispose();
        m_NormalDatas = null;
        m_NormalResults = null;
    }
}