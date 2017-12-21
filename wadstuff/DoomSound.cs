using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class DoomSound {
	private byte[] data;
	private string name;

	private int formatNumber;
	private int sampleRate;
	private int sampleCount;
	private byte[] sampleData;

	public DoomSound(byte[] lumpData, string lumpName) {
		data = lumpData;
		name = lumpName;

		formatNumber = (int) BitConverter.ToInt16(data, 0);
		sampleRate = (int) BitConverter.ToInt16(data, 2);
		sampleCount = (int) BitConverter.ToInt32(data, 4) - 32;

		sampleData = new byte[sampleCount];
		Debug.Log(name);
		Debug.Log(formatNumber);
		Debug.Log(sampleRate);
		Debug.Log(sampleCount);

		Buffer.BlockCopy(data, 24, sampleData, 0, sampleCount);

	}

	public AudioClip ToAudioClip() {
		AudioClip output = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
		float[] audioData = new float[sampleCount];
		for (int i = 0; i < sampleCount; i++) {
			audioData[i] = ((float) sampleData[i] / 128.0f) - 1.0f;
		}
		output.SetData(audioData, 0);
		return output;
	}
}
