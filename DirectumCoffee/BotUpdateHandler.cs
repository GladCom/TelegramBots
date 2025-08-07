using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotCommon;
using BotCommon.Repository;
using BotCommon.Scenarios;
using Newtonsoft.Json;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DirectumCoffee;

/// <summary>
/// Обработчик обновлений бота.
/// </summary>
public class BotUpdateHandler : IUpdateHandler
{
  #region Поля и свойства

  /// <summary>
  /// Логгер.
  /// </summary>
  private static readonly ILogger log = LogManager.GetCurrentClassLogger();

  /// <summary>
  /// Репозиторий для работы со сценариями.
  /// </summary>
  private readonly UserScenarioRepository userScenarioRepository;

  #endregion

  #region IUpdateHandler

  /// <inheritdoc />
  public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
  {
    log.Trace(JsonConvert.SerializeObject(update));
    var userInfo = BotHelper.GetUserInfo(update);
    log.Info($"user: {BotHelper.GetUsername(userInfo)}, userMessage: {BotHelper.GetMessage(update)}");
    var userId = userInfo.Id;
    BotDbContext.Instance.Add(new BotUser(
      userId,
      userInfo.Username,
      userInfo.FirstName,
      userInfo.LastName,
      userInfo.LanguageCode));
    FillUserSystemInfo(userId);
    UserCommandScenario? userScenario = null;

    switch (BotHelper.GetMessage(update))
    {
      case BotChatCommands.Start:
        {
          var isFirstMeet = !BotDbContext.Instance.UserInfos.Any(u => u.UserId == userId);
          if (isFirstMeet)
          {
            var replyMarkup = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData(BotMessages.GoMessage, BotChatCommands.Go));
            await botClient.SendTextMessageAsync(
              userId,
              BotMessages.BotFirstMeet,
              cancellationToken: cancellationToken,
              parseMode: ParseMode.MarkdownV2,
              replyMarkup: replyMarkup);
          }
          else
          {
            var userSystemInfo = BotDbContext.Instance.UserInfos.FirstOrDefault(i => i.UserId == userId);
            InlineKeyboardMarkup replyMarkup;
            if (userSystemInfo is { SearchDisable: true })
            {
              replyMarkup = new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithCallbackData(BotMessages.MyInfo, BotChatCommands.Info)],
              [InlineKeyboardButton.WithCallbackData(BotMessages.RestartInfo, BotChatCommands.Restart)]
              ]);
            }
            else
            {
              replyMarkup = new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithCallbackData(BotMessages.MyInfo, BotChatCommands.Info)],
              [InlineKeyboardButton.WithCallbackData(BotMessages.StopInfo, BotChatCommands.Stop)]
              ]);
            }

            await botClient.SendTextMessageAsync(
              userId,
              BotMessages.BotStartMessage,
              cancellationToken: cancellationToken,
              parseMode: ParseMode.MarkdownV2,
              replyMarkup: replyMarkup);
          }

          this.userScenarioRepository.Remove(userId);
          break;
        }

      case BotChatCommands.Stop:
        {
          var userSystemInfo = BotDbContext.Instance.UserInfos.FirstOrDefault(i => i.UserId == userId);
          if (userSystemInfo != null)
            userSystemInfo.SearchDisable = true;

          await BotDbContext.Instance.SaveChangesAsync(cancellationToken);
          await botClient.SendTextMessageAsync(
            userId,
            BotMessages.StopInfoMessage,
            cancellationToken: cancellationToken,
            parseMode: ParseMode.MarkdownV2);

          this.userScenarioRepository?.Remove(userId);
          break;
        }

      case BotChatCommands.Restart:
        {
          var userSystemInfo = BotDbContext.Instance.UserInfos.FirstOrDefault(i => i.UserId == userId);
          if (userSystemInfo != null)
            userSystemInfo.SearchDisable = false;
          await BotDbContext.Instance.SaveChangesAsync(cancellationToken);

          await botClient.SendTextMessageAsync(
            userId,
            BotMessages.RestartInfoMessage,
            cancellationToken: cancellationToken,
            parseMode: ParseMode.MarkdownV2);

          this.userScenarioRepository?.Remove(userId);
          break;
        }

      case BotChatCommands.Go:
        {
          userScenario = new UserCommandScenario(userId, new MainScenario());
          break;
        }

      case BotChatCommands.Info:
        {
          var info = BotDbContext.Instance.UserInfos
            .FirstOrDefault(u => u.UserId == userId);

          var userInfoText = new StringBuilder();
          if (info == null)
          {
            userInfoText.Append(BotMessages.InfoNotFound);
          }
          else
          {
            userInfoText.AppendLine($"Имя: {info.Name}");
            userInfoText.AppendLine($"Город: {info.City}");
            userInfoText.AppendLine($"Направление: {info.Work}");
            userInfoText.AppendLine($"Увлечения: {info.Hobby}");
            userInfoText.AppendLine($"О чём хочешь пообщаться: {info.Interests}");
          }

          var replyMarkup = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("Заполнить заново", BotChatCommands.Go)],
          [InlineKeyboardButton.WithCallbackData(BotMessages.ChangeInfo, BotChatCommands.Change)],
          [InlineKeyboardButton.WithCallbackData(BotMessages.BackButton, BotChatCommands.Start)]
          ]);

          await botClient.SendTextMessageAsync(userId, userInfoText.ToString(), cancellationToken: cancellationToken, replyMarkup: replyMarkup);
          this.userScenarioRepository?.Remove(userId);

          break;
        }

      case BotChatCommands.Change:
        {
          var replyMarkup = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("Имя", BotChatCommands.ChangeName)],
          [InlineKeyboardButton.WithCallbackData("Город", BotChatCommands.ChangeCity)],
          [InlineKeyboardButton.WithCallbackData("Направление", BotChatCommands.ChangeWork)],
          [InlineKeyboardButton.WithCallbackData("Увлечения", BotChatCommands.ChangeHobby)],
          [InlineKeyboardButton.WithCallbackData("О чём хочешь пообщаться", BotChatCommands.ChangeInterests)],
          [InlineKeyboardButton.WithCallbackData(BotMessages.BackButton, BotChatCommands.Start)]
          ]);
          await botClient.SendTextMessageAsync(
            userId,
            "Выбери, что хочешь изменить",
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);

          this.userScenarioRepository?.Remove(userId);
          break;
        }

      case BotChatCommands.ChangeName:
        {
          userScenario = new UserCommandScenario(userId, new ChangeNameScenario());
          break;
        }

      case BotChatCommands.ChangeCity:
        {
          userScenario = new UserCommandScenario(userId, new ChangeCityScenario());
          break;
        }

      case BotChatCommands.ChangeWork:
        {
          userScenario = new UserCommandScenario(userId, new ChangeWorkScenario());
          break;
        }

      case BotChatCommands.ChangeHobby:
        {
          userScenario = new UserCommandScenario(userId, new ChangeHobbyScenario());
          break;
        }

      case BotChatCommands.ChangeInterests:
        {
          userScenario = new UserCommandScenario(userId, new ChangeInterestsScenario());
          break;
        }

      case BotChatCommands.GeneratePairs:
        {
          if (userId != new BotConfigManager().Config.BotAdminId.FirstOrDefault())
            return;

          var profiles = BotDbContext.Instance.UserInfos
            .Where(i => !i.SearchDisable && !i.PairFound).ToList();
          var profilesDictionary = profiles
            .Where(p => !CheckIsInfoEmpty(p))
            .ToDictionary(
              k => k.UserId, v =>
            {
              var sb = new StringBuilder(v.Interests);
              sb.AppendLine();
              sb.AppendLine(v.Hobby);
              return sb.ToString();
            });

          await botClient.SendTextMessageAsync(
            userId,
            "start generating pairs...",
            cancellationToken: cancellationToken);
          try
          {
            new PairGenerator().GeneratePairs(profilesDictionary);
          }
#pragma warning disable CA1031
          catch (Exception e)
#pragma warning restore CA1031
          {
            Console.WriteLine(e);
          }
          finally
          {
            await botClient.SendTextMessageAsync(
              userId,
              "generating pairs completed",
              cancellationToken: cancellationToken);
          }

          break;
        }

      case BotChatCommands.SendPairs:
        {
          try
          {
            if (userId != new BotConfigManager().Config.BotAdminId.FirstOrDefault())
              return;

            var pairs = BotDbContext.Instance.CoffeePairs.ToList()
              .Where(p => p.PairingDate.Date == DateTime.Today)
              .ToList();

            foreach (var coffeePair in pairs)
            {
              var firstUserInfo = BotDbContext.Instance.UserInfos.FirstOrDefault(i => i.UserId == coffeePair.FirstUserId);
              var secondUserInfo = BotDbContext.Instance.UserInfos.FirstOrDefault(i => i.UserId == coffeePair.SecondUserId);

              if (coffeePair.SecondUserId != -1)
              {
                if (secondUserInfo != null)
                {
                  await botClient.SendTextMessageAsync(
                    coffeePair.FirstUserId,
                    string.Format(
                      BotMessages.PairFoundMessage,
                      secondUserInfo.Name,
                      secondUserInfo.Hobby,
                      secondUserInfo.Work,
                      secondUserInfo.Interests),
                    cancellationToken: cancellationToken);
                }
                if (firstUserInfo != null)
                {
                  await botClient.SendTextMessageAsync(
                    coffeePair.SecondUserId,
                    string.Format(
                      BotMessages.PairFoundMessage,
                      firstUserInfo.Name,
                      firstUserInfo.Hobby,
                      firstUserInfo.Work,
                      firstUserInfo.Interests),
                    cancellationToken: cancellationToken);
                }
              }
              else
              {
                var reply = new InlineKeyboardMarkup(
                  InlineKeyboardButton.WithCallbackData(
                    "Найти случайного собеседника",
                    BotChatCommands.RandomPair));
                await botClient.SendTextMessageAsync(
                  coffeePair.FirstUserId,
                  BotMessages.PairNotFoundMessage,
                  replyMarkup: reply,
                  cancellationToken: cancellationToken);
              }
            }
          }
#pragma warning disable CA1031
          catch
#pragma warning restore CA1031
          {
            // Игнорируем исключения.
          }

          break;
        }

      case BotChatCommands.RandomPair:
        {
          try
          {
            var userWithNoPair = BotDbContext.Instance.CoffeePairs
                .ToList()
                .FirstOrDefault(p => p.SecondUserId == -1 && p.FirstUserId != userId && p.PairingDate.Date == DateTime.Today);
            if (userWithNoPair == null)
            {
              await botClient.SendTextMessageAsync(
                userId,
                BotMessages.PairNotFoundCompletelyMessage,
                cancellationToken: cancellationToken);
              break;
            }

            userWithNoPair.SecondUserId = userId;
            var currentUser = BotDbContext.Instance.CoffeePairs
              .FirstOrDefault(p => p.FirstUserId == userId);

            if (currentUser != null)
              currentUser.SecondUserId = userWithNoPair.FirstUserId;

            await BotDbContext.Instance.SaveChangesAsync(cancellationToken);

            var firstUserInfo = BotDbContext.Instance.UserInfos
              .ToList()
              .FirstOrDefault(i => i.UserId == userId);
            var secondUserInfo = BotDbContext.Instance.UserInfos
              .ToList()
              .FirstOrDefault(i => i.UserId == userWithNoPair.FirstUserId);

            if (secondUserInfo != null)
            {
              await botClient.SendTextMessageAsync(
                userId,
                string.Format(
                  BotMessages.PairFoundMessage,
                  secondUserInfo.Name,
                  secondUserInfo.Hobby,
                  secondUserInfo.Work,
                  secondUserInfo.Interests),
                cancellationToken: cancellationToken);
            }
            if (firstUserInfo != null)
            {
              await botClient.SendTextMessageAsync(
                userWithNoPair.FirstUserId,
                string.Format(
                  BotMessages.PairFoundMessage,
                  firstUserInfo.Name,
                  firstUserInfo.Hobby,
                  firstUserInfo.Work,
                  firstUserInfo.Interests),
                cancellationToken: cancellationToken);
            }
          }
#pragma warning disable CA1031
          catch (Exception e)
#pragma warning restore CA1031
          {
            Console.WriteLine(e);
          }

          break;
        }

      case "/broadcast":
        {
          // var pairs = BotDbContext.Instance.CoffeePairs
          //     .Where(p => p.FirstUserId != -1)
          //     .Select(p => p.FirstUserId)
          //     .Union(BotDbContext.Instance.CoffeePairs
          //         .Where(p => p.SecondUserId != -1)
          //         .Select(p => p.SecondUserId))
          //     .Distinct();
          // var users = BotDbContext.Instance.BotUsers
          //     .Where(u => pairs.Contains(u.Id))
          //     .ToList();
          // var message =
          //     "\ud83d\udc4b Привет, в понедельник пришлю тебе нового собеседника, а пока успей назначить встречу с текущим\\.\n\nДля лучших совпадений пропиши больше своих интересов, так выше вероятность найти подходящего собеседника\\. \n\nМожешь поменять \"обо всём\" на перечисление своих хобби и увлечений \u2728";
          // BroadcastMessageSender.BroadcastMessage(botClient, users, message);
          break;
        }
    }

    if (userScenario == null
        && this.userScenarioRepository != null
        && this.userScenarioRepository.TryGet(userId, out var existingUserScenario))
    {
      userScenario = existingUserScenario;
    }
    else
    {
      this.userScenarioRepository?.AddOrReplace(userScenario);
    }

    if (userScenario != null && !(await userScenario.Run(botClient, update, userId)))
      this.userScenarioRepository?.Remove(userScenario);
  }

  /// <inheritdoc />
  public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
  {
    log.Error(exception);
    Environment.Exit(0);
    return Task.CompletedTask;
  }

  #endregion

  #region Методы

  /// <summary>
  /// Заполнить информацию о пользователе.
  /// </summary>
  /// <param name="userId">ИД пользователя.</param>
  private static void FillUserSystemInfo(long userId)
  {
    var info = BotDbContext.Instance.UserInfos
      .FirstOrDefault(i => i.UserId == userId);
    if (info == null)
    {
      var userInfo = new UserInfo
      {
        Name = string.Empty,
        City = string.Empty,
        Hobby = string.Empty,
        Work = string.Empty,
        Interests = string.Empty,
        UserId = userId,
        PairFound = false,
        SearchDisable = false,
        KeyWords = []
      };
      BotDbContext.Instance.UserInfos.Add(userInfo);
      BotDbContext.Instance.SaveChanges();
    }
  }

  /// <summary>
  /// Проверить, что информация пользователя пуста.
  /// </summary>
  /// <param name="info">Инфо пользователя, которое нужно проверить.</param>
  /// <returns><c>True</c>, если информации нет.</returns>
  private static bool CheckIsInfoEmpty(UserInfo info)
  {
    return string.IsNullOrEmpty(info.Interests);
  }

  #endregion

  #region Конструкторы

  /// <summary>
  /// Конструктор.
  /// </summary>
  public BotUpdateHandler()
  {
    this.userScenarioRepository = new UserScenarioRepository();
  }

  #endregion"

}