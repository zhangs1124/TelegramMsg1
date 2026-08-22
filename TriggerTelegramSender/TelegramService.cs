// 確保使用 UTF-8 BOM 編碼，中文註解才不會亂碼
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class TelegramService
{
    private static readonly HttpClient client = new HttpClient();

    /// <summary>
    /// 發送 Telegram 訊息給指定 Chat ID
    /// </summary>
    /// <param name="token">Telegram Bot Token</param>
    /// <param name="chatId">目標群組 ID (例如: -1004420857526)</param>
    /// <param name="text">訊息內容</param>
    /// <returns>是否成功</returns>
    public static async Task<bool> SendMessageAsync(string token, string chatId, string text)
    {
        string apiUrl = $"https://api.telegram.org/bot{token}/sendMessage";

        var parameters = new Dictionary<string, string>
        {
            { "chat_id", chatId },
            { "text", text }
        };

        var content = new FormUrlEncodedContent(parameters);

        try
        {
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);
            string responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Telegram] 訊息發送成功！目標群組: {chatId}");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Telegram] 訊息發送失敗。狀態碼: {response.StatusCode}");
                Console.WriteLine($"[Telegram] API 回傳: {responseString}");
                Console.ResetColor();
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Telegram] 發送過程發生例外: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }
}