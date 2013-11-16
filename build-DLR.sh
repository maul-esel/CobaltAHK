#!/bin/bash

git clone https://github.com/IronLanguages/main.git IronDLR

cd IronDLR
git checkout ipy-2.7.4
cd ..

sed -i 's/444;//g' IronDLR/Solutions/Common.proj

cp DLR.Core.sln IronDLR/Runtime/DLR.Core.sln
xbuild /t:Build /p:Configuration=v2Release IronDLR/Runtime/DLR.Core.sln

ls IronDLR/bin/v2Release

mkdir -p CobaltAHK/bin/Debug
cp IronDLR/bin/v2Release/*.dll CobaltAHK/bin/Debug

mkdir -p CobaltAHK-app/bin/Debug
cp IronDLR/bin/v2Release/*.dll CobaltAHK-app/bin/Debug

mkdir -p Tests/bin/Debug
cp IronDLR/bin/v2Release/*.dll Tests/bin/Debug
