# DoomUnity

# what?

Various tools for working with Doom WADs and levels in Unity.
Also a Doom engine, called NaSTY (Not a Sourceport, Thank You)

# why?

Because it doesn't already exist. There are various cool applications for such a set of tools but personally I just like the idea of letting potential games of mine use doom levels.

# how?

Download a zip of the project from the code or release section and extract the DoomUnity folder contained within into your Unity project, or create a new folder in your Unity project called "DoomUnity" and clone the repository there.

You will also need to [download the SharpZip DLL](https://github.com/icsharpcode/SharpZipLib/releases) and import it into your Unity project for PK3 support. At some point I will try to include this in the source, or make it optional.

Here are some commands you can try once everything is imported:

`WadFile wad = new WadFile(path_to_wad);`
`MapData map = new MapData(wad, "MAP01");`
`Texture2D impSprite = DoomGraphic.BuildPatch("TROOA1", wad);`

# Can I play doom in this yet?

Kinda? you can run around levels and look at monsters!

To get the engine running in Unity, just pop a gameobject in an empty scene and add the `GameSetup` component, and attach the `VanillaPlayer` prefab to the player prefab option.
You can enter and commandline arguments here, too. 

You will need a Doom IWAD! Currently supports Doom 1, Doom 2, Final Doom, Freedoom and Chex Quest.

To run extra PWADs, add `-file path_to_wads` to the commandline parameters. 

For Midi, you must have a soundfont file in the root directory, and merge in a wad with MIDI files.
The IWADS use MUS files and those are not supported yet.

# Building the engine wad

To run the engine stuff, you'll need to generate `nasty.wad` and put that in the root folder of your project (just below Assets).

To build the engine wad: you need to have python 3, omgifol, and pillow

`pip install omgifol`  
`pip install pillow`

you will want to change the path to the root directory of the project or build.
then just run `python3 build_wad.py` to generate the engine wad.

(If you cba, you can just download one of the releases and pick out the nasty.wad from there. It won't be updated to the current source but might not be a problem)
