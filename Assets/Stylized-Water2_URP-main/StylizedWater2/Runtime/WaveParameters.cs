using System;
using UnityEngine;

namespace StylizedWater2
{
    /// <summary>
    /// Helper class to retrieve and store a water material's wave settings
    /// </summary>
    [Serializable]
    public class WaveParameters
    {
        private const string WavesKeyword = "_WAVES";
        
        private static int _WaveDistance = Shader.PropertyToID("_WaveDistance");
        private static int _WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        private static int _WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static int _WaveSteepness = Shader.PropertyToID("_WaveSteepness");
        private static int _WaveCount = Shader.PropertyToID("_WaveCount");
        private static int _WaveDirection = Shader.PropertyToID("_WaveDirection");
        private static int _AnimationParams = Shader.PropertyToID("_AnimationParams");
        
        /// <summary>
        /// XY: Direction
        /// Z: Speed multiplier
        /// </summary>
        public Vector4 animationParams;
        public int count;
        public float distance;
        public float speed;
        public float height;
        public float steepness;
        public Vector4 direction;
        
        public static bool WavesEnabled(Material waterMat)
        {
            if (!waterMat) return false;
            
            return waterMat.IsKeywordEnabled(WavesKeyword);
        }

        public static float GetMaxWaveHeight(Material mat)
        {
            return mat.GetFloat(_WaveHeight);
        } 

        public void Update(Material waterMat)
        {
            animationParams = waterMat.GetVector(_AnimationParams);
            speed = waterMat.GetFloat(_WaveSpeed);
            distance = waterMat.GetFloat(_WaveDistance);
            steepness = waterMat.GetFloat(_WaveSteepness);
            height = waterMat.GetFloat(_WaveHeight);
            count = waterMat.GetInt(_WaveCount);
            direction = waterMat.GetVector(_WaveDirection);
        }
        
        public void SetAsGlobal()
        {
            Shader.SetGlobalVector(_AnimationParams, animationParams);
            Shader.SetGlobalFloat(_WaveSpeed, speed);
            Shader.SetGlobalFloat(_WaveDistance, distance);
            Shader.SetGlobalFloat(_WaveSteepness, steepness);
            Shader.SetGlobalFloat(_WaveHeight, height);
            Shader.SetGlobalFloat(_WaveCount, count);
            Shader.SetGlobalVector(_WaveDirection, direction);
        }
        
        public void Apply(Material mat)
        {
            mat.SetVector(_AnimationParams, animationParams);
            mat.SetFloat(_WaveSpeed, speed);
            mat.SetFloat(_WaveDistance, distance);
            mat.SetFloat(_WaveSteepness, steepness);
            mat.SetFloat(_WaveHeight, height);
            mat.SetInt(_WaveCount, count);
            mat.SetVector(_WaveDirection, direction);
        }
    }
}