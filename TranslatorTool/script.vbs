Option Explicit
 
dim path, WshShell
 

' Создаем ссылку на объект WscriptShell
set WshShell = WScript.CreateObject("Wscript.Shell")
 
' Открываем notepad (Wshshell Run)
WshShell.Run "sqlcmd -i C:\\WINDOWS\\script.sql", ,true
