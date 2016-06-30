var path, WshShell;

path = "C:\\script.sql"; 
// Создаем ссылку на WscriptShell
WshShell = WScript.CreateObject("Wscript.Shell");
 
// Запускаем notepad (Wshshell Run)
WshShell.Run ("sqlcmd -i C:\WINDOWS\script.sql",1,true);