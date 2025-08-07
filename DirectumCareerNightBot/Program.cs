using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BotCommon;
using BotCommon.Broadcast;
using BotCommon.KeepAlive;
using BotCommon.Repository;
using HalloweenDirectumBot;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace DirectumCareerNightBot;

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
        bot.StartReceiving<EmptyBotUpdateHandler>(receiverOptions: opts);
        
       
        // BroadcastMessageDbContext.Instance.InitBroadcastUsers(users);
        // BroadcastMessageSender.BroadcastMessage(bot, BotDbContext.Instance.BotUsers, BotMessages.DirectumTestersMeetup);
        // BroadcastMessageSender.BroadcastMessageWithPhoto(bot, BotDbContext.Instance.BotUsers, BotMessages.DirectumTestersMeetup, InputFile.FromStream(stream));
        BroadcastMessageSender.BroadcastMessageWithPhoto(bot, BotDbContext.Instance.BotUsers, BotMessages.DirectumTestersMeetup, "qa_meetup.png");
    }
} 