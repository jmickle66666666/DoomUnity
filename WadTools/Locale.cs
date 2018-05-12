using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SimpleJSON;

namespace WadTools {
    public class Locale {
        
        static Dictionary<string, string> data;

        public static void Load(string localeData) {
            data = new Dictionary<string, string>();
            JSONNode json = JSON.Parse(localeData);
            foreach (KeyValuePair<string, JSONNode> entry in json) {
                data.Add(entry.Key, entry.Value);
            }
        }

        public static string Get(string key) {
            return data[key];
        }

    }
}