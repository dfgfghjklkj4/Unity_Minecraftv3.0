using HorizonBasedAmbientOcclusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class toggleEvent : MonoBehaviour
{
    public GameObject EF;
    public Toggle T;
    // Start is called before the first frame update
 
    // Update is called once per frame
   public void Open()
    {
        EF.gameObject.SetActive(T.isOn);
    }

  

}
