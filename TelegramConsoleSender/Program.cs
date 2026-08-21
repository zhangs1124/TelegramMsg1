using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TelegramSender
{
    public class TelegramGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ChatId);
    }

    internal class Program
    {
        // Telegram Bot Token
        private const string BotToken = "8893171713:AAEs62GcENHAK_Ursn9sfMH_DhSi6AEjklk";

        // 1 到 4 館群組清單
        private static readonly List<TelegramGroup> DormitoryGroups = new List<TelegramGroup>
        {
            new TelegramGroup { Id = 1, Name = "文化大學大倫館", ChatId = "-5286846277" },
            new TelegramGroup { Id = 2, Name = "文化大學大雅館", ChatId = "-5369953878" },
            new TelegramGroup { Id = 3, Name = "文化大學第三館 (待加入)", ChatId = "" },
            new TelegramGroup { Id = 4, Name = "文化大學第四館 (待加入)", ChatId = "" }
        };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("==========================================================");
                Console.WriteLine("🏫 文化大學宿舍 Telegram 訊息發送控制台");
                Console.WriteLine("==========================================================");
                Console.WriteLine("【目標群組選項】：");

                foreach (var g in DormitoryGroups)
                {
                    string status = g.IsConfigured ? string.Format("(Chat ID: {0})", g.ChatId) : "[未設定 Chat ID]";
                    Console.WriteLine(string.Format("  [{0}] {1,-26} {2}", g.Id, g.Name, status));
                }

                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine("  [直接按 Enter] => 📢 全送 (廣播至所有已設定之館別)");
                Console.WriteLine("  [輸入 0]       => 🚪 離開程式");
                Console.WriteLine("==========================================================");
                Console.Write("👉 請選擇發送對象 (1-4 / Enter 全送 / 0 離開)：");

                string input = Console.ReadLine()?.Trim() ?? string.Empty;

                if (input == "0")
                {
                    Console.WriteLine("\n感謝使用，程式即將結束。");
                    break;
                }

                List<TelegramGroup> targetList = new List<TelegramGroup>();

                if (string.IsNullOrEmpty(input) || input.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    // 全送模式：選取所有已設定 Chat ID 的群組
                    targetList = DormitoryGroups.Where(g => g.IsConfigured).ToList();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n🚀 已選擇：【全送模式】（共 " + targetList.Count + " 個已設定群組）");
                    Console.ResetColor();
                }
                else if (int.TryParse(input, out int selectedId) && selectedId >= 1 && selectedId <= DormitoryGroups.Count)
                {
                    var selectedGroup = DormitoryGroups.FirstOrDefault(g => g.Id == selectedId);
                    if (selectedGroup != null)
                    {
                        if (!selectedGroup.IsConfigured)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(string.Format("\n⚠️ [{0}] 尚未設定 Chat ID，無法發送！請先將機器人拉進群組。", selectedGroup.Name));
                            Console.ResetColor();
                            Console.WriteLine("\n請按任意鍵返回選單...");
                            Console.ReadKey();
                            continue;
                        }
                        targetList.Add(selectedGroup);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(string.Format("\n🎯 已選擇單一群組：[{0}] {1}", selectedGroup.Id, selectedGroup.Name));
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n❌ 輸入選項無效，請重新選擇！");
                    Console.ResetColor();
                    Console.WriteLine("\n請按任意鍵返回選單...");
                    Console.ReadKey();
                    continue;
                }

                // 定義預設發送訊息內容
                string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                string defaultMsg = "🔔【宿舍管理即時通報】\n親愛的住宿生您好：\n請配合宿舍用電與晚間門禁安全規範，隨手關閉電源與門窗。\n\n📅 通報時間：" + timestamp + "\n🏫 文化大學宿舍管理中心 敬啟";

                Console.WriteLine("\n----------------------------------------------------------");
                Console.WriteLine("預設推播訊息內容：");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(defaultMsg);
                Console.ResetColor();
                Console.WriteLine("----------------------------------------------------------");
                Console.Write("是否直接發送此預設訊息？(按 Enter 直接發送 / 輸入新內容)：");
                
                string customInput = Console.ReadLine()?.Trim() ?? string.Empty;
                string finalMessage = string.IsNullOrEmpty(customInput) ? defaultMsg : customInput;

                Console.WriteLine("\n⏳ 開始執行推播作業...\n");

                foreach (var group in targetList)
                {
                    Console.Write(string.Format("👉 正在發送至 [{0}]... ", group.Name));
                    bool success = await SendTelegramMessageAsync(BotToken, group.ChatId, finalMessage);

                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ 發送成功！");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ 發送失敗！");
                    }
                    Console.ResetColor();
                }

                Console.WriteLine("\n==========================================================");
                Console.WriteLine("🎉 本次推播作業完成！請按任意鍵返回主選單...");
                Console.ReadKey();
            }
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
