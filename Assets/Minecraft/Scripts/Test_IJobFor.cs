using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Test_IJobFor : MonoBehaviour
{
    [BurstCompile]
    struct My_IJobFor : IJobFor
    {
        [ReadOnly]
        public NativeArray<int> array;

        public float deltaTime;

        public NativeArray<int> count;

        public void Execute(int index)
        {
            count[index] = Compute(index);
        }
    }

    public bool OpenJob;

    private void Update()
    {
        int a = 0;

        if (OpenJob)
        {
            var array = new NativeArray<int>(1000, Allocator.TempJob);
            var count = new NativeArray<int>(1000, Allocator.TempJob);

            for (int i = 0; i < 1000; i++)
            {
                array[i] = 1;
            }

            var job = new My_IJobFor()
            {
                array = array,
                deltaTime = Time.deltaTime,
                count = count
            };

            JobHandle sheduleJobDependency = new JobHandle();

            ///三种执行方式：

            //1.多个线程顺序执行多个Job，由于是顺序执行，需要等待上个Job执行完成才能执行下个Job，所以性能上与在主线程执行并无区别
            //JobHandle sheduleJobHandle = job.Schedule(array.Length, sheduleJobDependency);

            //2.并行 (迭代总次数，每个Job执行负责迭代次数，依赖的Job句柄)
            //将遍历总次数按批次平分成包，将不同的包放入不同线程中执行（有可能是主线程）。
            JobHandle sheduleJobHandle = job.ScheduleParallel(array.Length, 10, sheduleJobDependency);

            sheduleJobHandle.Complete();

            //2.直接在当前线程执行Job
            //job.Run(array.Length);   

            for (int i = 0; i < count.Length; i++)
            {
                a += count[i];
            }

            array.Dispose();
            count.Dispose();

            Debug.Log("job:" + a);
        }
        else
        {
            int z = 0;
            for (int i = 0; i < 1000; i++)
            {
                z += Compute(i);
            }
            a = z;
            Debug.Log(a);
        }
    }

    static int Compute(int i)
    {
        // 耗时的计算代码
        double value = 0f;
        for (int j = 0; j < 1000; j++)
        {
            value = math.exp10(math.sqrt(value));
        }

        return i;
    }
}