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

public class ChangeCityScenario : AutoStepBotCommandScenario
{
  public override Guid Id { get; } = new Guid("26EE39FD-98AC-4D66-AC05-9789C6C38940");

  public override string ScenarioCommand { get; }

  private async Task StepAction1(ITelegramBotClient bot, Update update, long chatId)
  {
    await bot.SendTextMessageAsync(chatId, BotMessages.YourCity, parseMode: ParseMode.MarkdownV2);
  }

  private async Task StepAction2(ITelegramBotClient bot, Update update, long chatId)
  {
    var userInfo = BotDbContext.Instance.UserInfos
      .FirstOrDefault(i => i.UserId == chatId);
    userInfo.City = update.Message.Text;

    await BotDbContext.Instance.SaveChangesAsync();
    var replyMarkup = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData(BotMessages.ChangeInfo, BotChatCommands.Change) },
      new[] { InlineKeyboardButton.WithCallbackData(BotMessages.BackButton, BotChatCommands.Start) },
    });
    await bot.SendTextMessageAsync(chatId, BotMessages.Success, parseMode: ParseMode.MarkdownV2, replyMarkup: replyMarkup);
  }

  public ChangeCityScenario()
  {
    this.steps = new List<BotCommandScenarioStep>
      {
        new (this.StepAction1),
        new (this.StepAction2),
      }.GetEnumerator();
  }
}