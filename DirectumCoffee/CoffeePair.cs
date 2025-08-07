using System;
using System.ComponentModel.DataAnnotations;
using BotCommon.Repository;

namespace DirectumCoffee;

public class CoffeePair
{
  [Key]
  public int Id { get; set; }

  public long FirstUserId { get; set; }

  public BotUser FirstUser { get; set; }

  public long SecondUserId { get; set; }

  public string[] CommonInterests { get; set; }

  public DateTime PairingDate { get; set; }
}