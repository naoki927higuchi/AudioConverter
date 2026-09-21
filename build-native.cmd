@echo off
setlocal
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSINSTALL=%%i"
if not defined VSINSTALL exit /b 1
call "%VSINSTALL%\VC\Auxiliary\Build\vcvars64.bat"
if errorlevel 1 exit /b %errorlevel%
cd /d "%~dp0"
if not exist native mkdir native
cl /nologo /utf-8 /std:c++17 /EHsc /W4 /MT /LD Command.cpp /Fonative\Command.obj /Fenative\AudioConverterCommand.dll /link /DEF:Command.def ole32.lib shell32.lib shlwapi.lib runtimeobject.lib uuid.lib
exit /b %errorlevel%
