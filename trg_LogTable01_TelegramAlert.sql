-- ============================================================================
-- SQL Server Trigger: LogTable01 警報即時通知發報
-- 資料庫: JINGSI_Database01
-- 資料表: LogTable01
-- 觸發條件: INSERT / UPDATE 且 Alarmsstate = 3
-- 說明: 將 chrono, Name, evtTitle, Description 打包成 Base64 JSON 格式，
--       透過 start /b 非同步脫鉤呼叫 TriggerTelegramSender.exe，交易 0 延遲。
-- ============================================================================

USE master;
GO
-- 1. 確保已啟用 xp_cmdshell (若已啟用可略過)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;
GO

USE JINGSI_Database01;
GO

CREATE OR ALTER TRIGGER trg_LogTable01_TelegramAlert
ON LogTable01
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 宣告變數
    DECLARE @Chrono NVARCHAR(100);
    DECLARE @Name NVARCHAR(200);
    DECLARE @EvtTitle NVARCHAR(200);
    DECLARE @Description NVARCHAR(500);
    DECLARE @Json NVARCHAR(MAX);
    DECLARE @Base64 NVARCHAR(MAX);
    DECLARE @Cmd NVARCHAR(4000);
    
    -- 設定執行檔路徑 (請依實際部署路徑調整)
    DECLARE @ExePath NVARCHAR(500) = 'D:\project\TriggerTelegram\TriggerTelegramSender\bin\Debug\net8.0\TriggerTelegramSender.exe';

    -- 只抓取 Alarmsstate = 3 的變更紀錄 (取最新一筆)
    SELECT TOP 1
        @Chrono = ISNULL(CAST(chrono AS NVARCHAR(100)), ''),
        @Name = ISNULL([Name], ''),
        @EvtTitle = ISNULL(evtTitle, ''),
        @Description = ISNULL([Description], '')
    FROM inserted
    WHERE Alarmsstate = 3;

    -- 若有符合條件的紀錄才觸發發報
    IF @Chrono IS NOT NULL AND LEN(@Chrono) > 0
    BEGIN
        -- 1. 處理 JSON 跳脫字元 (防止引號或斜線破壞 JSON 結構)
        SET @Name = REPLACE(REPLACE(@Name, '\', '\\'), '"', '\"');
        SET @EvtTitle = REPLACE(REPLACE(@EvtTitle, '\', '\\'), '"', '\"');
        SET @Description = REPLACE(REPLACE(@Description, '\', '\\'), '"', '\"');
        SET @Description = REPLACE(REPLACE(@Description, CHAR(13), ''), CHAR(10), '\n');

        -- 2. 組合 JSON 字串
        SET @Json = N'{"Chrono":"' + @Chrono + N'","Name":"' + @Name + N'","EvtTitle":"' + @EvtTitle + N'","Description":"' + @Description + N'"}';

        -- 3. 將 JSON 轉為 Base64 (SQL Server 原生 XML / Binary 轉碼，免安裝外部元件)
        SELECT @Base64 = CAST(N'' AS XML).value('xs:base64Binary(xs:hexBinary(sql:column("bin")))', 'VARCHAR(MAX)')
        FROM (SELECT CAST(@Json AS VARBINARY(MAX)) AS bin) AS t;

        -- 4. 組合命令列：使用 start /b 非同步脫鉤，SQL 交易瞬間 Commit 完成！
        SET @Cmd = 'cmd.exe /c start /b "" "' + @ExePath + '" --base64 "' + @Base64 + '"';

        -- 5. 執行發報
        EXEC xp_cmdshell @Cmd, NO_OUTPUT;
    END
END;
GO