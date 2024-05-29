﻿// <auto-generated />
using System;
using Gambler.Bot.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Gambler.Bot.AutoBet.Migrations
{
    [DbContext(typeof(BotContext))]
    partial class BotContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("Gambler.Bot.AutoBet.Helpers.SessionStats", b =>
                {
                    b.Property<int>("SessionStatsId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("AvgLoss")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("AvgStreak")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("AvgWin")
                        .HasColumnType("TEXT");

                    b.Property<long>("BestStreak")
                        .HasColumnType("INTEGER");

                    b.Property<long>("BestStreak2")
                        .HasColumnType("INTEGER");

                    b.Property<long>("BestStreak3")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Bets")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("CurrentProfit")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("LargestBet")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("LargestLoss")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("LargestWin")
                        .HasColumnType("TEXT");

                    b.Property<long>("LossStreak")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Losses")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Luck")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("MaxProfit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("MaxProfitSinceReset")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("MinProfit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("MinProfitSinceReset")
                        .HasColumnType("TEXT");

                    b.Property<long>("NumLossStreaks")
                        .HasColumnType("INTEGER");

                    b.Property<long>("NumStreaks")
                        .HasColumnType("INTEGER");

                    b.Property<long>("NumWinStreaks")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("PorfitSinceLimitAction")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Profit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("ProfitPer24Hour")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("ProfitPerBet")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("ProfitPerHour")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("ProfitSinceLastReset")
                        .HasColumnType("TEXT");

                    b.Property<long>("RunningTime")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Simulation")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("StreakLossSinceLastReset")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("StreakProfitSinceLastReset")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Wagered")
                        .HasColumnType("TEXT");

                    b.Property<long>("WinStreak")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Wins")
                        .HasColumnType("INTEGER");

                    b.Property<long>("WorstStreak")
                        .HasColumnType("INTEGER");

                    b.Property<long>("WorstStreak2")
                        .HasColumnType("INTEGER");

                    b.Property<long>("WorstStreak3")
                        .HasColumnType("INTEGER");

                    b.Property<long>("laststreaklose")
                        .HasColumnType("INTEGER");

                    b.Property<long>("laststreakwin")
                        .HasColumnType("INTEGER");

                    b.Property<long>("winsAtLastReset")
                        .HasColumnType("INTEGER");

                    b.HasKey("SessionStatsId");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Games.CrashBet", b =>
                {
                    b.Property<string>("BetID")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Crash")
                        .HasColumnType("TEXT");

                    b.Property<string>("Currency")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Date")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Edge")
                        .HasColumnType("TEXT");

                    b.Property<string>("Guid")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsWin")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Payout")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Profit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("TEXT");

                    b.Property<long>("Userid")
                        .HasColumnType("INTEGER");

                    b.HasKey("BetID");

                    b.ToTable("CrashBets");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Games.DiceBet", b =>
                {
                    b.Property<string>("BetID")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Chance")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClientSeed")
                        .HasColumnType("TEXT");

                    b.Property<string>("Currency")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Date")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Edge")
                        .HasColumnType("TEXT");

                    b.Property<string>("Guid")
                        .HasColumnType("TEXT");

                    b.Property<bool>("High")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsWin")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Nonce")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Profit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Roll")
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerSeed")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("TEXT");

                    b.Property<long>("Userid")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WinnableType")
                        .HasColumnType("INTEGER");

                    b.HasKey("BetID");

                    b.ToTable("DiceBets");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Games.PlinkoBet", b =>
                {
                    b.Property<string>("BetID")
                        .HasColumnType("TEXT");

                    b.Property<string>("Currency")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Date")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Edge")
                        .HasColumnType("TEXT");

                    b.Property<string>("Guid")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsWin")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Profit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("TEXT");

                    b.Property<long>("Userid")
                        .HasColumnType("INTEGER");

                    b.HasKey("BetID");

                    b.ToTable("PlinkoBets");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Games.RouletteBet", b =>
                {
                    b.Property<string>("BetID")
                        .HasColumnType("TEXT");

                    b.Property<string>("Currency")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Date")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Edge")
                        .HasColumnType("TEXT");

                    b.Property<string>("Guid")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsWin")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Profit")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("TotalAmount")
                        .HasColumnType("TEXT");

                    b.Property<long>("Userid")
                        .HasColumnType("INTEGER");

                    b.HasKey("BetID");

                    b.ToTable("RouletteBets");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Sites.Classes.Currency", b =>
                {
                    b.Property<string>("Symbol")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Icon")
                        .HasColumnType("BLOB");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Symbol");

                    b.ToTable("Currencies");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Sites.Classes.SeedDetails", b =>
                {
                    b.Property<string>("ServerHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClientSeed")
                        .HasColumnType("TEXT");

                    b.Property<long?>("Nonce")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PreviousClient")
                        .HasColumnType("TEXT");

                    b.Property<string>("PreviousHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("PreviousServer")
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerSeed")
                        .HasColumnType("TEXT");

                    b.HasKey("ServerHash");

                    b.ToTable("Seeds");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Sites.Classes.SiteDetails", b =>
                {
                    b.Property<string>("name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Currencies")
                        .HasColumnType("TEXT");

                    b.Property<string>("Games")
                        .HasColumnType("TEXT");

                    b.Property<bool>("caninvest")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("canresetseed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("cantip")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("canwithdraw")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("edge")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("maxroll")
                        .HasColumnType("TEXT");

                    b.Property<string>("siteurl")
                        .HasColumnType("TEXT");

                    b.Property<bool>("tipusingname")
                        .HasColumnType("INTEGER");

                    b.HasKey("name");

                    b.ToTable("Sites");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Storage.User", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Sitename")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserName")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.HasIndex("Sitename");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Gambler.Bot.Core.Storage.User", b =>
                {
                    b.HasOne("Gambler.Bot.Core.Sites.Classes.SiteDetails", "Site")
                        .WithMany()
                        .HasForeignKey("Sitename");

                    b.Navigation("Site");
                });
#pragma warning restore 612, 618
        }
    }
}
