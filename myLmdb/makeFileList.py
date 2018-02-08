# -*- coding: utf-8 -*-
"""
Recording all same suffix files with absolute paths in one list(.txt)
"""
import os
import sys
import time
from sys import argv
print time.strftime('%Y-%m-%d %H:%M:%S', time.localtime())

fileList = "FileList.txt"
if 3 != len(argv):
    print("Please input srcRoot(abs route) and file sort.")
    print("File sort such as: png, txt, jpg, etc.")
    sys.exit(0)
else:
    srcRoot = argv[1]
    fileSort = argv[2]
    sortLength = len(fileSort)

if not os.path.exists(srcRoot):
    print("No Source!!!")
    sys.exit(0)
else:
    targetFile = srcRoot + os.path.sep + fileSort + fileList
    print "WRITE:"
    print targetFile
    with open(targetFile, 'w') as fw:
        for rt, dirs, files in os.walk(srcRoot):
            for name in files:
                if len(name) - sortLength == name.find(fileSort):
                    fw.write(os.path.join(rt, name))
                    fw.write(os.linesep)
                else:
                    continue

print time.strftime('%Y-%m-%d %H:%M:%S', time.localtime())