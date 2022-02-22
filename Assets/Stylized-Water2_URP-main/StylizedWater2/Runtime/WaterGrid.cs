﻿using System;
using System.Collections;
using System.Collections.Generic;
using StylizedWater2;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace StylizedWater2
{
    [ExecuteInEditMode]
    [AddComponentMenu("Stylized Water 2/Water Grid")]
    public class WaterGrid : MonoBehaviour
    {
        [Header("Appearance")]
        [Tooltip("Material used on the tile meshes")]
        public Material material;
        
        [Tooltip("When not in play-mode, the water will follow the scene-view camera position.")]
        public bool followSceneCamera = false;
        [Tooltip("If enabled, the object with the \"MainCamera\" will be assigned as the follow target when entering play mode")]
        public bool autoAssignCamera;
        [Tooltip("The grid will follow this Transform's position on the XZ axis. Ideally set to the camera's transform.")]
        public Transform followTarget;
        
        [Header("Grid")]
        [Tooltip("Scale of the entire grid in the length and width")]
        public float scale = 500f;
        [Range(0.15f, 10f)] 
        [Tooltip("Distance between vertices, rather higher than lower")]
        public float vertexDistance = 2f;
        [Min(1)]
        public int rowsColumns = 4;
        
        [HideInInspector]
        public int m_rowsColumns = 4;
        private float tileSize;
        private WaterObject m_waterObject = null;
        private Mesh mesh;
        [SerializeField]
        [HideInInspector]
        private List<WaterObject> objects = new List<WaterObject>();
        private Transform actualFollowTarget;
        private Vector3 targetPosition;

        private void Start()
        {
            if (autoAssignCamera) followTarget = Camera.main ? Camera.main.transform : followTarget;
        }
        
#if UNITY_EDITOR
        private void OnEnable()
        {
            UnityEditor.SceneView.duringSceneGui += OnSceneGUI;

            m_rowsColumns = rowsColumns;
            if(objects.Count == 0) Recreate();
        }
#endif

#if UNITY_EDITOR
        private void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
        }
#endif

        void Update()
        {
            if (Application.isPlaying) actualFollowTarget = followTarget;

            if (actualFollowTarget)
            {
                targetPosition = actualFollowTarget.transform.position;

                targetPosition = SnapToGrid(targetPosition, vertexDistance);
                targetPosition.y = this.transform.position.y;
                this.transform.position = targetPosition;
            }
        }

        public void Recreate()
        {
            rowsColumns = Mathf.Max(rowsColumns, 1);
            
            tileSize = Mathf.Max(1f, scale / rowsColumns);
            mesh = WaterMesh.Create(WaterMesh.Shape.Rectangle, tileSize, Mathf.FloorToInt(tileSize / vertexDistance), tileSize);

            bool requireRecreate = (m_rowsColumns != rowsColumns) || objects.Count < (rowsColumns * rowsColumns);
            if (requireRecreate) m_rowsColumns = rowsColumns;

            //Only destroy/recreate objects if grid subdivision has changed
            if (requireRecreate && objects.Count > 0)
            {
                foreach (WaterObject obj in objects)
                {
                    if (obj) DestroyImmediate(obj.gameObject);
                }
                objects.Clear();
            }

            int index = 0;
            for (int x = 0; x < rowsColumns; x++)
            {
                for (int z = 0; z < rowsColumns; z++)
                {
                    if (requireRecreate)
                    {
                        m_waterObject = WaterObject.New(material, mesh);
                        objects.Add(m_waterObject);

                        m_waterObject.transform.parent = this.transform;
                        m_waterObject.gameObject.layer = 4;

                        m_waterObject.name = "WaterTile_x" + x + "z" + z;
                    }
                    else
                    {
                        m_waterObject = objects[index];
                        m_waterObject.AssignMesh(mesh);
                        m_waterObject.AssignMaterial(material);
                    }

                    m_waterObject.transform.localPosition = GridLocalCenterPosition(x, z);

                    index++;
                }
            }
            
        }

        private Vector3 GridLocalCenterPosition(int x, int z)
        {
            return new Vector3(x * tileSize - ((tileSize * (rowsColumns)) * 0.5f) + (tileSize * 0.5f), 0f,
                z * tileSize - ((tileSize * (rowsColumns)) * 0.5f) + (tileSize * 0.5f));
        }

        private static Vector3 SnapToGrid(Vector3 position, float cellSize)
        {
            return new Vector3(SnapToGrid(position.x, cellSize), SnapToGrid(position.y, cellSize), SnapToGrid(position.z, cellSize));
        }

        private static float SnapToGrid(float position, float cellSize)
        {
            return Mathf.FloorToInt(position / cellSize) * (cellSize) + (cellSize * 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.5f);
            
            for (int x = 0; x < rowsColumns; x++)
            {
                for (int z = 0; z < rowsColumns; z++)
                {
                    Vector3 pos = transform.TransformPoint(GridLocalCenterPosition(x, z));
                   
                    Gizmos.DrawWireCube(pos, new Vector3(tileSize, 0f, tileSize));
                }
            }
        }

#if UNITY_EDITOR
        private void OnSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (followSceneCamera)
            {
                actualFollowTarget = sceneView.camera.transform;
                Update();
            }
            else
            {
                actualFollowTarget = null;
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WaterGrid))]
    public class CreateWaterGridInspector : Editor
    {
        private WaterGrid script;
        private int vertexCount;

        private void OnEnable()
        {
            script = (WaterGrid) target;
            script.m_rowsColumns = script.rowsColumns;
        }
        
        public override void OnInspectorGUI()
        {
            vertexCount = Mathf.FloorToInt(((script.scale / script.rowsColumns) / script.vertexDistance) * ((script.scale / script.rowsColumns) / script.vertexDistance));
            if(vertexCount > 65535)
            {
                EditorGUILayout.HelpBox("Vertex count of individual tiles is too high. Increase the vertex distance, decrease the grid scale, or add more rows/columns", MessageType.Error);
            }
            
            EditorGUI.BeginChangeCheck();
            
            if(script.material == null) EditorGUILayout.HelpBox("A material must be assigned", MessageType.Error);
            
            base.OnInspectorGUI();
            
            //Executed here since objects can't be destroyed from OnValidate
            if (EditorGUI.EndChangeCheck())
            {
                script.Recreate();
            }
        }
    }
#endif
}