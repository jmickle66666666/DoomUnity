# DoomUnity

## What?

Various tools for working with Doom WADs and levels in Unity.
Also a Doom engine, called NaSTY (Not a Sourceport, Thank You)

## Why?

Because it doesn't already exist. There are various cool applications for such a set of tools but personally I just like the idea of letting potential games of mine use doom levels.

## How?

Download a zip of the project from the code or release section and extract the DoomUnity folder contained within into your Unity project, or create a new folder in your Unity project called "DoomUnity" and clone the repository there.

You will also need to [download the SharpZip DLL](https://github.com/icsharpcode/SharpZipLib/releases) and import it into your Unity project for PK3 support. At some point I will try to include this in the source, or make it optional.

Here are some commands you can try once everything is imported:

```
WadFile wad = new WadFile(path_to_wad);
MapData map = new MapData(wad, "MAP01");
Texture2D impSprite = DoomGraphic.BuildPatch("TROOA1", wad);
```

# Can I play Doom WADs with this yet?

Kinda? you can run around levels and look at monsters! You will of course first need a Doom IWAD to play with. The currently supported IWADs are:

- Doom
- Doom II
- Final Doom
- Freedoom
- Chex Quest

## Playing with the compiled build

1. Download the latest nasty build (Linux, OSx or Win) from the [Releases](https://github.com/jmickle66666666/DoomUnity/releases) section and extract it.
2. Add one or more supported IWADS to the root of the nasty build folder.
3. Run nasty.exe. If there is more than one IWAD in the folder you will be prompted to select one.

## Playing in Unity with source code

To run the engine in Unity, you'll first need to generate the engine wad and put that in the root folder of your Unity project (just below Assets). See "Building the engine wad" below for more info. Alternatively, you can just download a [build](https://github.com/jmickle66666666/DoomUnity/releases) and extract the nasty.wad from there. It won't be updated to the latest source, but this might not be a problem.

To start the engine in Unity, just create an empty gameobject in a new scene and add the `GameSetup` component to it. Then add the `VanillaPlayer` prefab to the "Player Prefab" field.

You can also enter commandline arguments here in the `Editor Args` field. For example,
to run extra PWADs, add `-file path_to_wads` to the commandline parameters.

For Midi, you must have a soundfont file in the root directory, and merge in a wad with MIDI files.
The IWADS use MUS files and those are not supported yet.

# Building the engine wad

To build the engine wad (nasty.wad): you'll need python 3, omgifol, and pillow.

`pip install omgifol`  
`pip install pillow`

You will want to change the path to the root directory of the project or build.
then just run `python3 build_wad.py` to generate the engine wad.

# License

Free Public License 1.0.0

Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

# Other licenses

SimpleJSON.cs has an MIT License, and is outlined [in the source](https://github.com/jmickle66666666/DoomUnity/blob/master/External/SimpleJSON.cs)

The FirstPersonDrifter controller and the [C# Digital Audio Synth](https://github.com/n-yoda/unity-midi/tree/master/Assets/UnityMidi/CSharpSynth) do not include licenses, and are NOT covered by any license provided elsewhere in this project.
