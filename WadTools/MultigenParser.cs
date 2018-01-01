using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine;

public class MultigenState {
	public string spriteName;
	public string spriteFrame;
	public string duration;
	public string action;
	public string nextState;
}

public class MultigenObject {
	public Dictionary<string, string> data;
	public static Dictionary<string, string> defaultData;
	public string name;

	public MultigenObject(string name) {
		if (defaultData != null) {
			data = new Dictionary<string, string>(defaultData);
		}
		this.name = name;
	}
}

public class MultigenParser {

	public Dictionary<string, MultigenObject> objects;
	public Dictionary<string, MultigenState> states;

	private string[] fieldNames = new string[] {
		"doomednum",
		"spawnstate",
		"spawnhealth",
		"seestate",
		"seesound",
		"reactiontime",
		"attacksound",
		"painstate",
		"painchance",
		"painsound",
		"meleestate",
		"missilestate",
		"deathstate",
		"xdeathstate",
		"deathsound",
		"speed",
		"radius",
		"height",
		"mass",
		"damage",
		"activesound",
		"flags",
		"raisestate"
	};

	public MultigenObject GetObjectByDoomedNum(int doomednum) {
		foreach (KeyValuePair<string, MultigenObject> entry in objects) {
			if (entry.Value.data["doomednum"] == doomednum.ToString()) {
				return entry.Value;
			}
		}
		return null;
	}

	public MultigenParser(string data) {
		objects = new Dictionary<string, MultigenObject>();
		states = new Dictionary<string, MultigenState>();

		// Step 1: strip all comments.
		// Comments are anything between a ';' and a newline
		string comments = @";(.*?)\r?\n";
		data = Regex.Replace(data, comments, "");

		// Step 2: create whitespace-delimited set of tokens
		string[] tokens = data.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
		string currentObject = "";

		// Step 3: step through tokens, processing as needed
		for (int i = 0; i < tokens.Length; i++) {
			// Default state
			if (tokens[i] == "$") {

				i++;
				if (tokens[i] == "+") {
					currentObject = NewUniqueName();
					objects.Add(currentObject, new MultigenObject(currentObject));
				} else if (tokens[i] == "DEFAULT") {
					currentObject = "DEFAULT";
					MultigenObject.defaultData = new Dictionary<string, string>();
				} else {
					currentObject = tokens[i];
					objects.Add(currentObject, new MultigenObject(currentObject));
				}

			} else if (IsFieldName(tokens[i])) {

				if (currentObject == "DEFAULT") {
					MultigenObject.defaultData.Add(tokens[i], tokens[i + 1]);
				} else {
					objects[currentObject].data[tokens[i]] = tokens[i + 1];
				}
				i++;

			} else if (tokens[i].StartsWith("S_")) {

				MultigenState newState = new MultigenState() {
					spriteName = tokens[i+1],
					spriteFrame = tokens[i+2],
					duration = tokens[i+3],
					action = tokens[i+4],
					nextState = tokens[i+5]
				};

				states.Add(tokens[i], newState);
				i += 5;

			} else {
				Debug.LogError("Unexpected token: "+tokens[i]);
				Debug.Log("Current object: "+currentObject);
			}
		}

		/*
		Parsing behaviour

		Default state:
			$ -> New multigen object. Object created with DEFAULT settings if it exists.
			matches a field name -> Set field data
			S_* -> New state with name of token
			EOF -> end
			else -> error
		
		New multigen object:
			+ -> New unique name added for object. return to default
			else -> Token is object name. return to default

		Set field data:
			any -> data is set as token for field in current object. return to default

		New state:
			next -> token is state sprite name
			next -> token is state sprite frame (optional * for fullbright)
			next -> token is state sprite duration (tics)
			next -> token is state action
			next -> token is state nextstate
			next -> optional?
			next -> optional?
			return to default.
		*/
	}

	private bool IsFieldName(string input) {
		for (int i = 0; i < fieldNames.Length; i++) {
			if (input == fieldNames[i]) return true;
		}
		return false;
	}

	private string NewUniqueName() {
		int i = 0;
		while (IsObjectKey("Z"+i.ToString())) {
			i++;
		}
		return "Z"+i.ToString();
	}

	private bool IsObjectKey(string input) {
		foreach (KeyValuePair<string, MultigenObject> entry in objects) {
			if (entry.Key == input) return true; 
		}
		return false;
	}

}
