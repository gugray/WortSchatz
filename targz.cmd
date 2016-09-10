IF EXIST wschatz.tar.gz DEL wschatz.tar.gz
IF EXIST wschatz.tar DEL wschatz.tar
CD Publish
7z a ..\wschatz.tar .\**
CD ..
7z a wschatz.tar.gz wschatz.tar
DEL wschatz.tar
