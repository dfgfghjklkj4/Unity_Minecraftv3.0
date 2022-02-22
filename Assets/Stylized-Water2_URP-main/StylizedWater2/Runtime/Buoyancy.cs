//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//#undef MATHEMATICS

using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;

#if MATHEMATICS
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Vector4 = Unity.Mathematics.float4;
using Vector3 = Unity.Mathematics.float3;
using Vector2 = Unity.Mathematics.float2;
#endif

namespace StylizedWater2
{
    public static class Buoyancy
    {
        private static WaveParameters waveParameters = new WaveParameters();
        private static Material lastMaterial;
        
        private static readonly int CustomTimeID = Shader.PropertyToID("_CustomTime");
        private static readonly int TimeParametersID = Shader.PropertyToID("_TimeParameters");

        //OBSOLETE
        private const string WavesKeyword = "_WAVES";
      
        private static void GetMaterialParameters(Material mat)
        {
            waveParameters.Update(mat);
        }

        /// <summary>
        /// Returns the maximum possible wave height set on the material
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        [Obsolete("Use WaveParameters.GetMaxWaveHeight(Material) instead")]
        public static float GetMaxWaveHeight(Material mat)
        {
            if (!mat) return 0f;
            
            if (WavesEnabled(mat) == false) return 0f;

            return WaveParameters.GetMaxWaveHeight(mat);
        }

        public static bool UseCustomTime = false;
        private static float customTimeValue = 0f;

        /// <summary>
        /// See "Shader" section of documentation. This is used for network synchronized waves.
        /// </summary>
        /// <param name="value"></param>
        public static void SetCustomTime(float value)
        {
            customTimeValue = value;
            Shader.SetGlobalFloat(CustomTimeID, customTimeValue);
        }
        
        //Returns the same value as _TimeParameters.x
        private static float _TimeParameters
        {
            get
            {
                if (UseCustomTime) return customTimeValue;
                
#if UNITY_EDITOR
                return Application.isPlaying ? Time.time : Shader.GetGlobalVector(TimeParametersID).x;
#else
                return Time.time;
#endif
            }
        }
        
        private static float Dot2(Vector2 a, Vector2 b)
        {
#if MATHEMATICS
            return dot(a,b);
#else
            return Vector2.Dot(a, b);
#endif
        }
        
        private static float Dot3(Vector3 a, Vector3 b)
        {
#if MATHEMATICS
            return dot(a,b);
#else
            return Vector3.Dot(a, b);
#endif
        }
        
        private static float Dot4(Vector4 a, Vector4 b)
        {
#if MATHEMATICS
            return dot(a,b);
#else
            return Vector4.Dot(a, b);
#endif
        }
        
        private static float Sine(float t)
        {
#if MATHEMATICS
            return sin(t);
#else
            return Mathf.Sin(t);
#endif
        }
        
        private static float Cosine(float t)
        {
#if MATHEMATICS
            return cos(t);
#else
            return Mathf.Cos(t);
#endif
        }

        private static void Vector4Sin(ref Vector4 input, Vector4 a, Vector4 b)
        {
            input.x = Sine(a.x + b.x);
            input.y = Sine(a.y + b.y);
            input.z = Sine(a.z + b.z);
            input.w = Sine(a.w + b.w);
        }
        
        private static void Vector4Cosin(ref Vector4 input, Vector4 a, Vector4 b)
        {
            input.x = Cosine(a.x + b.x);
            input.y = Cosine(a.y + b.y);
            input.z = Cosine(a.z + b.z);
            input.w = Cosine(a.w + b.w);
        }

        private static Vector4 MultiplyVec4(Vector4 a, Vector4 b)
        {
#if MATHEMATICS
            return a * b;
#else
            return Vector4.Scale(a, b);
#endif
        }

        private static Vector4 sine;
        private static Vector4 cosine;
        private static Vector4 dotABCD;
        private static Vector4 AB;
        private static Vector4 CD;
        private static Vector4 direction1;
        private static Vector4 direction2;
        private static Vector4 TIME;
        private static Vector2 planarPosition;
        
        private static Vector4 amp = new Vector4(0.3f, 0.35f, 0.25f, 0.25f);
        private static Vector4 speed = new Vector4(1.2f, 1.375f, 1.1f, 1);
        private static Vector4 dir1 = new Vector4(0.3f, 0.85f, 0.85f, 0.25f);
        private static Vector4 dir2 = new Vector4(0.1f, 0.9f, -0.5f, -0.5f);
        private static Vector4 steepness = new Vector4(12f,12f,12f,12f);

        //Real frequency value per wave layer
        private static Vector4 frequency;
        private static Vector4 realSpeed;

        [Obsolete("Use WaveParameters.WavesEnabled(Material) instead")]
        public static bool WavesEnabled(Material waterMat)
        {
            return waterMat.IsKeywordEnabled(WavesKeyword);
        }
        
        [Obsolete("Use the function with a 4th bool parameter to indicate if the water material is dynamic")]
        public static float SampleWaves(Vector3 position, WaterObject waterObject, float rollStrength, out UnityEngine.Vector3 normal)
        {
            return SampleWaves(position, waterObject.material, waterObject.transform.position.y, rollStrength, false, out normal);
        }
        
        [Obsolete("Use the function with a 4th bool parameter to indicate if the water material is dynamic")]
        public static float SampleWaves(Vector3 position, Material waterMat, float waterLevel, float rollStrength, out UnityEngine.Vector3 normal)
        {
            return SampleWaves(position, waterMat, waterLevel, rollStrength, false, out normal);
        }
        
        /// <summary>
        /// Returns a position in world-space, where a ray cast from the origin in the direction hits the (flat) water level height
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="waterLevel">Water level height in world-space</param>
        /// <returns></returns>
        public static Vector3 FindWaterLevelIntersection(Vector3 origin, Vector3 direction, float waterLevel)
        {
            float upDot = Dot3(direction, UnityEngine.Vector3.up);
            float angle = (Mathf.Acos(upDot) * 180f) / Mathf.PI;

            float depth = waterLevel - origin.y;
            //Distance from origin to water level along direction
            float hypotenuse = depth / Cosine(Mathf.Deg2Rad * angle);
            
            return origin + (direction * hypotenuse);
        }

        /// <summary>
        /// Faux-raycast against the water surface
        /// </summary>
        /// <param name="waterObject">Water object component, used to get the water material and level (height)</param>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="hit">Reference to a RaycastHit, hit point and normal will be set</param>
        public static void Raycast(WaterObject waterObject, Vector3 origin, Vector3 direction,  bool dynamicMaterial, out RaycastHit hit)
        {
            Raycast(waterObject.material, waterObject.transform.position.y, origin, direction, dynamicMaterial, out hit);
        }

        private static RaycastHit hit = new RaycastHit();
        /// <summary>
        /// Faux-raycast against the water surface
        /// </summary>
        /// <param name="waterMat">Material using StylizedWater2 shader</param>
        /// <param name="waterLevel">Height of the reference water plane.</param>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="hit">Reference to a RaycastHit, hit point and normal will be set</param>
        public static void Raycast(Material waterMat, float waterLevel, Vector3 origin, Vector3 direction, bool dynamicMaterial, out RaycastHit hit)
        {
            Vector3 samplePos = FindWaterLevelIntersection(origin, direction, waterLevel);

            float waveHeight = SampleWaves(samplePos, waterMat, waterLevel, 1f, dynamicMaterial, out var normal);
            samplePos.y = waveHeight;

            hit = Buoyancy.hit;
            hit.normal = normal;
            hit.point = samplePos;
        }

        /// <summary>
        /// Given a position in world-space, returns the wave height and normal
        /// </summary>
        /// <param name="position">Sample position in world-space</param>
        /// <param name="waterObject">Water object component, used to get the water material and level (height)</param>
        /// <param name="rollStrength">Multiplier for the the normal strength</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="normal">Output upwards normal vector, perpendicular to the wave</param>
        /// <returns>Wave height, in world-space.</returns>
        public static float SampleWaves(Vector3 position, WaterObject waterObject, float rollStrength, bool dynamicMaterial, out UnityEngine.Vector3 normal)
        {
            return SampleWaves(position, waterObject.material, waterObject.transform.position.y, rollStrength, dynamicMaterial, out normal);
        }
        
        /// <summary>
        /// Given a position in world-space, returns the wave height and normal
        /// </summary>
        /// <param name="position">Sample position in world-space</param>
        /// <param name="waterMat">Material using StylizedWater2 shader</param>
        /// <param name="waterLevel">Height of the reference water plane.</param>
        /// <param name="rollStrength">Multiplier for the the normal strength</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="normal">Output upwards normal vector, perpendicular to the wave</param>
        /// <returns>Wave height, in world-space.</returns>
        public static float SampleWaves(Vector3 position, Material waterMat, float waterLevel, float rollStrength, bool dynamicMaterial, out UnityEngine.Vector3 normal)
        {
            Profiler.BeginSample("Buoyancy sampling");

            if(!waterMat)
            {
                normal = UnityEngine.Vector3.up;
                return waterLevel;
            }
            
            //Fetch the material's wave parameters, so the exact calculations can be mirrored
            if (lastMaterial == null || lastMaterial.Equals(waterMat) == false)
            {
                GetMaterialParameters(waterMat);
                lastMaterial = waterMat;
            }
            if(dynamicMaterial || !Application.isPlaying) GetMaterialParameters(waterMat);

            Vector4 freq = new Vector4(1.3f, 1.35f, 1.25f, 1.25f) * (1-waveParameters.distance) * 3f;
            
            direction1 = MultiplyVec4(dir1, waveParameters.direction);
            direction2 = MultiplyVec4(dir2, waveParameters.direction);

            Vector3 offsets = Vector3.zero;
            frequency = freq;
            realSpeed.x *= waveParameters.animationParams.x;
            realSpeed.y *= waveParameters.animationParams.y;
            realSpeed.z *= waveParameters.animationParams.x;
            realSpeed.w *= waveParameters.animationParams.y;
            
            for (int i = 0; i <= waveParameters.count; i++)
            {
                float t = 1f+((float)i / (float)waveParameters.count);

                frequency *= t;

                AB.x = steepness.x * waveParameters.steepness * direction1.x * amp.x;
                AB.y = steepness.x * waveParameters.steepness * direction1.y * amp.x;
                AB.z = steepness.x * waveParameters.steepness * direction1.z * amp.y;
                AB.w = steepness.x * waveParameters.steepness * direction1.w * amp.y;

                CD.x = steepness.z * waveParameters.steepness * direction2.x * amp.z;
                CD.y = steepness.z * waveParameters.steepness * direction2.y * amp.z;
                CD.z = steepness.w * waveParameters.steepness * direction2.z * amp.w;
                CD.w = steepness.w * waveParameters.steepness * direction2.w * amp.w;
                
                planarPosition.x = position.x;
                planarPosition.y = position.z;
                
                #if MATHEMATICS
                dotABCD.x = Dot2(direction1.xy, planarPosition) * frequency.x;
                dotABCD.y = Dot2(direction1.zw, planarPosition) * frequency.y;
                dotABCD.z = Dot2(direction2.xy, planarPosition) * frequency.z;
                dotABCD.w = Dot2(direction2.zw, planarPosition) * frequency.w;
                #else
                dotABCD.x = Dot2(new Vector2(direction1.x, direction1.y), planarPosition) * frequency.x;
                dotABCD.y = Dot2(new Vector2(direction1.z, direction1.w), planarPosition) * frequency.y;
                dotABCD.z = Dot2(new Vector2(direction2.x, direction2.y), planarPosition) * frequency.z;
                dotABCD.w = Dot2(new Vector2(direction2.z, direction2.w), planarPosition) * frequency.w;
                #endif
                
                TIME = (_TimeParameters * waveParameters.animationParams.z * waveParameters.speed * speed);

                sine.x = Sine(dotABCD.x + TIME.x);
                sine.y = Sine(dotABCD.y + TIME.y);
                sine.z = Sine(dotABCD.z + TIME.z);
                sine.w = Sine(dotABCD.w + TIME.w);

                cosine.x = Cosine(dotABCD.x + TIME.x);
                cosine.y = Cosine(dotABCD.y + TIME.y);
                cosine.z = Cosine(dotABCD.z + TIME.z);
                cosine.w = Cosine(dotABCD.w + TIME.w);
                
                offsets.x += Dot4(cosine, new Vector4(AB.x, AB.z, CD.x, CD.z));
                offsets.y += Dot4(sine, amp);
                offsets.z += Dot4(cosine, new Vector4(AB.y, AB.w, CD.y, CD.w));
            }
            
            rollStrength *=  Mathf.Lerp(0.001f, 0.1f, waveParameters.steepness);
            
            normal = new Vector3(-offsets.x * rollStrength * waveParameters.height, 2f, -offsets.z * rollStrength * waveParameters.height);
            
#if MATHEMATICS
            normal = normalize(normal);
#else
            normal = normal.normalized;
#endif

            //Average height
            offsets.y /= waveParameters.count;
            
            Profiler.EndSample();
            return (offsets.y * waveParameters.height) + waterLevel;
        }

        /// <summary>
        /// Checks if the position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
        /// </summary>
        /// <param name="position"></param>
        /// <param name="waterObject"></param>
        /// <returns></returns>
        public static bool CanTouchWater(Vector3 position, WaterObject waterObject)
        {
            if (!waterObject) return false;
            
            return position.y < (waterObject.transform.position.y + WaveParameters.GetMaxWaveHeight(waterObject.material));
        }
        
        /// <summary>
        /// Checks if the position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
        /// </summary>
        public static bool CanTouchWater(Vector3 position, Material waterMaterial, float waterLevel)
        {
            return position.y < (waterLevel + WaveParameters.GetMaxWaveHeight(waterMaterial));
        }
    }
}