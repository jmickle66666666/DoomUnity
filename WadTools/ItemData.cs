using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SimpleJSON;

namespace WadTools {
    public class ItemInfo {
        public string bonus = "None";
        public int amount = 1;
        public string message = "DEFAULT_ITEM";
        public string sound = "DSITEMUP";
    }

    public class ItemData {
        static Dictionary<string, ItemInfo> data;
        static ItemInfo defaultItem;

        public static void Load(string jsonData) {
            data = new Dictionary<string, ItemInfo>();
            JSONNode dat = JSON.Parse(jsonData);
            foreach (KeyValuePair<string, JSONNode> entry in dat) {
                ItemInfo item = new ItemInfo();
                if (entry.Value["bonus"] != null) item.bonus = entry.Value["bonus"];
                if (entry.Value["amount"] != null) item.amount = entry.Value["amount"];
                if (entry.Value["message"] != null) item.message = entry.Value["message"];
                if (entry.Value["sound"] != null) item.sound = entry.Value["sound"];
                data[entry.Key] = item;
            }

            defaultItem = new ItemInfo();
        }

        public static ItemInfo Get(string key) {
            if (data.ContainsKey(key)) {
                return data[key];
            } else {
                Debug.LogError("No itemdata: "+key);
                return defaultItem;
            }
        }
    }
    

}