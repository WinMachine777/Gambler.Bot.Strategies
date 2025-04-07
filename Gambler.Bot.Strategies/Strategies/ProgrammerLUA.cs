﻿using Gambler.Bot.Strategies.Helpers;
using Gambler.Bot.Strategies.Strategies.Abstractions;
using Gambler.Bot.Common.Helpers;
using Microsoft.Extensions.Logging;
using NLua;
using System;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.Crash;
using Gambler.Bot.Common.Games.Plinko;
using Gambler.Bot.Common.Games.Roulette;
using Gambler.Bot.Common.Games;
using static IronPython.Modules._ast;
using Mono.Unix.Native;

namespace Gambler.Bot.Strategies.Strategies
{
    public class ProgrammerLUA : BaseStrategy, IProgrammerMode
    {
        public override string StrategyName { get; protected set; } = "ProgrammerLUA";
        public string FileName { get; set; }
        public bool High { get ; set ; }
        public decimal Amount { get ; set ; }
        public decimal Chance { get ; set ; }
        public decimal StartChance { get ; set ; }


        Lua CurrentRuntime = null;

        public event EventHandler<WithdrawEventArgs> OnWithdraw;
        public event EventHandler<InvestEventArgs> OnInvest;
        public event EventHandler<TipEventArgs> OnTip;
        public event EventHandler<EventArgs> OnResetSeed;
        public event EventHandler<PrintEventArgs> OnPrint;
        public event EventHandler<RunSimEventArgs> OnRunSim;
        public event EventHandler<EventArgs> OnResetStats;
        public event EventHandler<EventArgs> OnResetProfit;
        public event EventHandler<EventArgs> OnResetPartialProfit;
        public event EventHandler<ReadEventArgs> OnRead;
        public event EventHandler<ReadEventArgs> OnReadAdv;
        public event EventHandler<EventArgs> OnAlarm;
        public event EventHandler<EventArgs> OnChing;
        public event EventHandler<EventArgs> OnResetBuiltIn;
        public event EventHandler<ExportSimEventArgs> OnExportSim;
        public event EventHandler<PrintEventArgs> OnScriptError;
        public event EventHandler<PrintEventArgs> OnSetCurrency;
        public event EventHandler<InvestEventArgs> OnBank;

        public ProgrammerLUA(ILogger logger) : base(logger)
        {

        }
        public ProgrammerLUA()
        {
            
        }

        protected override PlaceBet NextBet(Bet PreviousBet, bool Win)
        {
            try
            {
                PlaceBet NextBet = PreviousBet.CreateRetry();
                
                
                LuaFunction DoDiceBet = CurrentRuntime.GetFunction("CalculateBet");
                if (DoDiceBet != null )
                {
                    object[] Result = DoDiceBet.Call(PreviousBet, Win, NextBet);
                }
                else
                {
                    
                    DoDiceBet = CurrentRuntime.GetFunction("dobet");
                    if (DoDiceBet != null && NextBet is PlaceDiceBet nxt)
                    {
                        CurrentRuntime["previousbet"] = PreviousBet.TotalAmount;
                        CurrentRuntime["nextbet"] = PreviousBet.TotalAmount;
                        CurrentRuntime["win"] = PreviousBet.IsWin;
                        CurrentRuntime["bethigh"] = nxt.High;
                        CurrentRuntime["chance"] = nxt.Chance;
                        CurrentRuntime["currentprofit"] = ((decimal)(PreviousBet.Profit * 100000000m)) / 100000000.0m;
                        CurrentRuntime["lastBet"] = PreviousBet;
                        object[] Result = DoDiceBet.Call();
                        nxt.Chance = (decimal)(double)CurrentRuntime["chance"];
                        nxt.Amount = (decimal)(double)CurrentRuntime["nextbet"];
                        nxt.High = (bool)CurrentRuntime["bethigh"];
                        
                    }
                }
                return NextBet;
            }
            //catch (InternalErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message=e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (SyntaxErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (ScriptRuntimeException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //   // throw e;
            //}
            catch (Exception e)
            {
                OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.ToString() });
               // throw e;
            }
            return null;
        }

        public override void OnError(BotErrorEventArgs ErrorDetails)
        {
            LuaFunction CallError = CurrentRuntime.GetFunction("OnError");
            if (CallError != null)
            {
                object[] Result = CallError.Call( ErrorDetails);
            }            
        }

        //public override PlaceCrashBet CalculateNextCrashBet(CrashBet PreviousBet, bool Win)
        //{
        //    PlaceCrashBet NextBet = new PlaceCrashBet();
        //    object[] DoDiceBet = CurrentRuntime.GetFunction("DoCrashBet");
        //    if (DoDiceBet != null)
        //    {
        //        object[] Result = CurrentRuntime.Call(DoDiceBet, PreviousBet, Win, NextBet);
        //    }
        //    return NextBet;
        //}

        //public override PlacePlinkoBet CalculateNextPlinkoBet(PlinkoBet PreviousBet, bool Win)
        //{
        //    PlacePlinkoBet NextBet = new PlacePlinkoBet();
        //    object[] DoDiceBet = CurrentRuntime.GetFunction("DoPlinkoBet");
        //    if (DoDiceBet != null)
        //    {
        //        object[] Result = CurrentRuntime.Call(DoDiceBet, PreviousBet, Win, NextBet);
        //    }
        //    return NextBet;
        //}

        //public override PlaceRouletteBet CalculateNextRouletteBet(RouletteBet PreviousBet, bool Win)
        //{
        //    PlaceRouletteBet NextBet = new PlaceRouletteBet();
        //    object[] DoDiceBet = CurrentRuntime.GetFunction("DoRouletteBet");
        //    if (DoDiceBet != null)
        //    {
        //        object[] Result = CurrentRuntime.Call(DoDiceBet, PreviousBet, Win, NextBet);
        //    }
        //    return NextBet;
        //}
       

        public void CreateRuntime()
        {
            CurrentRuntime = new Lua();
            //UserData.RegisterAssembly();
            /*UserData.RegisterType<SessionStats>();
            UserData.RegisterType < PlaceDiceBet>();
            UserData.RegisterType < DiceBet>();
            UserData.RegisterType < SiteDetails>();
            UserData.RegisterType < SiteStats>();
            UserData.RegisterType < CrashBet>();
            UserData.RegisterType < PlaceCrashBet>();
            UserData.RegisterType < PlinkoBet>();
            UserData.RegisterType < PlacePlinkoBet>();
            UserData.RegisterType < PlaceRouletteBet>();
            UserData.RegisterType < RouletteBet>();*/
            CurrentRuntime["Withdraw"] = (Action<string,decimal>)Withdraw;
            CurrentRuntime["Bank"] = (Action<decimal>)Bank;
            CurrentRuntime["Invest"] = (Action< decimal>)Invest;
            CurrentRuntime["Tip"] = (Action<string, decimal>)Tip;
            CurrentRuntime["ResetSeed"] = (Action)ResetSeed;
            CurrentRuntime["Print"] = (Action<object>)Print;
            CurrentRuntime["RunSim"] = (Action < decimal, long, bool>)RunSim;
            CurrentRuntime["ResetStats"] = (Action)ResetStats;
            CurrentRuntime["Read"] = (Func<string, int, object>)Read;
            CurrentRuntime["Readadv"] = (Func<string, int,string,string,string, object> )Readadv;
            CurrentRuntime["Alarm"] = (Action)Alarm;
            CurrentRuntime["Ching"] = (Action)Ching;
            CurrentRuntime["ResetBuiltIn"] = (Action)ResetBuiltIn;
            CurrentRuntime["ExportSim"] = (Action<string>)ExportSim;
            CurrentRuntime["Stop"] = (Action)_Stop;
            CurrentRuntime["SetCurrency"] = (Action<string>)SetCurrency;


            //legacy support
            CurrentRuntime["withdraw"] = (Action<string, decimal>)Withdraw; ;
            CurrentRuntime["invest"] = (Action<decimal>)Invest; ;
            CurrentRuntime["tip"] = (Action<string, decimal>)Tip; ;
            CurrentRuntime["stop"] = (Action)_Stop; 
            CurrentRuntime["resetseed"] = (Action)ResetSeed; ;
            CurrentRuntime["print"] = (Action<string>)Print; ;
            /*CurrentRuntime["getHistory"] = luagethistory;
            CurrentRuntime["getHistoryByDate"] = luagethistory;
            CurrentRuntime["getHistoryByQuery"] = QueryHistory;*/
            /*CurrentRuntime["runsim"] = runsim;
            CurrentRuntime["martingale"] = LuaMartingale;
            CurrentRuntime["labouchere"] = LuaLabouchere;
            CurrentRuntime["fibonacci"] = LuaFibonacci;
            CurrentRuntime["dalembert"] = LuaDAlember;
            CurrentRuntime["presetlist"] = LuaPreset;*/
            CurrentRuntime["resetstats"] = (Action)ResetStats;

            CurrentRuntime["resetprofit"]  = (Action)ResetProfit;
            CurrentRuntime["resetpartialprofit"] = (Action)ResetPartialProfit;
            /*CurrentRuntime["setvalueint"] = LuaSetValue;
            CurrentRuntime["setvaluestring"] = LuaSetValue;
            CurrentRuntime["setvaluedecimal"] = LuaSetValue;
            CurrentRuntime["setvaluebool"] = LuaSetValue;
            CurrentRuntime["getvalue"] = LuaGetValue;
            CurrentRuntime["loadstrategy"] = LuaLoadStrat;*/
            CurrentRuntime["read"] = (Func<string, int, object>)Read; ;
            CurrentRuntime["readadv"] = (Func<string, int, string, string, string, object>)Readadv; ;
            CurrentRuntime["alarm"] = (Action)Alarm; ;
            CurrentRuntime["ching"] = (Action)Ching; ;
            CurrentRuntime["resetbuiltin"] =(Action)NoAction;
            CurrentRuntime["exportsim"] = (Action<string>)ExportSim;
            CurrentRuntime["vault"] = (Action<decimal>)Bank; 

        }

        private void NoAction()
        {

        }

        public void LoadScript()
        {

            try
            {
                CurrentRuntime["Stats"] = Stats;
                CurrentRuntime["Balance"] = this.Balance;
                CurrentRuntime.DoFile(FileName);
            }
            //catch (InternalErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (SyntaxErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (ScriptRuntimeException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            catch (Exception e)
            {
                OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.ToString() });
                //throw e;
            }
        }

        public override PlaceBet RunReset(Games Game)
        {
            try
            {
                LuaFunction DoDiceBet = CurrentRuntime.GetFunction("Reset");
                if (DoDiceBet != null)
                {
                    PlaceBet NextBet = CreateEmptyPlaceBet(Game);
                    object[] Result = DoDiceBet.Call(NextBet, Game);
                    return NextBet;
                }
                else if (Game == Games.Dice)
                {
                    PlaceDiceBet NextBet = CreateEmptyPlaceBet(Game) as PlaceDiceBet;
                    //(decimal)CurrentRuntime["chance"];
                    NextBet.Amount = (decimal)(double)CurrentRuntime["nextbet"];
                    NextBet.Chance = (decimal)(double)CurrentRuntime["chance"];
                    NextBet.High = (bool)CurrentRuntime["bethigh"];
                    return NextBet;
                    //if (CurrentSite.Currency != (string)Lua["currency"])
                    //    CurrentSite.Currency = (string)Lua["currency"];
                    //EnableReset = (bool)Lua["enablersc"];
                    //EnableProgZigZag = (bool)Lua["enablezz"];
                }
            }
            //catch (InternalErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (SyntaxErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (ScriptRuntimeException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            catch (Exception e)
            {
                OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.ToString() });
                //throw e;
            }
            return null;
        }

        public void UpdateSessionStats(SessionStats Stats)
        {
            CurrentRuntime["Stats"] = Stats;
            CurrentRuntime["Balance"] = this.Balance;
            SetLegacyVars();
        }

        public void UpdateSite(SiteDetails Details)
        {
            CurrentRuntime["SiteDetails"] = Details;
            CurrentRuntime["site"] = Details;
            CurrentRuntime["currencies"] = Details.Currencies;
        }

        public void UpdateSiteStats(SiteStats Stats)
        {
            CurrentRuntime["SiteStats"] = Stats;
        }

        public void SetSimulation(bool IsSimulation)
        {
            CurrentRuntime["InSimulation"] = IsSimulation;
        }

        void Withdraw(string Address, decimal Amount)
        {
            OnWithdraw?.Invoke(this, new WithdrawEventArgs { Address=Address, Amount=Amount });            
        }

        void Bank(decimal Amount)
        {
            OnBank?.Invoke(this, new InvestEventArgs { Amount = Amount });
        }
        void Invest(decimal Amount)
        {
            OnInvest?.Invoke(this, new InvestEventArgs { Amount=Amount });
        }
        void Tip(string Receiver, decimal Amount)
        {
            OnTip?.Invoke(this, new TipEventArgs { Receiver=Receiver, Amount=Amount });
        }        
        void ResetSeed()
        {
            OnResetSeed?.Invoke(this, new EventArgs());
        }
        void Print(object PrintValue)
        {
            OnPrint?.Invoke(this, new PrintEventArgs { Message = PrintValue?.ToString() });
        }
        void RunSim(decimal Balance, long Bets, bool log)
        {
            OnRunSim?.Invoke(this, new RunSimEventArgs { Balance=Balance, Bets=Bets, WriteLog=log });
        }
        void ResetStats()
        {
            OnResetStats?.Invoke(this, new EventArgs());
        }
        void ResetProfit()
        {
            OnResetProfit?.Invoke(this, new EventArgs());
        }
        void ResetPartialProfit()
        {
            OnResetPartialProfit?.Invoke(this, new EventArgs());
        }
        void _Stop()
        {
            CallStop("Stop function used in programmer mode");
        }
        object Read(string prompt, int DataType)
        {
            ReadEventArgs tmpArgs = new ReadEventArgs { Prompt= prompt, DataType= DataType };
            OnRead?.Invoke(this, tmpArgs);
            return tmpArgs.Result;
        }
        object Readadv(string prompt, int DataType, string userinputext, string btncanceltext, string btnoktext)
        {
            ReadEventArgs tmpArgs = new ReadEventArgs { Prompt = prompt, DataType = DataType, userinputext=userinputext, btncanceltext=btncanceltext, btnoktext=btnoktext };
            OnReadAdv?.Invoke(this, tmpArgs);
            return tmpArgs.Result;
        }
        void Alarm()
        {
            OnAlarm?.Invoke(this, new EventArgs());
        }
        void Ching()
        {
            OnChing?.Invoke(this, new EventArgs());
        }
        void ResetBuiltIn()
        {
            OnResetBuiltIn?.Invoke(this, new EventArgs());
        }
        void ExportSim(string FileName)
        {
            OnExportSim?.Invoke(this, new ExportSimEventArgs { FileName = FileName});
        }

        public void ExecuteCommand(string Command)
        {
            try
            {
                CurrentRuntime.DoString(Command);
            }
            //catch (InternalErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (SyntaxErrorException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            //catch (ScriptRuntimeException e)
            //{
            //    OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.DecoratedMessage });
            //    //throw e;
            //}
            catch (Exception e)
            {
                OnScriptError?.Invoke(this, new PrintEventArgs { Message = e.ToString() });
                //throw e;
            }
        }
        public void SetCurrency(string newCurrency)
        {               
            OnSetCurrency?.Invoke(this, new PrintEventArgs { Message = newCurrency });
        }

        void SetLegacyVars()
        {
            try
            {
                
                //Lua.clear();
                CurrentRuntime["balance"] = this.Balance;
                CurrentRuntime["profit"] = this.Stats.Profit;
                CurrentRuntime["currentstreak"] = (this.Stats.WinStreak > 0) ? this.Stats.WinStreak : -this.Stats.LossStreak;
                CurrentRuntime["previousbet"] = Amount;
                CurrentRuntime["nextbet"] = Amount;
                CurrentRuntime["chance"] = Chance;
                CurrentRuntime["bethigh"] = High;
                CurrentRuntime["bets"] = this.Stats.Wins + this.Stats.Losses;
                CurrentRuntime["wins"] = this.Stats.Wins;
                CurrentRuntime["losses"] = this.Stats.Losses;
                CurrentRuntime["wagered"] = Stats.Wagered;
                CurrentRuntime["partialprofit"] = this.Stats.PartialProfit;
                

            }
            catch (Exception e)
            {
                _Logger?.LogError(e.Message, 1);
                _Logger?.LogError(e.StackTrace, 2);
                CallStop("LUA ERROR!!");
                Print("LUA ERROR!!");
                Print(e.Message);
            }
        }

        
    }
}
