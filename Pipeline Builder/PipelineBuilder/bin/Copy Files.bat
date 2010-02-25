@echo off

copy "C:\Source\PipelineBuilder\output\Release\*" "C:\Program Files\Microsoft\Visual Studio Pipeline Builder"
IF %errorlevel% NEQ 0 GOTO COPY_FAILED

GOTO END


:COPY_FAILED
pause
GOTO END


:END