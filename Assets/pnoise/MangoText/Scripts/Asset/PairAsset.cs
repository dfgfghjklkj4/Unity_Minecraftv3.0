using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

//This script holds data for font => XML file correlance for MangoText class

namespace UnityEngine.UI
{
	public class PairAsset : ScriptableObject
	{
		public Font fontFile;
		public TextAsset XMLFile;
		public Platform platform = Platform.Windows;
		public Compression compression = Compression.None;

	#if UNITY_EDITOR
		[MenuItem("Assets/Create/Kerning/Pair Asset")]
		private static void Create()
		{
			// get unique selected path
			string assetPath = AssetDatabase.GenerateUniqueAssetPath(GetPath() + "Pair Asset.asset");

			//force to create in Resources/Kerning
			/*if (!Directory.Exists("Assets/Resources"))
				Directory.CreateDirectory("Assets/Resources");
			if (!Directory.Exists("Assets/Resources/Kerning"))
				Directory.CreateDirectory("Assets/Resources/Kerning");
			string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Kerning/Pair Asset.asset");*/

			// create the main asset
			var mainAsset = CreateInstance<PairAsset>();
			AssetDatabase.CreateAsset(mainAsset, assetPath);

			// reimport & select
			AssetDatabase.ImportAsset(assetPath);
			Selection.activeObject = mainAsset;
		}

		private static string GetPath ()
		{
			if (Selection.activeObject == null)
				return "Assets/";

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == string.Empty)
				return "Assets/";

			if (Path.GetExtension(path) != string.Empty)
				path = path.Replace("/" + Path.GetFileName(path), "");
			return path + "/";
		}
	#endif

	}
}

