@echo off
set folder="C:\Users\xXJDr\AppData\Local\Diner_Smash"
IF EXIST "%folder%" (
    cd /d %folder%
    for /F "delims=" %%i in ('dir /b') do (rmdir "%%i" /s/q || del "%%i" /s/q)
    echo The contents of %folder% were erased.
    goto END	
)
echo The directory %folder% was not erased.
:END


