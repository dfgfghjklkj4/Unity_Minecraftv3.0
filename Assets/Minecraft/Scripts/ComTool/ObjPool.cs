using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public struct PoolID
{
    public int id;
    public float time;
    public PoolID(int id_, float time_)
    {
        id = id_;
        time = time_;

    }
}


public class ObjPool : MonoBehaviour
{
   
    public static Dictionary<int, Stack<Component>> pool_go = new Dictionary<int, Stack<Component>>();


    public static List<Component> returntf = new List<Component>();
    public static List<PoolID> returntime = new List<PoolID>();
  
    // Use this for initialization
    void Awake()
    {


        DontDestroyOnLoad(this);


    }

    private void Update()
    {
        if (returntf.Count > 0)
        {
            int c = returntf.Count - 1;//ֱ����gos.Count - 1��c
            for (int i = c; i > -1; i--)
            {
                var pd = returntime[i];
                if (pd.time < Time.time)
                {
                 Component C=   returntf[i];
                        pool_go[pd.id].Push(C);
                    C.gameObject.SetActive(false);
                    returntf.RemoveAt(i);
                    returntime.RemoveAt(i);
                }

            }
        }

    }




    public static Vector3 worldEdgePos = new Vector3(-9999999f, -999999f, -999999f);
    public static void SetPoolStartSize<T>(T prefab, int count) where T : Component
    {


        Stack<Component> temp;
        int name = prefab.GetHashCode();
        if (!pool_go.TryGetValue(name, out temp))
        {
           temp = new Stack<Component>(count);
            pool_go.Add(name, temp );
        }

        for (int i = 0; i < count; i++)
        {
            T go;
            //   go = GameObject.Instantiate<T>(prefab).GetComponent<T>();
            go = Instantiate<T>(prefab, worldEdgePos, Quaternion.identity);
          
            go.hideFlags = HideFlags.HideInHierarchy;

            temp.Push(go);
            go.gameObject.SetActive(false);
        }


    }


    /// <summary>
    /// ///////

    public static T GetComponent<T>(T prefab, Vector3 pos, Quaternion rot) where T : Component
    {

        Stack<Component> qc;
        T go;
        int name = prefab.GetHashCode();
        if (pool_go.TryGetValue(name, out qc) )
        {
            if (qc.Count > 0)
                {
                    go = (T)qc.Pop();
                go.transform.SetPositionAndRotation(pos, rot);
            }
            else
            {
                go = Instantiate<T>(prefab, pos, rot);
            }
        
        }
        else
        {
            qc = new Stack<Component>();
            pool_go.Add(name, qc);
            go = Instantiate<T>(prefab,pos,rot);
           // go.hideFlags = HideFlags.HideInHierarchy;


        }
       if ( !go.gameObject.activeSelf)
          go.gameObject.SetActive(true);
        return go;
    }
    public static T GetComponent<T>(T prefab, Vector3 pos, Quaternion rot,float t) where T : Component
    {

        Stack<Component> qc;
        T go;
        int name = prefab.GetHashCode();
        if (pool_go.TryGetValue(name, out qc))
        {
            if (qc.Count > 0)
            {
                go = (T)qc.Pop();
                go.transform.SetPositionAndRotation(pos, rot);
            }
            else
            {
                go = Instantiate<T>(prefab, pos, rot);
            }
        }
        else
        {
            qc = new Stack<Component>();
            pool_go.Add(name, qc);
            go = Instantiate<T>(prefab, pos, rot);
            // go.hideFlags = HideFlags.HideInHierarchy;

        }
        returntf.Add(go);
        returntime.Add(new PoolID(name, Time.time + t));
          go.gameObject.SetActive(true);
        return go;
    }


    public static void ReturnGO<T>(T go,T key) where T : Component
    {
        int name = key.GetHashCode();
            pool_go[name].Push(go);
        go.gameObject.SetActive(false);
    }

    /*
       public static void ReturnGO<T>(T go, int key) where T : Component
    {
        pool_go[key].Push(go);
        go.gameObject.SetActive(false);
    }
         */



}