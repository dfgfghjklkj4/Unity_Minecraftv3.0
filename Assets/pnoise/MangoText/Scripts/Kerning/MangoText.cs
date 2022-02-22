using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

//Custom text component with kerning possibility

namespace UnityEngine.UI
{
	[AddComponentMenu("UI/Mango Text")]	public class MangoText : Text
	{
		#region Variables

		private const string SupportedTagRegexPatterns = @"<b>|</b>|<i>|</i>|<size=.*?>|</size>|<color=.*?>|</color>|<material=.*?>|</material>";
		private KerningTable kerningTable;
		private static KerningManager kerningManager = new KerningManager();

		[SerializeField] private PairAsset m_PairAsset;
		[SerializeField] private bool m_UseKerning = true;
		[SerializeField] private bool m_UsingRichText = true;
		[SerializeField] private float m_KerningScale = 1.9f;

		[NonSerialized] private static readonly VertexHelper s_VertexHelper = new VertexHelper();

		bool measured = false;
		Stopwatch timer;

		public PairAsset pairAsset
		{
			get { return m_PairAsset; }
			set
			{
				m_PairAsset = value;
				if (m_PairAsset != null && font != m_PairAsset.fontFile)
					font = m_PairAsset.fontFile;
			}
		}

		public bool useKerning
		{
			get { return m_UseKerning; }
			set { m_UseKerning = value; }
		}

		public bool usingRichText
		{
			get { return m_UsingRichText; }
			set { m_UsingRichText = value; }
		}

		public float kerningScaleFactor
		{
			get { return m_KerningScale; }
			set { m_KerningScale = value; }
		}

		#endregion

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			if (pairAsset != null && pairAsset.fontFile != font)
				font = pairAsset.fontFile;
		}
#endif

		private void Update()
		{
			if (measured)
			{
				GameObject r = new GameObject();
				MangoText a = r.AddComponent<MangoText>();
				a.font = font;
				a.pairAsset = pairAsset;
				a.transform.SetParent(transform.parent);
				a.transform.position = transform.parent.position;
				a.fontSize = 100;
				a.color = Color.blue;
				a.verticalOverflow = VerticalWrapMode.Overflow;
				a.horizontalOverflow = HorizontalWrapMode.Overflow;
				a.text = timer.Elapsed.Minutes + ":" + timer.Elapsed.Seconds + ":" + timer.Elapsed.Milliseconds;
				a.alignment = TextAnchor.MiddleCenter;
				measured = false;
			}
		}

		#region UpdatingMesh

		protected override void UpdateGeometry ()
		{
			if (font != null)
			{
				if (useLegacyMeshGeneration)
					DoLegacyMeshGeneration();
				else
					DoMeshGeneration();
			}
		}

		private void DoMeshGeneration()
		{
			if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
				OnPopulateMesh(s_VertexHelper);
			else
				s_VertexHelper.Clear(); // clear the vertex helper so invalid graphics dont draw.

			var components = ListPool<Component>.Get();
			GetComponents(typeof(IMeshModifier), components);

			ModifyMesh(s_VertexHelper);
			for (var i = 0; i < components.Count; i++)
				((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);

			ListPool<Component>.Release(components);

			s_VertexHelper.FillMesh(workerMesh);
			canvasRenderer.SetMesh(workerMesh);
		}

		private void DoLegacyMeshGeneration()
		{
			if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
			{
#pragma warning disable 618
				OnPopulateMesh(workerMesh);
#pragma warning restore 618
			}
			else
			{
				workerMesh.Clear();
			}

			var components = ListPool<Component>.Get();
			GetComponents(typeof(IMeshModifier), components);

			for (var i = 0; i < components.Count; i++)
			{
#pragma warning disable 618
				((IMeshModifier)components[i]).ModifyMesh(workerMesh);
#pragma warning restore 618
			}

			ListPool<Component>.Release(components);
			canvasRenderer.SetMesh(workerMesh);
		}

		#endregion

		#region ModifyingMesh

		private void ModifyMesh(VertexHelper vh)
		{
			if (!this.IsActive())
				return;

			List<UIVertex> list = new List<UIVertex>();
			vh.GetUIVertexStream(list);

			Kern(list);

			vh.Clear();
			vh.AddUIVertexTriangleStream(list);
		}

		private void Kern(List<UIVertex> verts)
		{
			if (!IsActive() || !useKerning || pairAsset == null)
				return;

			if (kerningTable == null || kerningTable.name != pairAsset.XMLFile.name)
			{
				kerningTable = kerningManager.GetKerningTable(pairAsset.XMLFile, pairAsset.platform, pairAsset.compression, out timer, out measured);
				if (kerningTable == null)
					return;
			}

			bool isRichText = usingRichText && supportRichText;
			//int[] originalIdxs, actualFontSizes;
			//string[] lines = SplitToLines(text, isRichText, out originalIdxs, out actualFontSizes);

			int lastEnterIdx = -1;
			float kerningScale = .01f * kerningScaleFactor;

			MatchCollection matchCollection = null;
			Match match = null;
			Queue<int> Q = null;
			int matchCollectionIdx = 0;

			if (isRichText)
				matchCollection = Regex.Matches(text, SupportedTagRegexPatterns);

			while (lastEnterIdx < text.Length)
			{
				float lineOffset = CalculateLineOffset(lastEnterIdx + 1, text, matchCollection, isRichText, Q) * kerningScale * GetAlignmentFactor(alignment);

				if (lastEnterIdx + 1 > text.Length - 1 || text[lastEnterIdx + 1] == '\n')
				{
					++lastEnterIdx;
					continue;
				}

				float prefixOffset = 0f;
				MoveGlyph(verts, lastEnterIdx + 1, Vector3.right * (0f - lineOffset));

				int idx = lastEnterIdx + 1;
				uint leftGlyph = uint.MaxValue;
				int leftGlyphSize = 0;
				
				while (idx < text.Length && text[idx] != '\n')
				{
					if (isRichText && matchCollectionIdx < matchCollection.Count)
					{
						if (match == null)
							match = matchCollection[matchCollectionIdx];

						if (idx == match.Index)
						{
							if (IsSizeStartMatch(match.Value))
							{
								int newFontSize = StringToInt(match.Value.Substring(6, match.Value.Length - 7));

								if (Q == null)
									Q = new Queue<int>();
								Q.Enqueue(newFontSize);
							}
							else if (IsSizeEndMatch(match.Value))
								if (Q != null && Q.Count > 0)
									Q.Dequeue();

							idx += match.Length;
							match = null;
							++matchCollectionIdx;

							continue;
						}
					}

					uint rightGlyph = text[idx];
					int rightGlyphSize = fontSize;

					if (Q != null && Q.Count > 0)
						rightGlyphSize = Q.Peek();

					KerningPairValue kerningPairValue = kerningTable.GetKerningPair(leftGlyph, rightGlyph);

					prefixOffset += kerningPairValue.XAdvance1 * leftGlyphSize * kerningScale;
					MoveGlyph(verts, idx, Vector3.right * (prefixOffset - lineOffset));
					prefixOffset += kerningPairValue.XAdvance2 * rightGlyphSize * kerningScale;

					leftGlyph = rightGlyph;
					leftGlyphSize = rightGlyphSize;

					++idx;
				}

				lastEnterIdx = idx;
			}
			
			/*int glyphIdx = 0;

			for (int lineIndex = 0; lineIndex < lines.Length; ++lineIndex)
			{
				string line = lines[lineIndex];
				float[] prefixOffset = new float[line.Length + 1];
				float lineOffset = 0f;

				for (int i = 1; i < line.Length; ++i)
				{
					uint leftGlyph = line[i - 1];
					uint rightGlyph = line[i];

					KerningPairValue kernValue = kerningTable.GetKerningPair(leftGlyph, rightGlyph);

					float leftScaleFactor = actualFontSizes[glyphIdx + i - 1] * .01f * kerningScaleFactor;
					float rightScaleFactor = actualFontSizes[glyphIdx + i] * .01f * kerningScaleFactor;

					lineOffset += kernValue.XAdvance1 * leftScaleFactor + kernValue.XAdvance2 * rightScaleFactor;
					prefixOffset[i] += kernValue.XAdvance1 * leftScaleFactor;
					prefixOffset[i + 1] = prefixOffset[i] + kernValue.XAdvance2 * rightScaleFactor;
				}

				lineOffset *= GetAlignmentFactor(alignment);

				for (int i = 0; i < line.Length; ++i)
				{
					int originalIdx = originalIdxs[glyphIdx];
					Vector3 offset = Vector3.right * (prefixOffset[i] - lineOffset);

					if (6 * originalIdx + 5 > verts.Count - 1)
						return;

					for (int j = 0; j < 6; ++j)
					{
						int pos = 6 * originalIdx + j;
						UIVertex vert = verts[pos];
						vert.position += offset;
						verts[pos] = vert;
					}

					++glyphIdx;
				}

				++glyphIdx;
			}*/
		}

		private float CalculateLineOffset (int idx, string content, MatchCollection matchCollection, bool isRichText, Queue<int> Q)
		{
			Match match = null;
			int matchCollectionIdx = 0;

			float lineOffset = 0f;
			uint leftGlyph = uint.MaxValue;
			int leftGlyphSize = 0;

			while (idx < content.Length && content[idx] != '\n')
			{
				if (isRichText && matchCollectionIdx < matchCollection.Count)
				{
					if (match == null)
						match = matchCollection[matchCollectionIdx];

					if (idx == match.Index)
					{
						if (IsSizeStartMatch(match.Value))
						{
							int newFontSize = StringToInt(match.Value.Substring(6, match.Value.Length - 7));

							if (Q == null)
								Q = new Queue<int>();
							Q.Enqueue(newFontSize);
						}
						else if (IsSizeEndMatch(match.Value))
							if (Q != null && Q.Count > 0)
								Q.Dequeue();

						idx += match.Length;
						match = null;
						++matchCollectionIdx;

						continue;
					}
				}

				uint rightGlyph = content[idx];
				int rightGlyphSize = fontSize;

				if (Q != null && Q.Count > 0)
					rightGlyphSize = Q.Peek();

				KerningPairValue kerningPairValue = kerningTable.GetKerningPair(leftGlyph, rightGlyph);
				lineOffset += kerningPairValue.XAdvance1 * leftGlyphSize + kerningPairValue.XAdvance2 * rightGlyphSize;

				leftGlyph = rightGlyph;
				leftGlyphSize = rightGlyphSize;

				++idx;
			}

			return lineOffset;
		}

		private void MoveGlyph (List<UIVertex> verts, int idx, Vector3 offset)
		{
			if (6 * idx + 5 > verts.Count - 1)
				return;

			for (int i = 0; i < 6; ++i)
			{
				int pos = 6 * idx + i;
				UIVertex vert = verts[pos];
				vert.position += offset;
				verts[pos] = vert;
			}
		}

		private string[] SplitToLines(string text, bool isRichText, out int[] originalIdxs, out int[] actualFontSizes)
		{
			originalIdxs = new int[text.Length];
			actualFontSizes = new int[text.Length];

			if (isRichText)
			{
				MatchCollection matchCollection = Regex.Matches(text, SupportedTagRegexPatterns);
				Match match = null;
				Queue<int> Q = new Queue<int>();
				int matchCollectionIdx = 0;

				for (int charIdx = 0, actualCharIdx = 0; charIdx < text.Length; ++charIdx, ++actualCharIdx)
				{
					if (matchCollectionIdx < matchCollection.Count)
					{
						if (match == null)
							match = matchCollection[matchCollectionIdx];

						if (match.Index == charIdx)
						{
							if (IsSizeStartMatch(match.Value))
							{
								string number = match.Value.Substring(6, match.Value.Length - 7);
								Q.Enqueue(StringToInt(number));
							}
							else if (IsSizeEndMatch(match.Value) && Q.Count > 0)
								Q.Dequeue();

							charIdx += match.Length - 1;
							--actualCharIdx;
							++matchCollectionIdx;
							match = null;

							continue;
						}
					}

					originalIdxs[actualCharIdx] = charIdx;

					if (Q.Count > 0)
						actualFontSizes[actualCharIdx] = Q.Peek();
					else
						actualFontSizes[actualCharIdx] = fontSize;
				}

				text = Regex.Replace(text, SupportedTagRegexPatterns, string.Empty);
			}
			else
			{
				for (int i = 0; i < text.Length; ++i)
				{
					originalIdxs[i] = i;
					actualFontSizes[i] = fontSize;
				}
			}

			return text.Split('\n');
		}

		private float GetAlignmentFactor(TextAnchor alignment)
		{
			switch (alignment)
			{
				case TextAnchor.LowerLeft:
				case TextAnchor.MiddleLeft:
				case TextAnchor.UpperLeft:
					return 0f;

				case TextAnchor.LowerCenter:
				case TextAnchor.MiddleCenter:
				case TextAnchor.UpperCenter:
					return 0.5f;

				case TextAnchor.LowerRight:
				case TextAnchor.MiddleRight:
				case TextAnchor.UpperRight:
					return 1f;
			}

			return 0f;
		}

		private bool IsSizeStartMatch (string match)
		{
			if (match.Length < 8)
				return false;

			string number = match.Substring(6, match.Length - 7);

			if (number.Length == 1 && number[0] == '0')
				return false;

			for (int i = 0; i < number.Length; ++i)
				if (!char.IsDigit(number[i]))
					return false;

			return true;
		}

		private bool IsSizeEndMatch (string match)
		{
			return match == "</size>";
		}

		private int StringToInt(string s)
		{
			return Convert.ToInt32(s, 10);
		}

		#endregion
	}
}
