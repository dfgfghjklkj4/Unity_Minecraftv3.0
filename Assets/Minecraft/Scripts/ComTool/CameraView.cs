using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class CameraView : MonoBehaviour {

     Camera theCamera;

    //距离摄像机8.5米 用黄色表示
      float upperDistance = 8.5f;
    //距离摄像机12米 用红色表示
     float lowerDistance = 12.0f;

    private Transform tx;


    void Start()
    {
     
            theCamera = this.GetComponent<Camera>();
       

     
    }


    void Update()
    {
        upperDistance = theCamera.farClipPlane;
        lowerDistance = theCamera.nearClipPlane;
        tx = theCamera.transform;
        FindUpperCorners();
        FindLowerCorners();

        FindLower2UpperCorners();
    }


    void FindUpperCorners()
    {
        Vector3[] corners = GetCorners(upperDistance);

        // for debugging
        Debug.DrawLine(corners[0], corners[1], Color.yellow); // UpperLeft -> UpperRight
        Debug.DrawLine(corners[1], corners[3], Color.yellow); // UpperRight -> LowerRight
        Debug.DrawLine(corners[3], corners[2], Color.yellow); // LowerRight -> LowerLeft
        Debug.DrawLine(corners[2], corners[0], Color.yellow); // LowerLeft -> UpperLeft
    }


    void FindLowerCorners()
    {
        Vector3[] corners = GetCorners(lowerDistance);

        // for debugging
        Debug.DrawLine(corners[0], corners[1], Color.red);
        Debug.DrawLine(corners[1], corners[3], Color.red);
        Debug.DrawLine(corners[3], corners[2], Color.red);
        Debug.DrawLine(corners[2], corners[0], Color.red);
    }

    void FindLower2UpperCorners()
    {
        Vector3[] corners_upper = GetCorners(upperDistance);
        Vector3[] corners_lower = GetCorners(lowerDistance);

        Debug.DrawLine(corners_lower[0], corners_upper[0], Color.blue);
        Debug.DrawLine(corners_lower[1], corners_upper[1], Color.blue);
        Debug.DrawLine(corners_lower[2], corners_upper[2], Color.blue);
        Debug.DrawLine(corners_lower[3], corners_upper[3], Color.blue);
    }


    Vector3[] GetCorners(float distance)
    {
        Vector3[] corners = new Vector3[4];

        float halfFOV = (theCamera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
        float aspect = theCamera.aspect;

        float height = distance * Mathf.Tan(halfFOV);
        float width = height * aspect;

        // UpperLeft
        corners[0] = tx.position - (tx.right * width);
        corners[0] += tx.up * height;
        corners[0] += tx.forward * distance;

        // UpperRight
        corners[1] = tx.position + (tx.right * width);
        corners[1] += tx.up * height;
        corners[1] += tx.forward * distance;

        // LowerLeft
        corners[2] = tx.position - (tx.right * width);
        corners[2] -= tx.up * height;
        corners[2] += tx.forward * distance;

        // LowerRight
        corners[3] = tx.position + (tx.right * width);
        corners[3] -= tx.up * height;
        corners[3] += tx.forward * distance;

        return corners;
    }
}

