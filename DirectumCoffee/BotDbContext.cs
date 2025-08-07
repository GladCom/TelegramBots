using System;
using System.Linq;
using BotCommon.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DirectumCoffee;

/// <summary>
/// DBContext для бота.
/// </summary>
internal sealed class BotDbContext : UserDbContext
{
  #region Поля и свойства

  /// <summary>
  /// Объект для ленивой загрузки инстанса.
  /// </summary>
  private static readonly Lazy<BotDbContext> lazy = new Lazy<BotDbContext>(() => new BotDbContext());

  /// <summary>
  /// Экземпляр класса.
  /// </summary>
  public static BotDbContext Instance => lazy.Value;

  /// <summary>
  /// Набор информации о пользователях.
  /// </summary>
  public DbSet<UserInfo> UserInfos { get; set; }

  /// <summary>
  /// Набор образованных пар пользователей.
  /// </summary>
  public DbSet<CoffeePair> CoffeePairs { get; set; }

  #endregion

  #region Базовый класс

  /// <inheritdoc/>
  protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseSqlite(this._connectionString);

  /// <inheritdoc/>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<UserInfo>()
      .HasOne(u => u.BotUser)
      .WithMany()
      .HasForeignKey(u => u.UserId);
    modelBuilder.Entity<UserInfo>()
      .Property(cp => cp.KeyWords)
      .HasConversion(
        v => string.Join(',', v),
        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

    modelBuilder.Entity<CoffeePair>()
      .HasOne(cp => cp.FirstUser)
      .WithMany()
      .HasForeignKey(cp => cp.FirstUserId);
    modelBuilder.Entity<CoffeePair>()
      .Property(cp => cp.CommonInterests)
      .HasConversion(
        v => string.Join(',', v),
        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
  }

  #endregion

  #region Конструкторы

  /// <summary>
  /// Конструктор.
  /// </summary>
  private BotDbContext()
    : base("Filename=coffee.db")
  {
    this.Database.EnsureCreated();

    var creator = this.GetService<IRelationalDatabaseCreator>();
    if (!creator.Exists())
      creator.CreateTables();
  }

  #endregion

}