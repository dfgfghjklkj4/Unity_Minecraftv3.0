using UnityEngine;
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
//[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook2_fw : MonoBehaviour {
   // public WeaponTool_fw wtool;

    public enum MouseAxes_fw { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
   public enum RotationAxes_fw { X = 0, Y = 1, Z = 2 }
    public MouseAxes_fw axes = MouseAxes_fw.MouseXAndY;
  //  public RotationAxes_fw RotationAxes;
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

  public  float rotationY = 0F;
    public float rotationX = 0F;
   private Vector3 eulerAngles;
   
    void LateUpdate( )
    {
       // if (Input.GetAxis("Mouse X")>0 || Input.GetAxis("Mouse Y")>0)
        {
            if (axes == MouseAxes_fw.MouseXAndY)
            {
                //   float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX ;
                //     rotationY += Input.GetAxis("Mouse Y") * sensitivityY ;
                //      rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                //    transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);



                float x = Input.GetAxis("Mouse X");
                if (Mathf.Abs(x) > 0)
                {
                    rotationX += Input.GetAxis("Mouse X") * sensitivityX;
                  if (minimumX != 0 || maximumX != 0)
                    {
                        rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);
                        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, rotationX, transform.localEulerAngles.z);

                    }
                    else
                    {
                        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, rotationX, transform.localEulerAngles.z);
                    }
                 //   Mathf.DeltaAngle

            
                }

                //////////////////////////////////////////////////
                float y = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(y) > 0)
                   
                {
                    rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                 
                        rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                    if (rotationY!= minimumY|| rotationY != maximumY)
                    {
                        transform.localEulerAngles = new Vector3(eulerAngles.x - rotationY, transform.localEulerAngles.y, transform.localEulerAngles.z);
                    }


                   
                }

              
            

            }
            else if (axes == MouseAxes_fw.MouseX)
            {
                float x = Input.GetAxis("Mouse X");
                if (Mathf.Abs(x)>0)
                {
                    if (x>0)
                    {
                     //   x = sensitivityX;
                    }
                    else if(x<0)
                    {
                      //  x = -sensitivityX;
                    }
               //     print(x);

                  //  rotationX += x * sensitivityX * WeaponTool_fw.mouselookSpeed;
                //   if (minimumX != 0 || maximumX != 0)
                 //   {
                   //     rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);
                 //   }

                    //  transform.localEulerAngles = new Vector3(eulerAngles.x, eulerAngles.y + rotationX, eulerAngles.z);
                    transform.Rotate(0,x * sensitivityX * Time.deltaTime, 0);

                }


                //    transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
            }
            else
            {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                if (minimumY != 0 || maximumY != 0)
                {
                    rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                }


                if (true)
                {

                }
                    //    transform.localEulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, eulerAngles.z + rotationY);
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - rotationY, transform.localEulerAngles.y, transform.localEulerAngles.z);
            


            }
        }

    
    }

    void Start( )
    {
        // if (!wtool.playerNet.localPlayer)
        //  {
        //      enabled = false;
        //   }
        eulerAngles = transform.localEulerAngles;
    }
}