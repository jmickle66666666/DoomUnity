

using System.Text;
using System;

namespace WadTools {
	public class Mus2Mid {
		private byte[] data;

		private enum MusEvent : byte {
			MUS_RELEASEKEY = 0x00,
			MUS_PRESSKEY = 0x10,
			MUS_PITCHWHEEL = 0x20,
			MUS_SYSTEMEVENT = 0x30,
			MUS_CHANGECONTROLLER = 0x40,
			MUS_SCOREEND = 0x60
		}

		private enum MidiEvent : byte {
			MIDI_RELEASEKEY = 0x80,
			MIDI_PRESSKEY = 0x90,
			MIDI_AFTERTOUCHKEY = 0xA0,
			MIDI_CHANGECONTROLLER = 0xB0,
			MIDI_CHANGEPATCH = 0xC0,
			MIDI_AFTERTOUCHCHANNEL = 0xD0,
			MIDI_PITCHWHEEL = 0xE0
		}

		private class MusHeader {
			public string id;
			public int scoreLength;
			public int scoreStart;
			public int primaryChannels;
			public int secondaryChannels;
			public int instrumentCount;
		}

		private MusHeader musheader;

		private byte[] midiHeader = new byte[22] {
			0x4d, 0x54, 0x68, 0x64,	// Main header
			0x00, 0x00, 0x00, 0x06, // Header size
			0x00, 0x00,				// MIDI type (0)
			0x00, 0x01,				// Number of tracks
			0x00, 0x46,				// Resolution
			0x4d, 0x54, 0x72, 0x6b,		// Start of track
			0x00, 0x00, 0x00, 0x00  // Placeholder for track length
		};

		// Constants
		private static int NUM_CHANNELS = 16;
		private static int MUS_PERCUSSION_CHAN = 15;
		private static int MIDI_PERCUSSION_CHAN = 9;
		private static int MIDI_TRACKLENGTH_OFS = 18;

		// Cached velocities
		private byte[] channelVelocities = new byte[16] {
			127, 127, 127, 127, 127, 127, 127, 127, 
			127, 127, 127, 127, 127, 127, 127, 127
		};

		// Timestamps between sequences of MUS events
		private int queuedtime = 0;

		// Counter for the length of the track
		private int tracksize;

		private byte[] controller_map = new byte[15] {
		    0x00, 0x20, 0x01, 0x07, 0x0A, 0x0B, 0x5B, 0x5D,
		    0x40, 0x43, 0x78, 0x7B, 0x7E, 0x7F, 0x79
		};

		private int[] channel_map = new int[NUM_CHANNELS];

		// Wrapper to simulate Slade's memchunk.write()
		int position = 0;
		byte[] output = new byte[0];
		private void WriteData(byte[] data) {
			byte[] newData = new byte[output.Length + data.Length];
			Buffer.BlockCopy(output, 0, newData, 0, output.Length);
			Buffer.BlockCopy(data, 0, newData, output.Length, data.Length);
			output = newData;
		}

		private void WriteByte(byte data) {
			WriteData(new byte[] {data});
		}

		private void WriteEventByte(MidiEvent midiEvent, int channel) {
			WriteByte((byte) ((byte)midiEvent | (byte)channel));
		}

		private void WriteDataByte(byte eventData, int mod = 0x7F) {
			WriteByte((byte)(eventData & mod));
		}

		private void WriteTime(int time)
		{
		    int buffer = time & 0x7F;
		    byte writeval;

		    while ((time >>= 7) != 0)
		    {
		        buffer <<= 8;
		        buffer |= ((time & 0x7F) | 0x80);
		    }

		    for (;;)
		    {
		        writeval = (byte)(buffer & 0xFF);

		        WriteByte(writeval);

		        ++tracksize;

		        if ((buffer & 0x80) != 0)
		            buffer >>= 8;
		        else
		        {
		            queuedtime = 0;
		            return;
		        }
		    }
		}

		// Write the end of track marker
		private void WriteEndTrack()
		{
		    byte[] endtrack = new byte[3] {0xFF, 0x2F, 0x00};
		    WriteTime(queuedtime);
		    WriteData(endtrack);
		    tracksize += 3;
		}

		private void WritePressKey(int channel, byte key, byte velocity)
		{
			WriteTime(queuedtime);
			
			WriteEventByte(MidiEvent.MIDI_PRESSKEY, channel);
			WriteDataByte(key);
			WriteDataByte(velocity);

			tracksize += 3;
		}

		private void WriteReleaseKey(int channel, byte key)
		{
			WriteTime(queuedtime);
			
			WriteEventByte(MidiEvent.MIDI_RELEASEKEY, channel);
			WriteDataByte(key);
			WriteByte((byte) 0);

			tracksize += 3;
		}

		private void WritePitchWheel(int channel, byte wheel)
		{
			WriteTime(queuedtime);

			WriteEventByte(MidiEvent.MIDI_PITCHWHEEL, channel);
			WriteDataByte(wheel);
			WriteDataByte((byte)(wheel >> 7));

			tracksize += 3;
		}

		private void WriteChangePatch(int channel, byte patch)
		{
			WriteTime(queuedtime);

			WriteEventByte(MidiEvent.MIDI_CHANGEPATCH, channel);
			WriteDataByte(patch);

			tracksize += 2;
		}

		private void WriteChangeController_Valued(int channel, byte control, byte val)
		{
			WriteTime(queuedtime);

			WriteEventByte(MidiEvent.MIDI_CHANGECONTROLLER, channel);
			WriteDataByte(control);
			WriteByte((byte) ((val & 0x80)!=0 ? 0x7F : val));

			tracksize += 3;
		}

		private void WriteChangeController_Valueless(int channel, byte control)
		{
			WriteChangeController_Valued(channel, control, (byte) 0);
		}

		private int AllocateMIDIChannel()
		{
			int result;
			int max;
			int i;

			max = -1;
			for (i = 0; i < NUM_CHANNELS; i++) {
				if (channel_map[i] > max) {
					max = channel_map[i];
				}
			}

			result = max + 1;

			if (result == MIDI_PERCUSSION_CHAN) {
				result += 1;
			}

			return result;
		}

		private int GetMIDIChannel(int mus_channel) {
			if (mus_channel == MUS_PERCUSSION_CHAN) {
				return MIDI_PERCUSSION_CHAN;
			} else {
				if (channel_map[mus_channel] == -1) {
					channel_map[mus_channel] = AllocateMIDIChannel();
				}
				return channel_map[mus_channel];
			}
		}

		private void ReadMusHeader() {
			musheader = new MusHeader() {
				id = new string(Encoding.ASCII.GetChars(data, 0, 4)),
				scoreLength = BitConverter.ToUInt16(data, 4),
				scoreStart = BitConverter.ToUInt16(data, 6),
				primaryChannels = BitConverter.ToUInt16(data, 8),
				secondaryChannels = BitConverter.ToUInt16(data, 10),
				instrumentCount = BitConverter.ToUInt16(data, 12)
			};
		}

		int pos = 0;

		byte GetByte() {
			byte output = data[pos];
			pos++;
			return output;
		}

		public Mus2Mid (byte[] data) {
			this.data = data;

			ReadMusHeader();

			pos = 0;

			byte eventDescriptor;
			int channel;
			int mus_event;

			byte key;
			byte controllernumber;
			byte controllervalue;

			int hitscoreend = 0;

			byte working;
			int timedelay;

			for (channel = 0; channel < NUM_CHANNELS; channel++) {
				channel_map[channel] = -1;
			}

			pos = musheader.scoreStart;

			WriteData(midiHeader);
			tracksize = 0;

			while (hitscoreend == 0) {
				while (hitscoreend == 0) {
					eventDescriptor = GetByte();
					channel = GetMIDIChannel(eventDescriptor & 0x0F);
					mus_event = eventDescriptor & 0x70;

					switch(mus_event) {
						case (int)MusEvent.MUS_RELEASEKEY:
							key = GetByte();
							WriteReleaseKey(channel, key);
							break;
						case (int)MusEvent.MUS_PRESSKEY:
							key = GetByte();
							if ((int)(key & 0x80) != 0) {
								channelVelocities[channel] = (byte) (GetByte() & 0x7F);
							}
							WritePressKey(channel, key, channelVelocities[channel]);
							break;
						case (int)MusEvent.MUS_SYSTEMEVENT:
							controllernumber = GetByte();
							if (controllernumber >= 10 && controllernumber <= 14) {
								WriteChangeController_Valueless(channel, controller_map[controllernumber]);
							}
							break;
						case (int)MusEvent.MUS_CHANGECONTROLLER:
							controllernumber = GetByte();
							controllervalue = GetByte();
							if (controllernumber == 0) {
								WriteChangePatch(channel, controllervalue);
							} else {
								if (controllernumber >=1 && controllernumber <= 9) {
									WriteChangeController_Valued(channel, controller_map[controllernumber], controllervalue);
								}
							}
							break;
						case (int)MusEvent.MUS_SCOREEND:
							hitscoreend = 1;
							break;
						default:
							break;
					}
					if ((eventDescriptor & 0x80) != 0) {
						break;
					}
				}

				if (hitscoreend == 0) {
					timedelay = 0;
					for (;;) {
						working = GetByte();
						timedelay = timedelay * 128 + (working & 0x7F);
						if ((working & 0x80) == 0) break;
					}
					queuedtime += timedelay;
				}

				WriteEndTrack();

				output[18+0] = (byte)((tracksize >> 24) & 0xff);
				output[18+1] = (byte)((tracksize >> 16) & 0xff);
				output[18+2] = (byte)((tracksize >> 8) & 0xff);
				output[18+3] = (byte)(tracksize & 0xff);
			}
		}

		public byte[] MidiData() {
			return output;
		}
	}
}