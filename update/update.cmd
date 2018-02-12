@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

set cpuFIELDS=core0temp:core1temp:core2temp:core3temp:cpuload:core0load:core1load:core2load:core3load:cpufanspeed
set cpuDBDESCRIPTION=DS:core0temp:GAUGE:600:0:100^
     DS:core1temp:GAUGE:600:0:100^
     DS:core2temp:GAUGE:600:0:100^
     DS:core3temp:GAUGE:600:0:100^
     DS:cpuload:GAUGE:600:0:100^
     DS:core0load:GAUGE:600:0:100^
     DS:core1load:GAUGE:600:0:100^
     DS:core2load:GAUGE:600:0:100^
     DS:core3load:GAUGE:600:0:100^
     DS:cpufanspeed:GAUGE:600:0:4000
     
set mbFIELDS=temperature:fan1speed:fan2speed
set mbDBDESCRIPTION=DS:temperature:GAUGE:600:0:100^
     DS:fan1speed:GAUGE:600:0:6000^
     DS:fan2speed:GAUGE:600:0:4000

set HOSTNAME=%COMPUTERNAME%

call :tolower HOSTNAME

set REPORT_PATH=E:\tools\openhardwaremonitor\OpenHardwareMonitorReport.exe
REM set REPORT_PATH=E:\programs\openhardwaremonitor\Bin\Debug\OpenHardwareMonitorReport.exe

for /F "usebackq tokens=1,2" %%a in (`%REPORT_PATH%`) do (
  set DBNAME=%%a
  set VALUES=%%b
  
  set DBPATH=dbs\!DBNAME!.rrd
  set XMLPATH=\\scorpion-oi\dbs\monitoring\%HOSTNAME%\xmls\!DBNAME!.xml
  
  call set FIELDS=%%!DBNAME!FIELDS%%
  call set DBDESCRIPTION=%%!DBNAME!DBDESCRIPTION%%
  
  if not exist !DBPATH! (
      echo %DATE% %TIME% creating databse: "!DBPATH!"
      rrdtool create !DBPATH! --step 300^
       !DBDESCRIPTION!^
       RRA:AVERAGE:0.5:1:576^
       RRA:AVERAGE:0.5:6:672^
       RRA:AVERAGE:0.5:24:732^
       RRA:AVERAGE:0.5:144:1460
  )
  
  echo %DATE% %TIME% updating database: "!DBPATH!" fields: "!FIELDS!" with values: "!VALUES!"
  rrdtool update !DBPATH! -t !FIELDS! N:!VALUES!
  echo %DATE% %TIME% export database: "!DBPATH!" to: "!XMLPATH!"
  rrdtool dump !DBPATH! > !XMLPATH!
)

goto :EOF

:tolower
for %%L IN (a b c d e f g h i j k l m n o p q r s t u v w x y z) DO SET %1=!%1:%%L=%%L!
goto :EOF
