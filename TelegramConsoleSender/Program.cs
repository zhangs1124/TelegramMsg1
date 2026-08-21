using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TelegramSender
{
    internal class Program
    {
        // Telegram Bot Token 與 目標群組 Chat ID
        private const string BotToken = "8893171713:AAEs62GcENHAK_Ursn9sfMH_DhSi6AEjklk";
        private const string ChatId = "-5286846277"; // 文化大學大倫館

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==============================================");
            Console.WriteLine("🚀 Telegram 群組訊息發送器 (C# Console)");
            Console.WriteLine("==============================================");
            Console.WriteLine("目標群組：文化大學大倫館 (Chat ID: " + ChatId + ")");
            Console.WriteLine("----------------------------------------------");

            string message = "📢 [測試發送] 您好！這是一則來自 C# 終端機程式的自動推播訊息！ (發送時間：" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ")";

            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                message = args[0];
            }

            Console.WriteLine("準備發送內容：" + message);
            Console.WriteLine("\n⏳ 正在發送訊息至 Telegram 群組...");

            bool success = await SendTelegramMessageAsync(BotToken, ChatId, message);

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ 訊息發送成功！請至 Telegram「文化大學大倫館」群組查看。");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ 訊息發送失敗，請檢查網路或權限設定。");
            }

            Console.ResetColor();
            Console.WriteLine("----------------------------------------------");
        }

        /// <summary>
        /// 發送 Telegram 訊息給指定 Chat ID
        /// </summary>
        private static async Task<bool> SendTelegramMessageAsync(string token, string chatId, string text)
        {
            string apiUrl = "https://api.telegram.org/bot" + token + "/sendMessage";

            using (HttpClient client = new HttpClient())
            {
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
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("API 回傳錯誤：" + responseString);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("發送發生例外異常：" + ex.Message);
                    return false;
                }
            }
        }
    }
}
