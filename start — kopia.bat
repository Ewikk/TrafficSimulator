if not DEFINED IS_MINIMIZED set IS_MINIMIZED=1 && start "" /min "%~dpnx0" %* && exit
@echo off

cd CarProcess\bin\Debug\net6.0-windows
for /l %%x in (1, 1, 15) do (
start CarProcess.exe
timeout /t 1
)
cd ..\..\..\..\PedestrianProcess\bin\Debug\net6.0-windows
start PedestrianProcess.exe
timeout /t 3
cd ..\..\..\..\TramProcess\bin\Debug\net6.0-windows
start TramProcess.exe
timeout /t 1
start TramProcess.exe
exit