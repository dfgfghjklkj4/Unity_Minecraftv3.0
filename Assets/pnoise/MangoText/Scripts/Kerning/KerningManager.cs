using System.Collections.Generic;
using System.Diagnostics;

//Manager that holds all the kerning tables that have been generated from XML files
//not to recalculate kerning table everytime we need it

namespace UnityEngine.UI
{
	public class KerningManager
	{
		List<KerningTable> kerningTables = new List<KerningTable>();
		Dictionary<string, int> nameToIdx = new Dictionary<string, int>();

		public KerningTable GetKerningTable(TextAsset XMLFile, Platform platform, Compression compression, out Stopwatch timer, out bool measured)
		{
			timer = new Stopwatch();
			measured = false;

			if (XMLFile == null)
			{
				Debug.LogWarning("XML File empty !");
				return null;
			}

			if (!XMLFileLoaded(XMLFile.name))
				AddKerningTable(XMLFile, platform, compression, timer, ref measured);

			return kerningTables[nameToIdx[XMLFile.name]];
		}

		private bool XMLFileLoaded (string XMLName)
		{
			return nameToIdx.ContainsKey(XMLName);
		}

		private void AddKerningTable (TextAsset XMLFile, Platform platform, Compression compression, Stopwatch timer, ref bool measured)
		{
			timer.Reset();
			timer.Start();
			KerningTable kerningTable = new KerningTable(XMLFile, platform, compression);
			timer.Stop();
			measured = true;

			nameToIdx.Add(XMLFile.name, kerningTables.Count);
			kerningTables.Add(kerningTable);
		}
	}

	public enum Platform
	{
		Unicode = 0,
		Macintosh = 1,
		Windows = 3,
		Custom = 4
	};

	public enum Compression
	{
		None,
		Regular,
		Super
	};
}

