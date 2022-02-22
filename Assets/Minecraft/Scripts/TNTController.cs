using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TNTController : MonoBehaviour
{
    public static TNTController instanc;
    public static readonly float FUSE_TIME = 2.0f;
    public static readonly float LAUNCH_VELOCITY = 20.0f;
 public static readonly float Explode_distance = 5;
    public GameObject explosionEffect;


    private float _timer = 0.0f;

   // private ChunkManager _chunkManager;

  
     public  Material flashMaterial;
    public static bool isFlashing;

    private void Awake()
    {
        if (!instanc)
        {
            instanc = this;
        }
    }

    private void Update()
    {
        if (isFlashing)
        {
            FlashMaterial();
        }

        TNT.TntExploedCheck();
    }


  
    string _EmissionColor = "_EmissionColor";

    private void FlashMaterial()
    {
   
        float t = Mathf.Sin(Time.time * Mathf.PI * 3.0f);
        Color color = Color.Lerp(Color.black, Color.white, t);

        flashMaterial.SetColor(_EmissionColor, color);
        

      
    }

    int blastRadius = 5;
    //   HashSet<WorldChunk> ExplodeWC = new HashSet<WorldChunk>();

public LayerMask layer;


    public static Dictionary<TNTController, float> TntToExplo = new Dictionary<TNTController, float>();
public static Queue<Collider[]> collidersQueuu=new Queue<Collider[]>();

public static void AddColQueue(int count_){
      int c= (int)TNTController.Explode_distance;
        c*=2;
        c=c*c*c;
        for (int i = 0; i < count_; i++)
        {
             TNTController.collidersQueuu.Enqueue(new Collider[24]);
        }
}

    public static Queue<TNTController> ToEXP = new Queue<TNTController>();
  //  Collider[] cols = new Collider[24];

    static List<TNTController> TntToExploTemp = new List<TNTController>(50);
    static List<float> TntToExploTempTime = new List<float>(50);


}
