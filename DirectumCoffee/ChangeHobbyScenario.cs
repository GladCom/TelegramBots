using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotCommon.Scenarios;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DirectumCoffee;

public class ChangeHobbyScenario: AutoStepBotCommandScenario
{
    public override Guid Id { get; } = new Guid("155C6FAB-2EAF-42ED-9EAC-308318A9B842");
    public override string ScenarioCommand { get; }

    private async Task StepAction1(ITelegramBotClient bot, Update update, long chatId)
    {
        await bot.SendTextMessageAsync(chatId, BotMessages.YourHobby, parseMode: ParseMode.MarkdownV2);
    }

    private async Task StepAction2(ITelegramBotClient bot, Update update, long chatId)
    {
        var userInfo = BotDbContext.Instance.UserInfos
            .Where(i => i.UserId == chatId)
            .FirstOrDefault();
        userInfo.Hobby = update.Message.Text;

        await BotDbContext.Instance.SaveChangesAsync();
        var replyMarkup = new InlineKeyboardMarkup(new []
        {
            new []{InlineKeyboardButton.WithCallbackData(BotMessages.ChangeInfo, BotChatCommands.Change)},
            new []{InlineKeyboardButton.WithCallbackData(BotMessages.BackButton, BotChatCommands.Start)},
        });
        await bot.SendTextMessageAsync(chatId, BotMessages.Success, parseMode: ParseMode.MarkdownV2, replyMarkup: replyMarkup);
    }

    public ChangeHobbyScenario()
    {
        this.steps = new List<BotCommandScenarioStep>
        {
            new (StepAction1),
            new (StepAction2),

        }.GetEnumerator();
    }
}