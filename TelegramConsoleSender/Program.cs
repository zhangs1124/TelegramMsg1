using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TelegramSender
{
    public class TelegramGroup
    {
        public string Name { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
    }

    internal class Program
    {
        // Telegram Bot Token
        private const string BotToken = "8893171713:AAEs62GcENHAK_Ursn9sfMH_DhSi6AEjklk";

        // 已設定的群組清單
        private static readonly List<TelegramGroup> TargetGroups = new List<TelegramGroup>
        {
            new TelegramGroup { Name = "文化大學大倫館", ChatId = "-5286846277" },
            new TelegramGroup { Name = "文化大學大雅館", ChatId = "-5369953878" }
        };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==============================================");
            Console.WriteLine("🚀 Telegram 多群組訊息發送器 (C# Console)");
            Console.WriteLine("==============================================");
            Console.WriteLine("目前已設定群組清單：");
            for (int i = 0; i < TargetGroups.Count; i++)
            {
                Console.WriteLine(string.Format("  [{0}] {1} (Chat ID: {2})", i + 1, TargetGroups[i].Name, TargetGroups[i].ChatId));
            }
            Console.WriteLine("----------------------------------------------");

            string defaultMessage = "📢 [多群組測試] 您好！這是一則來自 C# 終端機程式的自動推播訊息！ (發送時間：" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ")";
            string message = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : defaultMessage;

            Console.WriteLine("準備發送內容：" + message);
            Console.WriteLine("\n⏳ 開始依序發送訊息至各群組...\n");

            foreach (var group in TargetGroups)
            {
                Console.Write(string.Format("👉 發送中 -> {0} ({1})... ", group.Name, group.ChatId));
                bool success = await SendTelegramMessageAsync(BotToken, group.ChatId, message);

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ 成功！");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ 失敗！");
                }
                Console.ResetColor();
            }

            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("🎉 所有群組發送作業執行完畢！");
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
                        Console.WriteLine("\n[API 回傳錯誤] " + responseString);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n[例外異常] " + ex.Message);
                    return false;
                }
            }
        }
    }
}
