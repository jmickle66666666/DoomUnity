using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using UnityEngine;

//using ICSharpCode.SharpZipLib.Zip;

/*
Class for loading and pulling lumps from wad files
*/

namespace WadTools {

	public class DirectoryEntry {
		public int position;
		public int size;
		private string _name;
		public string name {
			get { 
				return _name;
			}
			set {
				//_name = value;
				char[] array = value.ToCharArray();
				_name = "";
				for (int i = 0; i < 8; i++) {
					if ((int) array[i] == 0) {
						break;
					} else {
						_name += value[i];
					} 
				}
			}
		}
		public void SetName(string name) {
			_name = name;
		}
	}

	public enum DataType {
		MIDI,
		MUS,
		DoomGraphic,
		DoomFlat,
		PNG,
		Unknown
	}

	public class WadFile {

		public string type;
		public int numLumps;
		public int directoryPos;
		public List<DirectoryEntry> directory;
		public byte[] wadData;
		public TextureTable textureTable;
		public MultigenParser multigen;

		public DataType DetectType(string name) {
			byte[] lump = GetLump(name);
			if (lump[0] == Convert.ToByte('M') &&
				lump[1] == Convert.ToByte('T') &&
				lump[2] == Convert.ToByte('h') &&
				lump[3] == Convert.ToByte('d')) {
				return DataType.MIDI;
			}

			if (lump[0] == Convert.ToByte('M') &&
				lump[1] == Convert.ToByte('U') &&
				lump[2] == Convert.ToByte('S')) {
				return DataType.MUS;
			}

			if (lump[1] == Convert.ToByte('P') &&
				lump[2] == Convert.ToByte('N') &&
				lump[3] == Convert.ToByte('G')) {
				return DataType.PNG;
			}

			if (lump.Length == 4096) {
				return DataType.DoomFlat;
			}

			return DataType.Unknown;
		}

		public string GetLumpAsText(string name) {
			byte[] data = GetLump(name);
			return Encoding.UTF8.GetString(data);
		}

		public string GetLumpAsText(int index) {
			byte[] data = GetLump(index);
			return Encoding.UTF8.GetString(data);
		}

		public MemoryStream GetLumpAsMemoryStream(string name) {
			byte[] data = GetLump(name);
			return new MemoryStream(data);
		}

		public byte[] GetLump(string name) {
			for (int i = directory.Count - 1; i >= 0; i--) {
				if (directory[i].name == name) return GetLump(directory[i]);
			}
			Debug.LogError("Can't find lump: "+name);
			return null;
		}

		public byte[] GetLump(int index) {
			return GetLump(directory[index]);
		}

		public byte[] GetLump(int start, int length) {
			byte[] output = new byte[length];
			Buffer.BlockCopy(wadData, start, output, 0, length);
			return output;
		}

		public byte[] GetLump(DirectoryEntry entry) {
			return GetLump(entry.position, entry.size);
		}

		// Used for sprites, get sprites with multiple parts of strings
		public Sprite GetSprite(string spriteName, string spriteFrame) {
			for (int i = directory.Count - 1; i >= 0; i--) {
				if (directory[i].name.StartsWith(spriteName) && 
					(directory[i].name.EndsWith(spriteFrame) || directory[i].name.Substring(4,2) == spriteFrame)
				) { 
					string dirName = directory[i].name;
					bool flipped = dirName.EndsWith(spriteFrame) && dirName.Length > 6;
					return new DoomGraphic(GetLump(directory[i])).ToSprite(flipped);
				}
			}
			Debug.LogError("Couldn't find sprite: "+spriteName + spriteFrame);
			return null;
		}

		public bool ContainsSpriteLump(string spriteName, string spriteFrame) {
			for (int i = directory.Count - 1; i >= 0; i--) {
				if (directory[i].name.StartsWith(spriteName) && 
					(directory[i].name.EndsWith(spriteFrame) || directory[i].name.Substring(4,2) == spriteFrame)
				) { 
					return true;
				}
			}
			return false;
		}

		// Get all lumps with the same name
		public List<byte[]> GetLumps(string name) {
			List<byte[]> output = new List<byte[]>();
			for (int i = 0; i < directory.Count; i++) {
				if (directory[i].name == name) output.Add(GetLump(i));
			}
			return output;
		}

		public void AddLump(string name, byte[] data) {
			DirectoryEntry entry = new DirectoryEntry();
			entry.SetName(name);
			entry.size = data.Length;
			entry.position = wadData.Length;
			
			byte[] newWadData = new byte[wadData.Length + data.Length];
			Buffer.BlockCopy(wadData, 0, newWadData, 0, wadData.Length);
			Buffer.BlockCopy(data, 0, newWadData, wadData.Length, data.Length);
			wadData = newWadData;

			directory.Add(entry);
		}

		public int GetIndex(string name) {
			for (int i = directory.Count - 1; i >= 0; i--) {
				if (directory[i].name == name) return i;
			}
			return -1;
		}

		public bool Contains(string name) {
			for (int i = 0; i < directory.Count; i++) {
				if (directory[i].name == name) return true;
			}
			return false;
		}

		public DirectoryEntry GetEntry(string name) {
			for (int i = directory.Count - 1; i >= 0; i--) {
				if (directory[i].name == name) return directory[i];
			}
			return null;
		}

		public void Merge(string wadPath) {
			Debug.Log("Merging "+wadPath);
			Merge(new WadFile(wadPath));
		}

		public void Merge(WadFile wad) {

			if (wad.wadData == null) return;

			numLumps += wad.numLumps;
			
			for (int i = 0; i < wad.directory.Count; i++) {
				wad.directory[i].position += wadData.Length;
			}

			if (wad.textureTable != null) {
				if (textureTable == null) {
					textureTable = wad.textureTable;
				} else {
					textureTable.Merge(wad.textureTable);
				}
			}

			directory.AddRange(wad.directory);

			byte[] newWadData = new byte[wadData.Length + wad.wadData.Length];
			Buffer.BlockCopy(wadData, 0, newWadData, 0, wadData.Length);
			Buffer.BlockCopy(wad.wadData, 0, newWadData, wadData.Length, wad.wadData.Length);
			wadData = newWadData;
		}

		public WadFile(byte[] data) {
			string testString = new string(Encoding.ASCII.GetChars(data, 0, 4));

			if (testString == "PWAD" || testString == "IWAD") {
				LoadWad(data);
			} else if (testString.Substring(0,2) == "PK") {
				// LoadPK3(data);
			}
		}

		public WadFile(string path) {
			byte[] data = File.ReadAllBytes(path);
			string testString = new string(Encoding.ASCII.GetChars(data, 0, 4));

			if (testString == "PWAD" || testString == "IWAD") {
				LoadWad(data);
			} else if (testString.Substring(0,2) == "PK") {
				// LoadPK3(data);
			}
		}

		public void LoadWad(byte[] wadData) {
			this.wadData = wadData;
			type = new string(Encoding.ASCII.GetChars(wadData, 0, 4));

			numLumps = (int) BitConverter.ToUInt32(wadData,4);
			directoryPos = (int) BitConverter.ToUInt32(wadData, 8);

			directory = new List<DirectoryEntry>();

			for (int i = directoryPos; i < directoryPos + (numLumps * 16); i += 16) {

				DirectoryEntry de = new DirectoryEntry() {
					position = (int) BitConverter.ToUInt32(wadData, i),
					size = (int) BitConverter.ToUInt32(wadData, i + 4),
					name = new string(Encoding.ASCII.GetChars(wadData, i + 8, 8))
				};

				directory.Add(de);
			}

			SetupTextures();
		}

		public void LoadWad(string filepath) {
			LoadWad(File.ReadAllBytes(filepath));
		}

		// public void LoadPK3(string filepath) {
		// 	FileStream fileStream = File.OpenRead(filepath);
		// 	LoadPK3(fileStream);
		// }

		// public void LoadPK3(byte[] data) {
		// 	MemoryStream stream = new MemoryStream(data);
		// 	LoadPK3(stream);
		// }

		// public void LoadPK3(Stream streamData) {
		// 	byte[] fileData = new byte[streamData.Length];
		// 	streamData.Read(fileData, 0, (int)streamData.Length);
		// 	ZipFile zip = new ZipFile(streamData);

		// 	wadData = new byte[0];
		// 	directory = new List<DirectoryEntry>();

		// 	for (int i = 0; i < zip.Count; i++) {
		// 		if(zip[i].IsFile) { 
		// 			Stream stream = zip.GetInputStream(zip[i]);
		// 			byte[] outBuffer = new byte[zip[i].Size];
		// 			stream.Read(outBuffer, 0, (int)zip[i].Size);
		// 			stream.Close();
		// 			string name = zip[i].Name;
		// 			name = Path.GetFileName(name);
		// 			name = Path.GetFileNameWithoutExtension(name);
		// 			//Debug.Log(name);
		// 			AddLump(name.ToUpper(), outBuffer);
		// 		}
		// 	}

			
		// }

		public void SetupTextures() {
			if (Contains("PNAMES")) {
				PatchTable pnames = new PatchTable(GetLump("PNAMES"));
				textureTable = new TextureTable(GetLump("TEXTURE1"), pnames);
				if (Contains("TEXTURE2")) {
					textureTable.Add(GetLump("TEXTURE2"), pnames);
				}
			}
		}

		public static string ByteRead(byte[] bytes) {
			string output = "";
			for (int i = 0; i < bytes.Length; i++) {
				output += (char) bytes[i];
			}
			return output;
		}

		public static string FixString(string input) {
			char[] array = input.ToCharArray();
			string output = "";
			for (int i = 0; i < 8; i++) {
				if ((int) array[i] == 0) break;
				output += input[i];
			}
			return output;
		}

		public static string GetString(byte[] data, int offset, int length = 8) {
			return FixString(new string(Encoding.ASCII.GetChars(data, offset, length)));
		}

		public string GetMD5() {
			byte[] md5 = MD5.Create().ComputeHash(wadData);
			StringBuilder sBuilder = new StringBuilder();
			for (int i = 0; i < md5.Length; i++) {
				sBuilder.Append(md5[i].ToString("x2"));
			}
			return sBuilder.ToString();
		}

		public static string GetMD5(string path) {
			byte[] data = File.ReadAllBytes(path);
			byte[] md5 = MD5.Create().ComputeHash(data);
			StringBuilder sBuilder = new StringBuilder();
			for (int i = 0; i < md5.Length; i++) {
				sBuilder.Append(md5[i].ToString("x2"));
			}
			return sBuilder.ToString();
		}

		

	}

}
