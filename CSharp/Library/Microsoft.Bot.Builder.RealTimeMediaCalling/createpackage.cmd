@echo off
echo *** Building Microsoft.Bot.Builder.RealTimeMediaCalling
setlocal
setlocal enabledelayedexpansion
setlocal enableextensions
set errorlevel=0
mkdir ..\nuget
erase /s ..\nuget\Microsoft.Bot.Builder.RealTimeMediaCalling*nupkg
msbuild /property:Configuration=release Microsoft.Bot.Builder.RealTimeMediaCalling.csproj
for /f %%v in ('powershell -noprofile "(Get-Command .\bin\release\Microsoft.Bot.Builder.RealTimeMediaCalling.dll).FileVersionInfo.FileVersion"') do set version=%%v
.\packages\NuGet.CommandLine.3.5.0\tools\NuGet.exe pack Microsoft.Bot.Builder.RealTimeMediaCalling.nuspec -symbols -properties version=%version% -OutputDirectory ..\nuget
echo *** Finished building Microsoft.Bot.Builder.RealTimeMediaCalling