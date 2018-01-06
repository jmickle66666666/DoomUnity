using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings {

	public delegate void SettingsUpdate();
	public static event SettingsUpdate settingsUpdateListener;

	public static Dictionary<string, string> data = new Dictionary<string, string>();

	public static void Init() {
		// Default settings
	}

	public static void SaveSettings() {

	}

	public static void LoadSettings() {

	}

	public static string Set(string key, string value, bool write = true) {
		key = key.ToUpper();
		if (data.ContainsKey(key)) {
			data[key] = value;
			if (settingsUpdateListener != null) settingsUpdateListener();
			return value;
		} else {
			if (write) {
				data.Add(key, value);
				if (settingsUpdateListener != null) settingsUpdateListener();
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
	
}
