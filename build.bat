@echo off
rem Build KohaneTimeViewer.exe with the C# compiler bundled in Windows (.NET Framework 4.8)
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /codepage:65001 /optimize+ /out:KohaneTimeViewer.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll KohaneTimeViewer.cs
echo.
if errorlevel 1 (
  echo BUILD FAILED
) else (
  echo BUILD OK: KohaneTimeViewer.exe
)
pause
