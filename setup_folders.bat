@echo off
chcp 65001 > nul
echo ========================================================
echo   正在自動建立目標目錄並複製執行檔...
echo ========================================================

:: 1. 建立原本 Trigger 指向的完整深層目錄
mkdir "D:\project\TriggerTelegram\TriggerTelegramSender\bin\Debug\net8.0" 2>nul

:: 2. 同時建立標準的 C:\TriggerTelegram 目錄 (備用)
mkdir "C:\TriggerTelegram" 2>nul

:: 3. 複製目前的執行檔與設定檔至這兩個目錄 (若目前目錄有檔案)
if exist "TriggerTelegramSender.exe" (
    copy /y "TriggerTelegramSender.exe" "D:\project\TriggerTelegram\TriggerTelegramSender\bin\Debug\net8.0\"
    copy /y "template.txt" "D:\project\TriggerTelegram\TriggerTelegramSender\bin\Debug\net8.0\"
    copy /y "appsettings.json" "D:\project\TriggerTelegram\TriggerTelegramSender\bin\Debug\net8.0\"

    copy /y "TriggerTelegramSender.exe" "C:\TriggerTelegram\"
    copy /y "template.txt" "C:\TriggerTelegram\"
    copy /y "appsettings.json" "C:\TriggerTelegram\"
)

echo.
echo [完成] 目錄建立與檔案部署完成！
echo.
pause