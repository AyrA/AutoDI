@ECHO OFF
SETLOCAL
PUSHD "%~dp0"

CD AutoDI\bin\Release\net8.0\publish

REM find latest package file name
FOR %%F IN (*.nupkg) DO (
SET NUGET=%%F
)

REM Sign DLL files
FOR %%F IN (AyrA.*.DLL) DO (
CALL C:\Bat\sign.bat "%%F" AyrA.AutoDI
)
REM Replace files in nuget package with signed copies
"C:\Program Files\7-Zip\7z.exe" d "%NUGET%" .signature.p7s
"C:\Program Files\7-Zip\7z.exe" a "%NUGET%" AyrA.*.dll
REM Sign nuget package
dotnet nuget sign "%NUGET%" --certificate-fingerprint b479a834f1eda6cf291b9a8420d959f498e8d9cb --timestamper http://timestamp.digicert.com
PAUSE
POPD
ENDLOCAL
