using System;
using BotCommon;
using BotCommon.KeepAlive;
using BroadcastMessageSender;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

internal class Program
{
  private static readonly ILogger log = LogManager.GetCurrentClassLogger();

  public static void Main(string[] args)
  {
    var bot = new TelegramBotClient(new BotConfigManager().Config.BotToken);
    PrepareForStartBot(bot);
    StartBot(bot);
    string command;
    do
    {
      command = Console.ReadLine();
    } while (!command.Equals("/exit", StringComparison.InvariantCulture));
    log.Info("Bye bye");
    Environment.Exit(0);
  }

  private static void PrepareForStartBot(ITelegramBotClient bot)
  {
    var botKeepAlive = new BotKeepAlive(bot);
    botKeepAlive.StartKeepAlive();
  }

  private static void StartBot(ITelegramBotClient bot)
  {
    log.Debug("Start Bot");
    var opts = new ReceiverOptions
    {
      AllowedUpdates =
      [
        UpdateType.Message,
        UpdateType.CallbackQuery
      ],
      ThrowPendingUpdates = true
    };
    bot.StartReceiving<BotUpdateHandler>(receiverOptions: opts);
  }
}