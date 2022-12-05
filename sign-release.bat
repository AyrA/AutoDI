@ECHO OFF
SETLOCAL
PUSHD "%~dp0"

CD AutoDI\bin\Release\net6.0\publish
FOR %%F IN (*.nupkg) DO (
dotnet nuget sign "%%F" --certificate-fingerprint b479a834f1eda6cf291b9a8420d959f498e8d9cb --timestamper http://timestamp.digicert.com
)
FOR %%F IN (AyrA.*.DLL) DO (
CALL C:\Bat\sign.bat "%%F" AyrA.AutoDI
)
POPD
ENDLOCAL
