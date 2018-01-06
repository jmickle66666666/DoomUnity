using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Settings {

	public delegate void SettingsUpdate();
	public static event SettingsUpdate settingsUpdateListener;

	public static Dictionary<string, string> data = new Dictionary<string, string>();

	public static void Init() {
		LoadSettings();
	}

	public static void SaveSettings() {
		string output = "";
		foreach (KeyValuePair<string, string> e in data) {
			output += e.Key + " " + e.Value + "\n";
		}
		File.WriteAllText("settings.cfg", output);
	}

	public static void LoadSettings() {
		if (File.Exists("settings.cfg")) {
			string[] readData = File.ReadAllLines("settings.cfg");
			for (int i = 0; i < readData.Length; i++) {
				string[] s = readData[i].Split(' ');
				data.Add(s[0], s[1]);
			}
		}
	}

	public static string Set(string key, string value, bool write = true) {
		key = key.ToUpper();
		if (data.ContainsKey(key)) {
			data[key] = value;
			if (settingsUpdateListener != null) settingsUpdateListener();
			SaveSettings();
			return value;
		} else {
			if (write) {
				data.Add(key, value);
				if (settingsUpdateListener != null) settingsUpdateListener();
				SaveSettings();
				return value;
			} else {
				return "Unknown key: "+key;
			}
		}
		
	}

	public static string Get(string key, string defaultValue) {
		key = key.ToUpper();
		if (data.ContainsKey(key)) {
			return data[key];
		} else {
			data.Add(key, defaultValue);
			if (settingsUpdateListener != null) settingsUpdateListener();
			SaveSettings();
			return defaultValue;
		}	
	}

	public static string Get(string key) {
		key = key.ToUpper();
		if (data.ContainsKey(key)) {
			return data[key];
		} else {
			return "Unknown key: "+key;
		}	
	}

	public static List<string> Autocomplete(string partialValue) {
		partialValue = partialValue.ToUpper();
		List<string> output = new List<string>();
		if (partialValue == "") return output;

		foreach (KeyValuePair<string, string> e in data) {
			if (e.Key.StartsWith(partialValue)) output.Add(e.Key);
		}

		return output;
	}
	
}
