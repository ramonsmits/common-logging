REM other targets are:
REM 'build'
REM 'test'
REM 'test-integration'

@ECHO OFF
cls

tools\nant\bin\nant.exe -f:Common.Logging.build %1 %2 %3 %4 %5 %6 %7 %8 %9 > buildlog.txt

@echo Launching text file viewer to display buildlog.txt contents...
start "ignored but required placeholder window title argument" buildlog.txt
