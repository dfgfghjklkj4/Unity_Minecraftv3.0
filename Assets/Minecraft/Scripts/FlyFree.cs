using UnityEngine;
using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.ComponentModel.Design;

public class FlyFree : MonoBehaviour
{
    //   public Transform came_;
    public float moveSpeed = 10;


    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    public float rotationY = 0F;
    public float rotationX = 0F;
    private Vector3 eulerAngles;


    private float timer;

    private void Start()
    {
     
        q = transform.rotation;
        lasty = transform.eulerAngles.y;
    }
    float lasty;
    float ry, ryc;
    //  float dy,dyc;
    Quaternion q;
    Vector3 vv;


   
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            moveSpeed += 10;
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            moveSpeed -= 10;
        }
        float py = transform.position.y;
     

        //  dy=Mathf
        if (Input.GetKey(KeyCode.Space))
        {
            //   Resources.UnloadUnusedAssets();

            //  GC.Collect();
            transform.Translate(0, moveSpeed * Time.deltaTime, 0);

        }
        if (Input.GetKey(KeyCode.X))
        {
         
          transform.Translate(0, -moveSpeed *0.5f* Time.deltaTime, 0);

        }
        transform.position=new Vector3( transform.position.x,Mathf.Clamp(transform.position.y,45f,85f), transform.position.z);
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(h + v) > 0)
        {
            float s = moveSpeed * Time.deltaTime;
            transform.Translate(s * h, 0, s * v);
        }

        if (sensitivityX>0)
        {
            float x = Input.GetAxis("Mouse X");
            if (Mathf.Abs(x) > 0)
            {
                rotationX += Input.GetAxis("Mouse X") * sensitivityX;
             //   if (minimumX != 0 || maximumX != 0)
             //   {
             //       rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);
              //      transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, rotationX, transform.localEulerAngles.z);

            //    }
             //   else
             //   {
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, rotationX, transform.localEulerAngles.z);
             //   }
                //   Mathf.DeltaAngle


            }
        }
        if (sensitivityY>0)
        {
            float y = Input.GetAxis("Mouse Y");
            if (Mathf.Abs(y) > 0)

            {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                if (rotationY != minimumY || rotationY != maximumY)
                {
                    transform.localEulerAngles = new Vector3(eulerAngles.x - rotationY, transform.localEulerAngles.y, transform.localEulerAngles.z);
                }



            }
        }

        //////////////////////////////////////////////////

     
          //  if (transform.position.y<90)
          //  {
          //      transform.position = new Vector3(transform.position.x, py, transform.position.z);
          //  }
        



        


    

    }

  
}