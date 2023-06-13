cd CarProcess\bin\Debug\net6.0-windows
for /l %%x in (1, 1, 10) do (
start CarProcess.exe
timeout /t 2
)