-- ============================================================================
-- 【遠端專用】SQL Server Trigger 部署腳本
-- 資料庫: JINGSI_Database01
-- 資料表: LogTable01
-- 執行檔路徑: C:\TriggerTelegram\TriggerTelegramSender.exe
-- 觸發條件: INSERT / UPDATE 且 AlarmState = 3
-- ============================================================================

-- 步驟 1: 啟用 xp_cmdshell
USE master;
GO
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO
EXEC sp_configure 'xp_cmdshell', 1;
RECONFIGURE;
GO

-- 步驟 2: 先安全刪除舊的 Trigger (若存在)
USE JINGSI_Database01;
GO
DROP TRIGGER IF EXISTS trg_LogTable01_TelegramAlert;
GO

-- 步驟 3: 建立遠端正式 Trigger
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE TRIGGER trg_LogTable01_TelegramAlert
ON LogTable01
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Chrono NVARCHAR(100);
    DECLARE @Name NVARCHAR(255);
    DECLARE @EvtTitle NVARCHAR(255);
    DECLARE @Description NVARCHAR(255);
    DECLARE @Json NVARCHAR(MAX);
    DECLARE @Base64 NVARCHAR(MAX);
    DECLARE @Cmd NVARCHAR(4000);
    
    -- 👉 遠端伺服器固定路徑
    DECLARE @ExePath NVARCHAR(500) = 'C:\TriggerTelegram\TriggerTelegramSender.exe';

    -- 只抓取 AlarmState = 3 的變更紀錄
    SELECT TOP 1
        @Chrono = ISNULL(CAST(Chrono AS NVARCHAR(100)), ''),
        @Name = ISNULL([Name], ''),
        @EvtTitle = ISNULL(EvtTitle, ''),
        @Description = ISNULL([Description], '')
    FROM inserted
    WHERE AlarmState = 3;

    IF @Chrono IS NOT NULL AND LEN(@Chrono) > 0
    BEGIN
        -- 1. JSON 特殊跳脫處理
        SET @Name = REPLACE(REPLACE(@Name, '\', '\\'), '"', '\"');
        SET @EvtTitle = REPLACE(REPLACE(@EvtTitle, '\', '\\'), '"', '\"');
        SET @Description = REPLACE(REPLACE(@Description, '\', '\\'), '"', '\"');
        SET @Description = REPLACE(REPLACE(@Description, CHAR(13), ''), CHAR(10), '\n');

        -- 2. 封裝成 JSON 字串
        SET @Json = N'{"Chrono":"' + @Chrono + N'","Name":"' + @Name + N'","EvtTitle":"' + @EvtTitle + N'","Description":"' + @Description + N'"}';

        -- 3. 轉為 Base64 (避免命令列字元解析錯誤)
        SELECT @Base64 = CAST(N'' AS XML).value('xs:base64Binary(xs:hexBinary(sql:column("bin")))', 'VARCHAR(MAX)')
        FROM (SELECT CAST(@Json AS VARBINARY(MAX)) AS bin) AS t;

        -- 4. 非同步脫鉤呼叫 (0 延遲，SQL 交易瞬間 Commit)
        SET @Cmd = 'cmd.exe /c start /b "" "' + @ExePath + '" --base64 "' + @Base64 + '"';

        -- 5. 執行外部發報
        EXEC xp_cmdshell @Cmd, NO_OUTPUT;
    END
END;
GO