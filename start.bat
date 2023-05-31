@echo off
cd TrafficSimulator\bin\Debug\net6.0-windows
start TrafficSimulator.exe
timeout /t 5
cd ..\..\..\..\CarProcess\bin\Debug\net6.0-windows
for /l %%x in (1, 1, 10) do (
start CarProcess.exe
timeout /t 1
)
cd ..\..\..\..\PedestrianProcess\bin\Debug\net6.0-windows
start PedestrianProcess.exe