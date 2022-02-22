using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
//Holds all the information about kerning pairs generated from XML file

namespace UnityEngine.UI
{
	public class KerningTable
	{
		#region Variables

		public string name;
		private XElement ttf;

		private Dictionary<string, int> keyMap;
		private Dictionary<uint, int> codeToIdx;

		private KerningPairValue[][] kerningTable;
		private Dictionary<int, KerningPairValue>[] kerningTables;
		private HashSet<KerningPair> kerningPairSet;

		private int glyphCount;
		private const int maxGlyphCount = 500;
		private int cnt = 0;
		public int Count
		{
			get; private set;
		}

		public StoreMode storeMode
		{
			get; private set;
		}

		#endregion

		#region KerningGeneration

		public KerningTable() { }

		public KerningTable(TextAsset file, Platform platform = Platform.Windows, Compression compression = Compression.None)
		{
			GenerateKerningFromFile(file, platform, compression);
		}

		public void GenerateKerningFromFile(TextAsset file, Platform platform, Compression compression)
		{
			if (file == null)
			{
				Debug.LogWarning("Error: XML file missing !");
				return;
			}

			name = file.name;
			ResetVariables();

			if (compression == Compression.None || compression == Compression.Regular)
			{
				if (!ParseFile(file))
					return;
		
				if (!MapKeys(platform))
					return;
			
				SetStoreMode();
				GenerateKerningTable();
			}
			else if (compression == Compression.Super)
			{
				StringReader reader = new StringReader(file.text);

				if (!MapKeys(reader, platform))
					return;

				SetStoreMode();
				GenerateKerningTable(reader);
			}

			Debug.Log("Success: " + Count + " kerning pairs added from " + name);
		}

		public void ResetKerning()
		{
			ResetVariables();
		}

		private void ResetVariables()
		{
			keyMap = new Dictionary<string, int>();
			codeToIdx = new Dictionary<uint, int>();

			kerningTable = null;
			kerningTables = null;
			kerningPairSet = null;

			ttf = null;

			Count = 0;
		}

		private void SetStoreMode()
		{
			if (glyphCount <= maxGlyphCount)
			{
				storeMode = StoreMode.Array;
				kerningTable = new KerningPairValue[glyphCount][];
			}
			else
			{
				storeMode = StoreMode.Mixed;
				kerningTable = new KerningPairValue[maxGlyphCount][];
				kerningTables = new Dictionary<int, KerningPairValue>[glyphCount];
			}
		}

		#endregion

		#region NoneAndRegularCompression

		private bool ParseFile(TextAsset file)
		{
			name = file.name;
			ttf = XDocument.Parse(file.text).Element("ttFont");

			return (ttf != null);
		}

		private bool MapKeys(Platform platform)
		{
			XElement cmap = ttf.Element("cmap");

			if (cmap == null)
			{
				Debug.LogWarning("Error: cmap table missing in XML File !");
				return false;
			}

			bool mapped = false;
			int platformID = (int)platform;

			foreach (XElement cmap_format in cmap.Elements())
			{
				XAttribute platformIDAttribute = cmap_format.Attribute("platformID");
				if (platformIDAttribute == null)
					continue;

				int platformIDValue = StringToInt(platformIDAttribute.Value);

				if (platformIDValue == platformID)
				{
					int idx = 0;

					foreach (XElement map in cmap_format.Elements("map"))
					{
						uint code = HexToUInt(map.Attribute("code").Value);
						string name = map.Attribute("name").Value;

						if (!keyMap.ContainsKey(name))
							keyMap.Add(name, idx);
						else
							Debug.Log(name + " -> " + "key already added to keyMap dictionary !");

						if (!codeToIdx.ContainsKey(code))
							codeToIdx.Add(code, idx);
						else
							Debug.Log(code + " -> " + "code already added to codeToIdx dictionary !");

						++idx;
					}

					glyphCount = idx;
					mapped = true;
				}
			}

			if (!mapped)
			{
				Debug.LogWarning("Key map not found !");
				return false;
			}

			return true;
		}

		private void GenerateKerningTable()
		{
			var kern = ttf.Element("kern");

			if (kern != null)
			{
				kern = kern.Element("kernsubtable");

				if (kern != null)
				{
					kerningPairSet = new HashSet<KerningPair>();

					foreach (var pair in kern.Elements("pair"))
					{
						string leftGlyphName = pair.Attribute("l").Value;
						string rightGlyphName = pair.Attribute("r").Value;

						int leftGlyphIdx, rightGlyphIdx;
						if (!keyMap.TryGetValue(leftGlyphName, out leftGlyphIdx) || !keyMap.TryGetValue(rightGlyphName, out rightGlyphIdx))
							continue;

						AddKernPair(new KerningPair(leftGlyphIdx, rightGlyphIdx));
					}

					Debug.Log(cnt);
				}
			}

			foreach (XElement pairPos in ttf.Elements("PairPos"))
			{
				int pairPosFormat = StringToInt(pairPos.Attribute("Format").Value);

				if (pairPosFormat == 1)
					HandleFormat1(pairPos);
				else if (pairPosFormat == 2)
					HandleFormat2(pairPos);
				else
					Debug.LogWarning("Unknown kerning format !");
			}
		}

		#region KerningFormat1

		private void HandleFormat1(XElement pairPos)
		{
			List<int> leftGlyphs = new List<int>();

			int valueFormat1 = StringToInt(pairPos.Element("ValueFormat1").Attribute("value").Value);
			int valueFormat2 = StringToInt(pairPos.Element("ValueFormat2").Attribute("value").Value);

			foreach (XElement glyph in pairPos.Element("Coverage").Elements())
			{
				string glyphName = glyph.Attribute("value").Value;
				int value;

				if (!keyMap.TryGetValue(glyphName, out value))
					value = int.MaxValue;

				leftGlyphs.Add(value);
			}

			foreach (XElement pairSet in pairPos.Elements("PairSet"))
			{
				int leftGlyphIndex = StringToInt(pairSet.Attribute("index").Value);

				foreach (XElement pairValueRecord in pairSet.Elements("PairValueRecord"))
				{
					string rightGlyphName = pairValueRecord.Element("SecondGlyph").Attribute("value").Value;
					int rightGlyphIdx;

					if (!keyMap.TryGetValue(rightGlyphName, out rightGlyphIdx))
						rightGlyphIdx = int.MaxValue;

					KerningPairValue kerningPairValue = new KerningPairValue();

					if (valueFormat1 != 0)
						kerningPairValue.XAdvance1 = StringToInt(pairValueRecord.Element("Value1").Attribute("XAdvance").Value);
					if (valueFormat2 != 0)
						kerningPairValue.XAdvance2 = StringToInt(pairValueRecord.Element("Value2").Attribute("XAdvance").Value);

					AddKerningPair(leftGlyphs[leftGlyphIndex], rightGlyphIdx, kerningPairValue);
				}
			}
		}

		#endregion

		#region KerningFormat2

		private void HandleFormat2(XElement pairPos)
		{
			HashSet<int> coverageList = new HashSet<int>();

			int classDef1Count = GetSizeOf(pairPos.Element("ClassDef1"));
			int classDef2Count = GetSizeOf(pairPos.Element("ClassDef2"));

			List<int>[] classDef1 = InitializeNewList(classDef1Count);
			List<int>[] classDef2 = InitializeNewList(classDef2Count);

			foreach (XElement glyph in pairPos.Element("Coverage").Elements("Glyph"))
			{
				string glyphName = glyph.Attribute("value").Value;
				int glyphIdx;

				if (!keyMap.ContainsKey(glyphName))
					continue;
				glyphIdx = keyMap[glyphName];

				coverageList.Add(glyphIdx);

				classDef1[0].Add(glyphIdx);
				classDef2[0].Add(glyphIdx);
			}

			int valueFormat1 = StringToInt(pairPos.Element("ValueFormat1").Attribute("value").Value);
			int valueFormat2 = StringToInt(pairPos.Element("ValueFormat2").Attribute("value").Value);

			AssignGlyphsToClass(pairPos.Element("ClassDef1"), classDef1, coverageList);
			AssignGlyphsToClass(pairPos.Element("ClassDef2"), classDef2, coverageList);

			foreach (XElement class1Record in pairPos.Elements("Class1Record"))
			{
				int classIndex1 = StringToInt(class1Record.Attribute("index").Value);

				foreach (XElement class2Record in class1Record.Elements("Class2Record"))
				{
					int classIndex2 = StringToInt(class2Record.Attribute("index").Value);

					KerningPairValue kerningPairValue = new KerningPairValue();

					if (valueFormat1 != 0)
						kerningPairValue.XAdvance1 = StringToInt(class2Record.Element("Value1").Attribute("XAdvance").Value);
					if (valueFormat2 != 0)
						kerningPairValue.XAdvance2 = StringToInt(class2Record.Element("Value2").Attribute("XAdvance").Value);

					AddKerningPair(classDef1[classIndex1], classDef2[classIndex2], kerningPairValue);
				}
			}
		}

		private int GetSizeOf(XElement ClassDef)
		{
			int size = 0;

			foreach (XElement classDef in ClassDef.Elements("ClassDef"))
			{
				int classID = StringToInt(classDef.Attribute("class").Value);
				if (classID > size)
					size = classID;
			}

			return size + 1;
		}

		private List<int>[] InitializeNewList(int size)
		{
			List<int>[] list = new List<int>[size];

			for (int i = 0; i < size; ++i)
				list[i] = new List<int>();

			return list;
		}

		private void AssignGlyphsToClass(XElement classDefElement, List<int>[] classDefList, HashSet<int> coverageList)
		{
			foreach (XElement classDef in classDefElement.Elements("ClassDef"))
			{
				string glyphName = classDef.Attribute("glyph").Value;
				int glyphIdx = int.MaxValue;

				if (!keyMap.ContainsKey(glyphName))
					continue;
				glyphIdx = keyMap[glyphName];

				int classID = StringToInt(classDef.Attribute("class").Value);

				if (coverageList.Contains(glyphIdx))
					classDefList[0].Remove(glyphIdx);

				classDefList[classID].Add(glyphIdx);
			}
		}

		#endregion

		#endregion

		#region SuperCompression

		private bool MapKeys (StringReader reader, Platform platform)
		{
			int cmapCount = StringToInt(reader.ReadLine());
			bool mapped = false;

			for (int i = 0; i < cmapCount; ++i)
			{
				int platformID = StringToInt(reader.ReadLine());
				int mapCount = StringToInt(reader.ReadLine());

				if (platformID != (int)platform)
				{
					for (int j = 0; j < 2 * mapCount; ++j)
						reader.ReadLine();
					continue;
				}

				glyphCount = mapCount;

				for (int j = 0; j < mapCount; ++j)
				{
					uint code = (uint)StringToInt(reader.ReadLine());
					string glyphName = reader.ReadLine();

					if (!keyMap.ContainsKey(glyphName))
						keyMap.Add(glyphName, j);
					else
						Debug.LogWarning(name + " -> " + "key already added to keyMap dictionary !");

					if (!codeToIdx.ContainsKey(code))
						codeToIdx.Add(code, j);
					else
						Debug.LogWarning(code + " -> " + "code already added to codeToIdx dictionary !");
				}

				mapped = true;
			}

			if (!mapped)
			{
				Debug.LogWarning("Error: Key map not found !");
				return false;
			}

			return true;
		}

		private void GenerateKerningTable (StringReader reader)
		{
			/*int kernPairCount = StringToInt(reader.ReadLine());
			
			if (kernPairCount > 0)
				kerningPairSet = new HashSet<KerningPair>();

			for (int i = 0; i < kernPairCount; ++i)
			{
				string leftGlyphName = reader.ReadLine();
				string rightGlyphName = reader.ReadLine();

				int leftGlyphIdx, rightGlyphIdx;
				if (keyMap.TryGetValue(leftGlyphName, out leftGlyphIdx) || !keyMap.TryGetValue(rightGlyphName, out rightGlyphIdx))
					continue;

				AddKernPair(new KerningPair(leftGlyphIdx, rightGlyphIdx));
			}*/

			int pairPosCount = StringToInt(reader.ReadLine());

			for (int i = 0; i < pairPosCount; ++i)
			{
				int format = StringToInt(reader.ReadLine());

				if (format == 1)
					SuperHandleFormat1(reader);
				else if (format == 2)
					SuperHandleFormat2(reader);
				else
					Debug.LogWarning("Unknown kerning format !");
			}
		}

		#region KerningFormat1

		private void SuperHandleFormat1(StringReader reader)
		{
			int coverageCount = StringToInt(reader.ReadLine());
			int[] leftGlyphs = new int[coverageCount];

			for (int i = 0; i < coverageCount; ++i)
			{
				string glyphName = reader.ReadLine();
				int glyphIdx;

				if (!keyMap.TryGetValue(glyphName, out glyphIdx))
					glyphIdx = int.MaxValue;

				leftGlyphs[i] = glyphIdx;
			}

			int valueFormat1 = StringToInt(reader.ReadLine());
			int valueFormat2 = StringToInt(reader.ReadLine());

			int pairSetCount = StringToInt(reader.ReadLine());
			for (int i = 0; i < pairSetCount; ++i)
			{
				int pairValueRecordCount = StringToInt(reader.ReadLine());
				int leftGlyphIdx = leftGlyphs[i];

				for (int j = 0; j < pairValueRecordCount; ++j)
				{
					string rightGlyphName = reader.ReadLine();
					int rightGlyphIdx;

					if (!keyMap.TryGetValue(rightGlyphName, out rightGlyphIdx))
						rightGlyphIdx = int.MaxValue;

					KerningPairValue kerningPairValue = new KerningPairValue();

					if (valueFormat1 != 0)
						kerningPairValue.XAdvance1 = StringToInt(reader.ReadLine());
					if (valueFormat2 != 0)
						kerningPairValue.XAdvance2 = StringToInt(reader.ReadLine());

					AddKerningPair(leftGlyphIdx, rightGlyphIdx, kerningPairValue);
				}
			}
		}

		#endregion

		#region KerningFormat2

		private void SuperHandleFormat2(StringReader reader)
		{
			int[][] classDef1 = ReadClass(reader);
			int[][] classDef2 = ReadClass(reader);

			int valueFormat1 = StringToInt(reader.ReadLine());
			int valueFormat2 = StringToInt(reader.ReadLine());

			int class1RecordCount = StringToInt(reader.ReadLine());
			for (int i = 0; i < class1RecordCount; ++i)
			{
				int class2RecordCount = StringToInt(reader.ReadLine());
				for (int j = 0; j < class2RecordCount; ++j)
				{
					KerningPairValue kerningPairValue = new KerningPairValue();

					if (valueFormat1 != 0)
						kerningPairValue.XAdvance1 = StringToInt(reader.ReadLine());
					if (valueFormat2 != 0)
						kerningPairValue.XAdvance2 = StringToInt(reader.ReadLine());

					AddKerningPair(classDef1[i], classDef2[j], kerningPairValue);
				}
			}
		}

		private int[][] ReadClass(StringReader reader)
		{
			int classCount = StringToInt(reader.ReadLine());
			int[][] classDef = new int[classCount][];

			for (int i = 0; i < classCount; ++i)
			{
				int classDefCount = StringToInt(reader.ReadLine());
				int[] tmp = new int[classDefCount];

				for (int j = 0; j < classDefCount; ++j)
				{
					string glyphName = reader.ReadLine();
					int glyphIdx;

					if (!keyMap.TryGetValue(glyphName, out glyphIdx))
						glyphIdx = int.MaxValue;

					tmp[j] = glyphIdx;
				}

				classDef[i] = tmp;
			}

			return classDef;
		}

		#endregion

		#endregion

		#region OtherFunctions

		private int StringToInt(string s)
		{
			return System.Convert.ToInt32(s, 10);
		}

		private uint HexToUInt(string s)
		{
			return System.Convert.ToUInt32(s, 16);
		}

		public void AddKerningPair (int leftGlyphIdx, int rightGlyphIdx, KerningPairValue kerningPairValue)
		{
			if ((kerningPairValue.XAdvance1 == 0 && kerningPairValue.XAdvance2 == 0) || leftGlyphIdx >= glyphCount || rightGlyphIdx >= glyphCount)
				return;

			if (IsKernPair(new KerningPair(leftGlyphIdx, rightGlyphIdx)))
				return;

			Debug.Log(leftGlyphIdx + " " + rightGlyphIdx);

			if (storeMode == StoreMode.Array)
			{
				if (kerningTable == null)
					return;

				if (kerningTable[leftGlyphIdx] == null)
					kerningTable[leftGlyphIdx] = new KerningPairValue[glyphCount];

				kerningTable[leftGlyphIdx][rightGlyphIdx] = kerningPairValue;
			}
			else if (storeMode == StoreMode.Mixed)
			{ 
				if (leftGlyphIdx < maxGlyphCount && rightGlyphIdx < maxGlyphCount)
				{
					if (kerningTable == null)
						return;

					if (kerningTable[leftGlyphIdx] == null)
						kerningTable[leftGlyphIdx] = new KerningPairValue[maxGlyphCount];

					kerningTable[leftGlyphIdx][rightGlyphIdx] = kerningPairValue;
				}
				else
				{
					if (kerningTables == null)
						return;

					if (kerningTables[leftGlyphIdx] == null)
						kerningTables[leftGlyphIdx] = new Dictionary<int, KerningPairValue>();
					else if (kerningTables[leftGlyphIdx].ContainsKey(rightGlyphIdx))
						return;

					kerningTables[leftGlyphIdx].Add(rightGlyphIdx, kerningPairValue);
				}
			}
			
			++Count;
		}

		private void AddKerningPair (int[] leftGlyphs, int[] rightGlyphs, KerningPairValue kerningPairValue)
		{
			if (kerningPairValue.XAdvance1 == 0 && kerningPairValue.XAdvance2 == 0)
				return;

			for (int i = 0; i < leftGlyphs.Length; ++i)
				for (int j = 0; j < rightGlyphs.Length; ++j)
					AddKerningPair(leftGlyphs[i], rightGlyphs[j], kerningPairValue);
		}

		private void AddKerningPair (List<int> leftGlyphs, List<int> rightGlyphs, KerningPairValue kerningPairValue)
		{
			if (kerningPairValue.XAdvance1 == 0 && kerningPairValue.XAdvance2 == 0)
				return;

			foreach (int leftGlyphIdx in leftGlyphs)
				foreach (int rightGlyphIdx in rightGlyphs)
					AddKerningPair(leftGlyphIdx, rightGlyphIdx, kerningPairValue);
		}

		private void AddKernPair (KerningPair kerningPair)
		{
			if (!kerningPairSet.Contains(kerningPair))
			{
				kerningPairSet.Add(kerningPair);
				++cnt;
			}
		}

		public KerningPairValue GetKerningPair (uint leftGlyph, uint rightGlyph)
		{
			if (codeToIdx == null)
				return new KerningPairValue();

			int leftGlyphIdx, rightGlyphIdx;
			if (!codeToIdx.TryGetValue(leftGlyph, out leftGlyphIdx) || !codeToIdx.TryGetValue(rightGlyph, out rightGlyphIdx))
				return new KerningPairValue();

			if (kerningPairSet != null && kerningPairSet.Contains(new KerningPair(leftGlyphIdx, rightGlyphIdx)))
				return new KerningPairValue();

			KerningPairValue kerningPairValue;

			if (storeMode == StoreMode.Array)
			{
				if (kerningTable == null || leftGlyphIdx >= glyphCount || rightGlyphIdx >= glyphCount || kerningTable[leftGlyphIdx] == null)
					return new KerningPairValue();

				return kerningTable[leftGlyphIdx][rightGlyphIdx];
			}
			else if (storeMode == StoreMode.Mixed)
			{
				if (leftGlyphIdx < maxGlyphCount && rightGlyphIdx < maxGlyphCount)
				{
					if (kerningTable == null || kerningTable[leftGlyphIdx] == null)
						return new KerningPairValue();

					return kerningTable[leftGlyphIdx][rightGlyphIdx];
				}
				else
				{
					if (kerningTables == null || !kerningTables[leftGlyphIdx].TryGetValue(rightGlyphIdx, out kerningPairValue))
						return new KerningPairValue();

					return kerningPairValue;
				}
			}
			else
				return new KerningPairValue();
		}

		private bool IsKernPair (KerningPair kerningPair)
		{
			if (kerningPairSet == null)
				return false;

			return (kerningPairSet.Contains(kerningPair));
		}

		#endregion
	}

	public struct KerningPair
	{
		public int leftGlyph;
		public int rightGlyph;

		public KerningPair (int leftGlyph_, int rightGlyph_)
		{
			leftGlyph = leftGlyph_;
			rightGlyph = rightGlyph_;
		}

		public override int GetHashCode ()
		{
			long mod = (long)int.MaxValue + 1L;

			long hash = 17L;
			hash = (hash * 23L + (long)leftGlyph) % mod;
			hash = (hash * 23L + (long)rightGlyph) % mod;

			return (int)hash;
		}
	}

	public struct KerningPairValue
	{
		private int xAdvance1;
		private int xAdvance2;
		private static float scale = 1 / 15.625f;

		public KerningPairValue (int xAdvance1_, int xAdvance2_)
		{
			xAdvance1 = xAdvance1_;
			xAdvance2 = xAdvance2_;
		}

		public float XAdvance1
		{
			get { return xAdvance1 * scale; }
			set { xAdvance1 = (int)value; }
		}

		public float XAdvance2
		{
			get { return xAdvance2 * scale; }
			set { xAdvance2 = (int)value; }
		}
	}

	public enum StoreMode
	{
		Array,
		Mixed
	};
}
