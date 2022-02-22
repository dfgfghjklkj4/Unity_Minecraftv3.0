using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UnderWateref : MonoBehaviour

{

    public FogSettings underWaterfogset;
    public FogSettings fogset;
    public GameObject underwagerGo;
    public static UnderWateref Instanc;
    public RenderPipelineAsset underWaterRPAsset;
    RenderPipelineAsset RPAsset;
    [SerializeField] UniversalRendererData rendererData;

    public KawaseBlur kblur;
    float far;
    void Start()
    {
        fogset = new FogSettings();
        fogset.fog = RenderSettings.fog;
        fogset.fogColor = RenderSettings.fogColor;
        fogset.fogMode = RenderSettings.fogMode;
        fogset.fogDensity = RenderSettings.fogDensity;
        Instanc = this;
       
        underWaterVolume.profile.TryGet(out ld);

        gameObject.SetActive(false);

    }
   
    public Volume underWaterVolume;
    public Volume globalVolume;
    public Color UnderWaterColor;
    public bool UnderWater;

    // Effects


    public LensDistortion ld;
 



    public void OnEnterWater()
    {
        underwagerGo.SetActive(true);
        RPAsset = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = underWaterRPAsset;
        QualitySettings.renderPipeline = underWaterRPAsset;
        //  waterMat.shader=button;
        underWaterVolume.gameObject.SetActive(true);
        globalVolume.gameObject.SetActive(false);


       underWaterfogset.Set();
        far = Camera.main.farClipPlane;
        // Camera.main.farClipPlane=50f;

    }

    public void OnExitWater()
    {
        globalVolume.gameObject.SetActive(true);
        underWaterVolume.gameObject.SetActive(false);
       fogset.Set();
        underwagerGo.SetActive(false);
        GraphicsSettings.renderPipelineAsset = RPAsset;
        QualitySettings.renderPipeline = RPAsset;
        //   Camera.main.farClipPlane=far;
        //     waterMat.shader=top;
        gameObject.SetActive(false);
    }
    // Start is called before the first frame update
    void OnEnable()

    {

  


    }

    private void OnDisable()
    { 
    }
    // Update is called once per frame
    float t;
    public float ldxMultiplier, ldyMultiplier;
    public float x1, y1;
    Vector2 ldcenter;
    void Update()
    {


      

        if (t < Time.time)
        {
            t = Time.time + 1f;
            ldxMultiplier = Random.Range(0, 0.2f);
            ldyMultiplier = Random.Range(0, 0.2f);
            ldcenter = new Vector2(Random.Range(0.45f, 0.55f), Random.Range(0.45f, 0.55f));
        }
        var vx = Mathf.Lerp(ld.xMultiplier.value, ldxMultiplier, 1.5f * Time.deltaTime);
        var vy = Mathf.Lerp(ld.yMultiplier.value, ldyMultiplier, 1.5f * Time.deltaTime);

        //  ld.xMultiplier = 0.5f;
        var vc = Vector2.Lerp(ld.center.value, ldcenter, 1.5f * Time.deltaTime);


        //vx=ldxMultiplier;
        //vy=ldyMultiplier;
        //vc=ldcenter;
        ld.xMultiplier.value = vx;
        ld.yMultiplier.value = vy;

        ld.center.value = vc;
    }
}
