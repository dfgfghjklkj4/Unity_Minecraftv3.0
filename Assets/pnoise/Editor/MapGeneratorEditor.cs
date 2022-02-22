using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		MapGenerator mapGenerator = (MapGenerator)target;

		if (DrawDefaultInspector() && mapGenerator.autoUpdate)
			mapGenerator.GenerateMap();

		if (GUILayout.Button("Generate"))
			mapGenerator.GenerateMap();
	}
}
