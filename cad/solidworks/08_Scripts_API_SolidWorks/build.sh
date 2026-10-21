#!/bin/bash
# usage: build.sh Name.cs  -> Name.exe
SWD="C:/Program Files/SOLIDWORKS Corp/SOLIDWORKS"
cd "$(dirname "$0")"
/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe -nologo -platform:x64 -out:"${1%.cs}.exe" \
  -r:"$SWD/SolidWorks.Interop.sldworks.dll" -r:"$SWD/SolidWorks.Interop.swconst.dll" "$1" Common.cs PB.cs Xf.cs
