// 確保使用 UTF-8 BOM 編碼，中文註解才不會亂碼
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("==========================================================");
        Console.WriteLine("🚨 花蓮XX警報 Telegram 自動發報終端機系統");
        Console.WriteLine("==========================================================");

        // 讀取 appsettings.json
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfiguration config = builder.Build();

        string botToken = config["Telegram:BotToken"] ?? "8893171713:AAEs62GcENHAK_Ursn9sfMH_DhSi6AEjklk";
        string chatId = config["Telegram:ChatId"] ?? "-1004420857526";

        string messageContent;

        // 判斷是否有命令列參數 (供 SQL Trigger 或背景行程傳入)
        if (args.Length > 0)
        {
            messageContent = string.Join(" ", args);
            Console.WriteLine($"[參數模式] 接收到傳入內容: {messageContent}");
        }
        else
        {
            messageContent = "系統警報連線測試正常。";
            Console.WriteLine("[測試模式] 未傳入參數，使用預設測試警報內容。");
        }

        // 套用 template.txt 樣版
        string finalMessage = FormatWithTemplate(messageContent);

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
            Console.WriteLine("🎉 [完成] 警報訊息已成功送達 Telegram 群組！");
            return 0;
        }
        else
        {
            Console.WriteLine("❌ [失敗] 訊息發送未完成，請檢查網路或權限。");
            return 1;
        }
    }

    /// <summary>
    /// 從 template.txt 讀取樣版並替換變數
    /// </summary>
    private static string FormatWithTemplate(string content)
    {
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.txt");
        string template;

        if (File.Exists(templatePath))
        {
            template = File.ReadAllText(templatePath, Encoding.UTF8);
        }
        else
        {
            template = "🚨【花蓮XX警報即時通知】\n📝 事件說明：{MESSAGE}\n📅 通報時間：{TIME}";
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return template
            .Replace("{MESSAGE}", content)
            .Replace("{TIME}", timestamp);
    }
}