set Platform=%1
set Configuration=%2

dumpbin /SYMBOLS %Platform%\%Configuration%\mdb.obj | findstr /R "().*External.*mdb_.*" > %Platform%\%Configuration%\mdb_symbols
(for /F "usebackq tokens=2 delims==|" %%E in (`type %Platform%\%Configuration%\mdb_symbols`) do @call :PrintTrimmed %%E) > %Platform%\%Configuration%\mdb_symbols_trimmed
echo EXPORTS > %Platform%\%Configuration%\lmdb.def
call :ParseTrimmed >> %Platform%\%Configuration%\lmdb.def 
goto :EOF

:PrintTrimmed
@for /f "tokens=1*" %%a in ("%1") do @echo %%a
@goto :EOF

:ParseTrimmed
@echo off
Setlocal EnableDelayedExpansion
for /F %%E in (%Platform%\%Configuration%\mdb_symbols_trimmed) do (
  set sym=%%E
  if "%Platform%" == "Win32" (echo   !sym:~1!) else (echo   %%E)
)
