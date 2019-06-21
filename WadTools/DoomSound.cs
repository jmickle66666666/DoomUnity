using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
namespace WadTools {
	public class DoomSound {
		private byte[] data;
		private string name;

		// private int formatNumber;
		private int sampleRate;
		private int sampleCount;
		private byte[] sampleData;

		public DoomSound(byte[] lumpData, string lumpName) {
			data = lumpData;
			name = lumpName;

			// formatNumber = (int) BitConverter.ToInt16(data, 0);
			sampleRate = (int) BitConverter.ToInt16(data, 2);
			sampleCount = (int) BitConverter.ToInt32(data, 4) - 32;

			sampleData = new byte[sampleCount];

			Buffer.BlockCopy(data, 24, sampleData, 0, sampleCount);

		}

		public AudioClip ToAudioClip() {
			AudioClip output = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
			float[] audioData = new float[sampleCount];
			for (int i = 0; i < sampleCount; i++) {
				audioData[i] = ((float) sampleData[i] / 128.0f) - 1.0f;
			}
			output.SetData(audioData, 0);
			// output.preloadAudioData;
			return output;
		}

		static Dictionary<string, AudioClip> soundCache;

		public static void PlaySoundAtPoint(WadFile wad, string name, Vector3 point) {
			if (soundCache == null) {
				soundCache = new Dictionary<string, AudioClip>();
			}

			AudioClip clip;
			if (soundCache.ContainsKey(name)) {
				clip = soundCache[name];
			} else {
				clip = new DoomSound(wad.GetLump(name), name).ToAudioClip();
				soundCache.Add(name, clip);
			}

			AudioSource.PlayClipAtPoint(clip, point);
		}
	}
}
