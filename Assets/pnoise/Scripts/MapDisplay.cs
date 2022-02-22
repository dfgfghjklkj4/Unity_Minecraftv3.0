using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour 
{
	#region Variables

	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	#endregion

	public void DrawTexture (Texture2D texture)
	{
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
	}

	public void DrawMesh (MeshDataCustom meshData, Texture2D texture)
	{
		meshFilter.sharedMesh = meshData.GetMesh();
		meshRenderer.sharedMaterial.mainTexture = texture;
	}
}
