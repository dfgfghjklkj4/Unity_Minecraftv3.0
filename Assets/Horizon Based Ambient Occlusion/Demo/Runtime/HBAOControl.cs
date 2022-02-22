using UnityEngine;

namespace HorizonBasedAmbientOcclusion
{
    public class HBAOControl : MonoBehaviour
    {
        public HBAO hbao;
        public UnityEngine.UI.Slider aoRadiusSlider;

        public void Start()
        {
            hbao.SetDebugMode(HBAO.DebugMode.Disabled);
            hbao.SetAoRadius(aoRadiusSlider.value);
        }

        public void ToggleShowAO()
        {
            if (hbao.generalSettings.debugMode != HBAO.DebugMode.Disabled)
                hbao.SetDebugMode(HBAO.DebugMode.Disabled);
            else
                hbao.SetDebugMode(HBAO.DebugMode.AOOnly);
        }

        public void UpdateAoRadius()
        {
            hbao.SetAoRadius(aoRadiusSlider.value);
        }
    }
}
