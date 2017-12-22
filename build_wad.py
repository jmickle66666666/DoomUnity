import omg
from os import listdir
from os.path import isfile, join
import sys

wadfilespath = "enginewad/"
wad = omg.WAD()
files = [f for f in listdir(wadfilespath) if isfile(join(wadfilespath, f))]
for f in files:
	with open(wadfilespath+f, "rb") as file:
		wad.data[f[:f.find('.')]] = omg.Lump(file.read())

wad.to_file("../../../nasty.wad")