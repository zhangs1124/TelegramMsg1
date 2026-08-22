// 確保使用 UTF-8 BOM 編碼，中文註解才不會亂碼
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class AlarmPayload
{
    public string Chrono { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EvtTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("==========================================================");
        Console.WriteLine("🚨 花蓮XX警報 Telegram 自動發報終端機系統 (JSON / Base64 模式)");
        Console.WriteLine("==========================================================");

        // 讀取 appsettings.json
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfiguration config = builder.Build();

        string botToken = config["Telegram:BotToken"] ?? "8893171713:AAEs62GcENHAK_Ursn9sfMH_DhSi6AEjklk";
        string chatId = config["Telegram:ChatId"] ?? "-1004420857526";

        var payload = new AlarmPayload();

        if (args.Length > 0)
        {
            string rawArg = string.Join(" ", args).Trim();

            // 判斷傳入的是 Base64、直接 JSON 還是純文字
            if (rawArg.StartsWith("--base64", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string base64Str = rawArg.Substring(8).Trim().Trim('\"', '\'');
                    byte[] bytes = Convert.FromBase64String(base64Str);
                    string jsonStr = Encoding.UTF8.GetString(bytes);
                    payload = JsonSerializer.Deserialize<AlarmPayload>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AlarmPayload();
                    Console.WriteLine("[模式] 成功解析 Base64 JSON 格式資料！");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[錯誤] 解析 Base64 失敗: {ex.Message}");
                    payload.Description = rawArg;
                }
            }
            else if (rawArg.StartsWith("{") && rawArg.EndsWith("}"))
            {
                try
                {
                    payload = JsonSerializer.Deserialize<AlarmPayload>(rawArg, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AlarmPayload();
                    Console.WriteLine("[模式] 成功解析直接 JSON 格式資料！");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[錯誤] 解析 JSON 失敗: {ex.Message}");
                    payload.Description = rawArg;
                }
            }
            else
            {
                // 純文字傳入
                payload.Description = rawArg;
                payload.Name = "手動通報";
                payload.EvtTitle = "即時通知";
                Console.WriteLine("[模式] 接收純文字參數模式。");
            }
        }
        else
        {
            // 無參數測試預設值
            payload.Chrono = "TEST-9999";
            payload.Name = "測試主機-A1";
            payload.EvtTitle = "溫度超溫警報測試";
            payload.Description = "這是一則由 Antigravity 終端機端發出的測試通知 (JSON 格式封裝)。";
            Console.WriteLine("[模式] 無傳入參數，執行預設測試通報。");
        }

        // 套用樣版替換
        string finalMessage = FormatWithTemplate(payload);

        // 記錄發送記錄檔 (保留 log 檔案不刪除)
        SaveLogRecord(payload, finalMessage);

        Console.WriteLine($"[目標群組] {chatId}");
        Console.WriteLine("----------------------------------------------------------");
        Console.WriteLine("發送內容預覽：");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(finalMessage);
        Console.ResetColor();
        Console.WriteLine("----------------------------------------------------------");

        Console.WriteLine("[發送中] 正在呼叫 Telegram API...");
        bool success = await TelegramService.SendMessageAsync(botToken, chatId, finalMessage);

        if (success)
        {
            Console.WriteLine("🎉 [完成] 警報訊息已成功送達 Telegram 群組！(記錄已保留)");
            return 0;
        }
        else
        {
            Console.WriteLine("❌ [失敗] 訊息發送未完成，請檢查網路或權限。");
            return 1;
        }
    }

    /// <summary>
    /// 從 template.txt 讀取樣版並替換所有欄位
    /// </summary>
    private static string FormatWithTemplate(AlarmPayload data)
    {
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.txt");
        string template;

        if (File.Exists(templatePath))
        {
            template = File.ReadAllText(templatePath, Encoding.UTF8);
        }
        else
        {
            template = "🚨【花蓮XX警報即時通知】\n📍 警報名稱：{NAME}\n⚠️ 事件標題：{TITLE}\n📝 詳細說明：{DESC}\n🔢 記錄編號：{CHRONO}\n📅 通報時間：{TIME}";
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return template
            .Replace("{NAME}", string.IsNullOrWhiteSpace(data.Name) ? "無" : data.Name)
            .Replace("{TITLE}", string.IsNullOrWhiteSpace(data.EvtTitle) ? "無" : data.EvtTitle)
            .Replace("{DESC}", string.IsNullOrWhiteSpace(data.Description) ? "無" : data.Description)
            .Replace("{CHRONO}", string.IsNullOrWhiteSpace(data.Chrono) ? "無" : data.Chrono)
            .Replace("{TIME}", timestamp);
    }

    /// <summary>
    /// 保存發報歷史 Log 檔 (持久保存不刪除)
    /// </summary>
    private static void SaveLogRecord(AlarmPayload data, string message)
    {
        try
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            string logFile = Path.Combine(logDir, $"AlarmLog_{DateTime.Now:yyyyMMdd}.txt");
            string logEntry = $"========================================\n" +
                              $"時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                              $"Chrono: {data.Chrono}\n" +
                              $"Name: {data.Name}\n" +
                              $"EvtTitle: {data.EvtTitle}\n" +
                              $"Description: {data.Description}\n" +
                              $"訊息完整內容:\n{message}\n\n";

            File.AppendAllText(logFile, logEntry, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Log記錄警告] 無法寫入 Log 檔案: {ex.Message}");
        }
    }
}