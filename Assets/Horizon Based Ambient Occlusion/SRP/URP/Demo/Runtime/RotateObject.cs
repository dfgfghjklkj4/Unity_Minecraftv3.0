using UnityEngine;

namespace HorizonBasedAmbientOcclusion.Universal
{
    public class RotateObject : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(Vector3.up * Time.deltaTime * 15.0f, Space.World);
        }
    }
}
