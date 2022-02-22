using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class speedui : MonoBehaviour
{
    public FlyFree fly;
    public Text text;
    float speed;
    // Start is called before the first frame update
   
    void Update()
    {
        if (speed!=fly.moveSpeed)
        {
            speed=fly.moveSpeed;
            text.text = "移动速度 "+ speed.ToString()+"m/s -- "+speed*3.6+"km/h";

        }
    }
}
