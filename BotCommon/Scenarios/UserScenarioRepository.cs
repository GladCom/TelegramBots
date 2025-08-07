using System.Collections.Generic;
using System.Linq;

namespace BotCommon.Scenarios;

public sealed class UserScenarioRepository
{
  private List<UserCommandScenario> _userScenarios = [];

  public bool TryGet(long userId, out UserCommandScenario userCommandScenario)
  {
    userCommandScenario = this.Get(userId);
    return userCommandScenario != null;
  }

  public UserCommandScenario Get(long userId)
  {
    var foundedScenario = this._userScenarios.SingleOrDefault(s => s.UserId == userId);
    return foundedScenario;
  }

  public void Remove(long userId)
  {
    var foundedScenario = this.Get(userId);
    if (foundedScenario == null)
      return;
    foundedScenario.CommandScenario.Reset();
    this._userScenarios.Remove(foundedScenario);
  }

  public void Remove(UserCommandScenario commandScenario)
  {
    if (commandScenario == null)
      return;
    this.Remove(commandScenario.UserId);
  }

  public void AddOrReplace(UserCommandScenario userCommandScenario)
  {
    if (userCommandScenario == null)
      return;
    if (this.TryGet(userCommandScenario.UserId, out var _userCommandScenario))
      this.Remove(_userCommandScenario);
    this._userScenarios.Add(userCommandScenario);
  }
}