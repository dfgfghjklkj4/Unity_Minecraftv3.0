using System.Runtime.InteropServices.ComTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerCenter : MonoBehaviour
{
    public static ManagerCenter Instance;
    public MonitorController monitor;

    // Start is called before the first frame update
    void Awake()
    {
        Instance=this;
      TNTController.AddColQueue(200);
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }   
}
