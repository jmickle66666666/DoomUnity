# DoomUnity

# what?

Various tools for working with Doom WADs and levels in Unity.

# why?

Because it doesn't already exist. There are various cool applications for such a set of tools but personally I just like the idea of letting potential games of mine use doom levels.

I guess I'm kind of making a new Doom engine with this stuff now.

# how?

Drop the "DoomUnity" folder into your project and try some stuff out!

`WadFile wad = new WadFile(path_to_wad);`
`MapData map = new MapData(wad, "MAP01");`
`Texture2D impSprite = DoomGraphic.BuildPatch("TROOA1", wad);`

And various other stuff. It's probably still gonna be a bit messy while I'm developing it!

# when?

Whenever I get to it, and am inspired to do so.