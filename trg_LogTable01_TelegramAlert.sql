-- ============================================================================
-- SQL Server Trigger: LogTable01 警報即時通知發報 (正式遠端標準版)
-- 資料庫: JINGSI_Database01
-- 資料表: LogTable01
-- 執行檔路徑: C:\TriggerTelegram\TriggerTelegramSender.exe
-- 觸發條件: INSERT / UPDATE 且 AlarmState = 3
-- 說明: 將 Chrono, Name, EvtTitle, Description 打包成 Base64 JSON 格式，
--       透過 start /b 非同步脫鉤呼叫 TriggerTelegramSender.exe，交易 0 延遲。
-- ============================================================================

-- 1. 啟用 xp_cmdshell (若已啟用可略過)
USE master;
GO
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;
GO

-- 2. 建立/更新 Trigger
USE JINGSI_Database01;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER TRIGGER trg_LogTable01_TelegramAlert
ON LogTable01
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 宣告變數
    DECLARE @Chrono NVARCHAR(100);
    DECLARE @Name NVARCHAR(255);
    DECLARE @EvtTitle NVARCHAR(255);
    DECLARE @Description NVARCHAR(255);
    DECLARE @Json NVARCHAR(MAX);
    DECLARE @Base64 NVARCHAR(MAX);
    DECLARE @Cmd NVARCHAR(4000);
    
    -- 👉 正式路徑設定：C:\TriggerTelegram\TriggerTelegramSender.exe
    DECLARE @ExePath NVARCHAR(500) = 'C:\TriggerTelegram\TriggerTelegramSender.exe';

    -- 只抓取 AlarmState = 3 的變更紀錄 (精確比對欄位名稱 AlarmState)
    SELECT TOP 1
        @Chrono = ISNULL(CAST(Chrono AS NVARCHAR(100)), ''),
        @Name = ISNULL([Name], ''),
        @EvtTitle = ISNULL(EvtTitle, ''),
        @Description = ISNULL([Description], '')
    FROM inserted
    WHERE AlarmState = 3;

    -- 若有符合條件的紀錄才觸發發報
    IF @Chrono IS NOT NULL AND LEN(@Chrono) > 0
    BEGIN
        -- 1. 處理 JSON 特殊跳脫字元
        SET @Name = REPLACE(REPLACE(@Name, '\', '\\'), '"', '\"');
        SET @EvtTitle = REPLACE(REPLACE(@EvtTitle, '\', '\\'), '"', '\"');
        SET @Description = REPLACE(REPLACE(@Description, '\', '\\'), '"', '\"');
        SET @Description = REPLACE(REPLACE(@Description, CHAR(13), ''), CHAR(10), '\n');

        -- 2. 組合 JSON 字串
        SET @Json = N'{"Chrono":"' + @Chrono + N'","Name":"' + @Name + N'","EvtTitle":"' + @EvtTitle + N'","Description":"' + @Description + N'"}';

        -- 3. 將 JSON 轉為 Base64 (SQL 原生 XML Binary 轉換)
        SELECT @Base64 = CAST(N'' AS XML).value('xs:base64Binary(xs:hexBinary(sql:column("bin")))', 'VARCHAR(MAX)')
        FROM (SELECT CAST(@Json AS VARBINARY(MAX)) AS bin) AS t;

        -- 4. 組合命令列：使用 start /b 非同步脫鉤，SQL 交易瞬間 Commit 完成！
        SET @Cmd = 'cmd.exe /c start /b "" "' + @ExePath + '" --base64 "' + @Base64 + '"';

        -- 5. 執行發報 (非同步無輸出模式)
        EXEC xp_cmdshell @Cmd, NO_OUTPUT;
    END
END;
GO