using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Text.RegularExpressions;

/*
Class for loading and pulling lumps from wad files
*/

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
				if ((int) array[i] != 0) _name += value[i];
			}
		}
	}
}

public class WadFile {

	public string type;
	public int numLumps;
	public int directoryPos;
	public List<DirectoryEntry> directory;
	public byte[] wadData;

	public byte[] GetLump(string name) {
		for (int i = 0; i < directory.Count; i++) {
			if (directory[i].name == name) return GetLump(directory[i]);
		}
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

	public int GetIndex(string name) {
		for (int i = 0; i < directory.Count; i++) {
			if (directory[i].name == name) return i;
		}
		return -1;
	}

	public DirectoryEntry GetEntry(string name) {
		for (int i = 0; i < directory.Count; i++) {
			if (directory[i].name == name) return directory[i];
		}
		return null;
	}

	public WadFile(string path) {
		wadData = File.ReadAllBytes(path);

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

	}

	public static string FixString(string input) {
		char[] array = input.ToCharArray();
		string output = "";
		for (int i = 0; i < 8; i++) {
			if ((int) array[i] != 0) output += input[i];
		}
		return output;
	}

	public static string GetString(byte[] data, int offset, int length = 8) {
		return FixString(new string(Encoding.ASCII.GetChars(data, offset, length)));
	}

}
