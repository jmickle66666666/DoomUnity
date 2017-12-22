import omg
from os import listdir
from os.path import isfile, join
import sys
from PIL import Image

wadfilespath = "enginewad/"
wad = omg.WAD()
files = [f for f in listdir(wadfilespath) if isfile(join(wadfilespath, f))]
for f in files:
	ext = f[f.find('.')+1:]
	if ext == "png":
		graphic = omg.Graphic()
		graphic.from_Image(Image.open(wadfilespath+f))
		lump = graphic
	else:
		with open(wadfilespath+f, "rb") as file:
			lump = omg.Lump(file.read())
			
	wad.data[f[:f.find('.')]] = lump

wad.to_file("../../../nasty.wad")