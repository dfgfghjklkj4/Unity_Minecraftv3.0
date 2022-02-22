#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.EventSystems;

//Creating MangoText in editor and drawing inspector for MangoText in editor

namespace UnityEngine.UI
{
	[CustomEditor(typeof(MangoText))]
	[CanEditMultipleObjects]
	public class MangoTextEditor : UnityEditor.UI.TextEditor
	{
		private SerializedProperty m_Text;
		private SerializedProperty m_PairAsset;
		private SerializedProperty m_FontData;
		private SerializedProperty m_UseKerning;
		private SerializedProperty m_UsingRichText;
		private SerializedProperty m_KerningScale;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_Text = serializedObject.FindProperty("m_Text");
			m_PairAsset = serializedObject.FindProperty("m_PairAsset");
			m_FontData = serializedObject.FindProperty("m_FontData");
			m_UseKerning = serializedObject.FindProperty("m_UseKerning");
			m_UsingRichText = serializedObject.FindProperty("m_UsingRichText");
			m_KerningScale = serializedObject.FindProperty("m_KerningScale");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_Text);
			EditorGUILayout.LabelField("Font and XML file", EditorStyles.boldLabel);
			EditorGUI.indentLevel = 1;
			EditorGUILayout.PropertyField(m_PairAsset);
			EditorGUI.indentLevel = 0;
			EditorGUILayout.PropertyField(m_FontData);

			EditorGUILayout.LabelField("Kerning", EditorStyles.boldLabel);
			EditorGUI.indentLevel = 1;
			EditorGUILayout.PropertyField(m_UseKerning);
			EditorGUILayout.PropertyField(m_UsingRichText);
			EditorGUILayout.PropertyField(m_KerningScale);
			EditorGUI.indentLevel = 0;

			GUILayout.Space(5);
			AppearanceControlsGUI();
			RaycastControlsGUI();

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("GameObject/UI/Mango Text")]
		public static void CreateMangoText()
		{
			GameObject parent = FindCanvas();

			GameObject go = new GameObject();
			MangoText mangoText = go.AddComponent<MangoText>();

			go.name = "Mango Text";
			go.transform.SetParent(parent.transform);
			go.transform.position = new Vector3(604f, 288f);
			mangoText.text = "Sample Text";
			mangoText.horizontalOverflow = HorizontalWrapMode.Overflow;

			// Register the creation in the undo system
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}

		private static GameObject FindCanvas()
		{
			Canvas canvas = null;
			GameObject selected = Selection.activeGameObject;

			if (selected != null)
			{
				canvas = selected.GetComponent<Canvas>();
				if (canvas != null)
					return selected;

				canvas = selected.GetComponentInParent<Canvas>();
				if (canvas != null)
					return selected;
			}

			canvas = FindObjectOfType<Canvas>();
			if (canvas != null)
				return canvas.gameObject;

			return CreateNewCanvas();
		}

		private static GameObject CreateNewCanvas()
		{
			GameObject go = new GameObject();

			Canvas canvas = go.AddComponent<Canvas>();
			go.AddComponent<CanvasScaler>();
			go.AddComponent<GraphicRaycaster>();

			go.name = "Canvas";
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			RectTransform rect = canvas.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(1208f, 576f);
			rect.position = new Vector3(604f, 288f);

			if (FindObjectOfType<EventSystem>() == null)
			{
				GameObject eventSystem = new GameObject();
				eventSystem.AddComponent<EventSystem>();
				eventSystem.AddComponent<StandaloneInputModule>();
				eventSystem.name = "EventSystem";
			}

			return go;
		}
	}
}
#endif
