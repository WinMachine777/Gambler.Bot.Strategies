﻿using Gambler.Bot.AutoBet.Helpers;
using Gambler.Bot.AutoBet.Strategies;
using Gambler.Bot.Core.Enums;
using Gambler.Bot.Core.Events;
using Gambler.Bot.Core.Games;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites;
using Gambler.Bot.Core.Sites.Classes;
using Gambler.Bot.Core.Storage;
using Gambler.Bot.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using static Gambler.Bot.AutoBet.Helpers.PersonalSettings;
using ErrorEventArgs = Gambler.Bot.Core.Events.ErrorEventArgs;

namespace Gambler.Bot.AutoBet
{
    public class AutoBet: INotifyPropertyChanged
    {
        #region Internal Variables
        private readonly ILogger _Logger;
        List<Core.Events.ErrorEventArgs> ActiveErrors = new List<ErrorEventArgs>();
        //PwDatabase Passdb = new PwDatabase();
        System.Timers.Timer BetTimer = new System.Timers.Timer { Interval=1000, Enabled=false, AutoReset=true };

        public bool KeepassOpen
        {
            get { return false;// Passdb.IsOpen;
                               }            
        }


        BotContext DBInterface { get; set; }

        string LastBetGuid = "";
        Queue<string> LastBetsGuids = new Queue<string>();

        /// <summary>
        /// Indicates that the bot is currently running placing bets
        /// </summary>
        public bool Running { get; private set; }      

        public bool RunningSimulation { get; private set; }
        private long totalRuntime = new long();

        public long TotalRuntime
        {
            get { return totalRuntime ; }
            set { totalRuntime = value; }
        }

        public bool LoggedIn { get; set; }
        public SessionStats Stats 
        { 
            get; 
            set; 
        }

        public ExportBetSettings StoredBetSettings { get; set; } = new ExportBetSettings { BetSettings = new InternalBetSettings() };

        public InternalBetSettings BetSettings 
        { 
            get => StoredBetSettings.BetSettings; 
            set => StoredBetSettings.BetSettings=value; 
        }

        public PersonalSettings PersonalSettings { get; set; } = new PersonalSettings();
        Bet MostRecentBet = null;
        DateTime MostRecentBetTime = new DateTime();
        PlaceBet NextBext = null;
        int Retries = 0;
        public bool StopOnWin { get; set; } = false;
        //internal variables
        #endregion

        string VersionStr = "";


        public AutoBet(ILogger logger)
        {
            _Logger = logger;
            VersionStr = string.Format("{0}.{1}.{2}", Environment.Version.Major, Environment.Version.MajorRevision, Environment.Version.Build);
            Stats = new SessionStats();
            Running = false;
            BetTimer.Elapsed += BetTimer_Elapsed;
            CurrentGame = Games.Dice;
        }

        

        public static List<SitesList> Sites = new List<SitesList>();
        public SitesList[] CompileSites()
        {
            if (Sites?.Count == 0)
            {
                _Logger?.LogInformation("Compiling Sites");
                List<string> Files = new List<string>();

                Sites = new List<SitesList>();
                if (Directory.Exists("Sites"))
                {
                    _Logger?.LogDebug("Sites dir found, searching for files");
                    string Sites = "";
                    foreach (string s in Directory.GetFiles("Sites"))
                    {
                        _Logger?.LogDebug("Sites dir found, searching for files. Found {Description}", s);
                        string outs = File.ReadAllText(s);
                        Sites += outs;
                        Files.Add(outs);
                    }

                    //Compile site fies

                }
                //else
                {
                    _Logger?.LogDebug("Site dir not found, Stepping Through Types");
                    Assembly SiteAss = Assembly.GetAssembly(typeof(BaseSite));
                    Type[] tps = SiteAss.GetTypes();

                    List<string> sites = new List<string>();
                    foreach (Type x in tps)
                    {
                        _Logger?.LogDebug("Stepping Through Types - {Description}", x.Name);
                        if (x.IsSubclassOf(SiteAss.GetType("Gambler.Bot.Core.Sites.BaseSite")))
                        {
                            _Logger?.LogDebug("Found Type - " + x.Name, 6);
                            sites.Add(x.Name);
                            string[] currenices = new string[] { "btc" };
                            string url = "";
                            Games[] games = new Games[] { Games.Dice };
                            try
                            {
                                _Logger?.LogDebug("Fetching currencies for - {Description}",x.Name);
                                BaseSite SiteInst = Activator.CreateInstance(x, _Logger) as BaseSite;
                                currenices = (SiteInst).Currencies;
                                url = SiteInst.SiteURL;
                                if (DBInterface != null)
                                {
                                    DBInterface.Sites.Add(SiteInst.SiteDetails);

                                }
                            }
                            catch (Exception e)
                            {
                                _Logger?.LogError(e.ToString());
                            }
                            try
                            {
                                _Logger?.LogDebug("Fetching currencies for - {Description}",x.Name);

                                games = (Activator.CreateInstance(x, _Logger) as BaseSite).SupportedGames;
                            }
                            catch (Exception e)
                            {
                                _Logger?.LogError(e.ToString());
                            }
                            Sites.Add(new SitesList { Name = x.Name, Currencies = currenices, SupportedGames = games, URL= url }.SetType(x));
                            
                        }
                    }
                }
                _Logger?.LogInformation("Populated Sites");

                if (DBInterface != null)
                {
                    _Logger?.LogInformation("Updating Sites Table");
                    DBInterface.SaveChanges();
                }
                else
                {
                    _Logger?.LogInformation("Not Updating Sites Table");
                }
            }
            return Sites.ToArray();  
        }
        Dictionary<string, Type> strategies;
        public Dictionary<string, Type> Strategies { get => strategies; set { strategies = value; RaisePropertyChanged(); } }
        public Dictionary<string, Type> GetStrats()
        {
            var tmpStrategies = new Dictionary<string, Type>();
            Type[] tps = Assembly.GetAssembly(typeof(BaseStrategy)).GetTypes();
            List<string> sites = new List<string>();

            Type BaseTyope = Type.GetType("Gambler.Bot.AutoBet.Strategies.BaseStrategy");
            foreach (Type x in tps)
            { 
                if (x.IsSubclassOf(BaseTyope))
                {
                    tmpStrategies.Add((Activator.CreateInstance(x,_Logger) as BaseStrategy).StrategyName, x);
                }
            }
            Strategies = tmpStrategies;
            return Strategies;
        }

        #region Site Stuff
        private BaseSite baseSite;

        public BaseSite CurrentSite
        {
            get { return baseSite; }
            set
            {
                if (baseSite!=null)
                {
                    baseSite.Action -= BaseSite_Action;
                    baseSite.ChatReceived -= BaseSite_ChatReceived;
                    baseSite.BetFinished -= BaseSite_DiceBetFinished;
                    baseSite.Error -= BaseSite_Error;
                    baseSite.LoginFinished -= BaseSite_LoginFinished;
                    baseSite.Notify -= BaseSite_Notify;
                    baseSite.RegisterFinished -= BaseSite_RegisterFinished;
                    baseSite.StatsUpdated -= BaseSite_StatsUpdated;
                    baseSite.OnInvestFinished -= BaseSite_OnInvestFinished;
                    baseSite.OnResetSeedFinished -= BaseSite_OnResetSeedFinished;
                    baseSite.OnTipFinished -= BaseSite_OnTipFinished;
                    baseSite.OnWithdrawalFinished -= BaseSite_OnWithdrawalFinished;
                    baseSite.OnBrowserBypassRequired -= BaseSite_OnBrowserBypassRequired;
                    baseSite.Disconnect();                    
                }
                baseSite = value;
                if (baseSite != null)
                {
                    baseSite.Action += BaseSite_Action;
                    baseSite.ChatReceived += BaseSite_ChatReceived;
                    baseSite.BetFinished += BaseSite_DiceBetFinished;
                    baseSite.Error += BaseSite_Error;
                    baseSite.LoginFinished += BaseSite_LoginFinished;
                    baseSite.Notify += BaseSite_Notify;
                    baseSite.RegisterFinished += BaseSite_RegisterFinished;
                    baseSite.StatsUpdated += BaseSite_StatsUpdated;
                    baseSite.OnInvestFinished += BaseSite_OnInvestFinished;
                    baseSite.OnResetSeedFinished += BaseSite_OnResetSeedFinished;
                    baseSite.OnTipFinished += BaseSite_OnTipFinished;
                    baseSite.OnWithdrawalFinished += BaseSite_OnWithdrawalFinished;
                    baseSite.OnBrowserBypassRequired += BaseSite_OnBrowserBypassRequired;
                    
                    if (!new List<Games>(baseSite.SupportedGames).Contains(CurrentGame))
                    {
                        CurrentGame = baseSite.SupportedGames[0];
                    }

                }
            }
        }

        private void BaseSite_OnBrowserBypassRequired(object sender, BypassRequiredArgs e)
        {
            OnBypassRequired?.Invoke(sender, e);
        }

        private void BaseSite_OnWithdrawalFinished(object sender, GenericEventArgs e)
        {
            CalculateNextBet();
        }

        private void BaseSite_OnTipFinished(object sender, GenericEventArgs e)
        {
            CalculateNextBet();
        }

        private void BaseSite_OnResetSeedFinished(object sender, GenericEventArgs e)
        {
            CalculateNextBet();
        }

        private void BaseSite_OnInvestFinished(object sender, GenericEventArgs e)
        {
            CalculateNextBet();
        }

        private Games currentGame;

        public Games CurrentGame
        {
            get { return currentGame; }
            set { currentGame = value; OnGameChanged?.Invoke(this, new EventArgs()); }
        }


        public event BaseSite.dStatsUpdated OnSiteStatsUpdated;
        public event BaseSite.dAction OnSiteAction;
        public event BaseSite.dChat OnSiteChat;
        public event BaseSite.dBetFinished OnSiteBetFinished;
        public event BaseSite.dError OnSiteError;
        public event BaseSite.dLoginFinished OnSiteLoginFinished;
        public event BaseSite.dNotify OnSiteNotify;
        public event BaseSite.dRegisterFinished OnSiteRegisterFinished;
        public event EventHandler OnGameChanged;
        public event EventHandler OnStrategyChanged;
        public event EventHandler<NotificationEventArgs> OnNotification;
        public event EventHandler<GetConstringPWEventArgs> NeedConstringPassword;
        public event EventHandler<GetConstringPWEventArgs> NeedKeepassPassword;
        public event EventHandler OnStarted;
        public event EventHandler<GenericEventArgs> OnStopped;
        public event EventHandler<BypassRequiredArgs> OnBypassRequired;
        public event PropertyChangedEventHandler PropertyChanged;

        private void BaseSite_StatsUpdated(object sender, StatsUpdatedEventArgs e)
        {
            OnSiteStatsUpdated?.Invoke(sender, e);
        }

        private void BaseSite_RegisterFinished(object sender, GenericEventArgs e)
        {
            OnSiteRegisterFinished?.Invoke(sender, e);
        }

        private void BaseSite_Notify(object sender, GenericEventArgs e)
        {
            OnSiteNotify?.Invoke(sender, e);
        }

        private void BaseSite_LoginFinished(object sender, LoginFinishedEventArgs e)
        {
            LoggedIn = e.Success;
            OnSiteLoginFinished?.Invoke(sender, e);
        }
        List<ErrorType> BettingErrorTypes = new List<ErrorType>(new ErrorType[] { ErrorType.BalanceTooLow, ErrorType.BetMismatch, ErrorType.InvalidBet, ErrorType.NotImplemented, ErrorType.Other, ErrorType.Unknown });
        List<ErrorType> NonBettingErrorTypes = new List<ErrorType>(new ErrorType[] { ErrorType.Withdrawal, ErrorType.Tip, ErrorType.ResetSeed });
        
        private void BaseSite_Error(object sender, ErrorEventArgs ea)
        {
            BotErrorEventArgs be = new BotErrorEventArgs(ea);
            ActiveErrors.Add(be);
            if (Strategy != null)
            {
                Strategy.OnError(be);
                if (be.Handled)
                {
                    switch (be.Action)
                    {
                        /*ResumeAsWin, ResumeAsLoss, Resume, Stop, Reset, Retry*/
                        case ErrorActions.Stop: 
                            StopStrategy(be.Type.ToString() + " error occurred - Set to stop.");
                            break;
                        case ErrorActions.Reset:
                            NextBext = (Strategy.RunReset());
                            CalculateNextBet();
                            break;
                        case ErrorActions.Resume:
                            //what and how?
                            break;
                        case ErrorActions.Retry:
                            if (Running )
                            {
                                PlaceBet(NextBext);
                            }
                            break;
                        case ErrorActions.ResumeAsWin:
                            CalculateNextBet();//what and how?
                            break;
                        case ErrorActions.ResumeAsLoss:
                            CalculateNextBet();//what and how?
                            break;
                            

                    }
                }
            }
            if (!be.Handled)
            {
                ErrorSetting tmpSetting = PersonalSettings.GetErrorSetting(be.Type);
                if (tmpSetting!=null)
                {
                    if (tmpSetting.Action == ErrorActions.Stop)
                        StopStrategy(tmpSetting.Type.ToString() + " error occurred - Set to stop.");
                    
                    
                    if (BettingErrorTypes.Contains(tmpSetting.Type))
                    {
                        if (ErrorActions.Reset == tmpSetting.Action)
                        {
                            if (Running)
                            {
                                if (Retries <= PersonalSettings.RetryAttempts)
                                {
                                    NextBext = (Strategy.RunReset());
                                    Thread.Sleep(PersonalSettings.RetryDelay);
                                    Retries++;
                                    CalculateNextBet();
                                }
                            }
                        }
                        else if (tmpSetting.Action == ErrorActions.Resume)
                        {

                        }
                        else if (ErrorActions.Retry == tmpSetting.Action)
                        {
                            if (Retries <= PersonalSettings.RetryAttempts)
                            {
                                Thread.Sleep(PersonalSettings.RetryDelay);
                                Retries++;
                                if (Running )
                                {
                                    PlaceBet(NextBext);
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (tmpSetting.Action)
                        {
                            case ErrorActions.Reset:
                                if (Running)
                                {
                                    if (Retries <= PersonalSettings.RetryAttempts)
                                    {
                                        NextBext = (Strategy.RunReset());
                                        Thread.Sleep(PersonalSettings.RetryDelay);
                                        Retries++;
                                        if (Running)
                                        {
                                            PlaceBet(NextBext);
                                        }
                                    }
                                }
                                break;
                            case ErrorActions.Resume:break;
                            case ErrorActions.Retry:
                                if (Retries <= PersonalSettings.RetryAttempts)
                                {
                                    Thread.Sleep(PersonalSettings.RetryDelay);
                                    Retries++;
                                    CalculateNextBet();
                                } break;
                        }
                    }
                }
            }
            OnSiteError?.Invoke(sender, be);
            try
            {
                ActiveErrors.Remove(be);
            }
            catch
            {

            }
        }

        private void BaseSite_DiceBetFinished(object sender, BetFinisedEventArgs e)
        {
            if (e.NewBet == null)
                return;
            try
            {
                
                DBInterface?.Add(e.NewBet);
                DBInterface?.SaveChanges();
                MostRecentBet = e.NewBet;
                MostRecentBetTime = DateTime.Now;
                Retries = 0;
                /*
                 * save bet to DB - invoke async?
                 * send bet to GUI - invoke async?
                 * */
                bool win = e.NewBet.GetWin(CurrentSite);
                string Response = "";
                bool Reset = false;
                if (BetSettings?.CheckResetPreStats(e.NewBet, win, Stats, CurrentSite.Stats) ?? false)
                {
                    Reset = true;
                    NextBext = Strategy.RunReset();
                }
                if (BetSettings?.CheckStopPreStats(e.NewBet, win, Stats, out Response, CurrentSite.Stats) ?? false)
                {
                    StopStrategy(Response);
                }
                Stats.UpdateStats(e.NewBet, win);
                if (Strategy is ProgrammerMode)
                {
                    (Strategy as ProgrammerMode).UpdateSessionStats(CopyHelper.CreateCopy<SessionStats>(Stats));
                    (Strategy as ProgrammerMode).UpdateSiteStats(CopyHelper.CreateCopy<SiteStats>(CurrentSite.Stats));
                    (Strategy as ProgrammerMode).UpdateSite(CopyHelper.CreateCopy<SiteDetails>(CurrentSite.SiteDetails));
                }
                OnSiteBetFinished?.Invoke(sender, e);


                if (e.NewBet.Guid != LastBetGuid || LastBetsGuids.Contains(e.NewBet.Guid))
                {
                    StopStrategy("Last bet did not match the latest bet placed.");
                    //stop
                    return;
                }
                else
                {
                    LastBetsGuids.Enqueue(e.NewBet.Guid);
                    if (LastBetsGuids.Count > 10)
                        LastBetsGuids.Dequeue();
                }





                foreach (Trigger x in PersonalSettings.Notifications)
                {
                    if (x.Enabled)
                    {
                        if (x.CheckNotification(Stats, CurrentSite.Stats))
                        {
                            switch (x.Action)
                            {
                                case TriggerAction.Alarm:
                                case TriggerAction.Chime:
                                case TriggerAction.Popup: OnNotification?.Invoke(this, new NotificationEventArgs { NotificationTrigger = x }); break;
                                case TriggerAction.Email: throw new NotImplementedException("Supporting infrastructure for this still needs to be built.");
                            }
                        }
                    }
                }
                NextBext = null;

                foreach (Trigger x in BetSettings.Triggers)
                {
                    if (x.Enabled)
                    {
                        if (x.CheckNotification(Stats, CurrentSite.Stats))
                        {
                            switch (x.Action)
                            {

                                case TriggerAction.Bank: throw new NotImplementedException(); break;
                                case TriggerAction.Invest: CurrentSite.Invest(x.GetValue(Stats, CurrentSite.Stats)); break;
                                case TriggerAction.Reset: NextBext = Strategy.RunReset(); Reset = true; break;
                                case TriggerAction.ResetSeed: if (CurrentSite.CanChangeSeed) CurrentSite.ResetSeed(CurrentSite.GenerateNewClientSeed()); break;
                                case TriggerAction.Stop: StopStrategy($"Stop trigger fired: {x.ToString()}"); break;
                                //case TriggerAction.Switch: Strategy.High = !Strategy.High; if (NewBetObject != null)NewBetObject.High = !e.NewBet.High;  break;
                                case TriggerAction.Tip: CurrentSite.SendTip(x.Destination, x.GetValue(Stats, CurrentSite.Stats)); break;
                                case TriggerAction.Withdraw: CurrentSite.Withdraw(x.Destination, x.GetValue(Stats, CurrentSite.Stats)); break;

                            }
                        }
                    }
                }
                if (BetSettings.CheckResetPostStats(e.NewBet, win, Stats, CurrentSite.Stats))
                {
                    Reset = true;
                    NextBext = Strategy.RunReset();
                }
                if (BetSettings.CheckStopPOstStats(e.NewBet, win, Stats, out Response, CurrentSite.Stats))
                {
                    StopStrategy(Response);
                }
                decimal withdrawamount = 0;
                if (BetSettings.CheckWithdraw(e.NewBet, win, Stats, out withdrawamount, CurrentSite.Stats))
                {
                    throw new NotImplementedException();
                    //if (CurrentSite.AutoWithdraw)
                    //CurrentSite.Withdraw(BetSettings.)
                    // this.Balance -= withdrawamount;
                }
                if (BetSettings.CheckBank(e.NewBet, win, Stats, out withdrawamount, CurrentSite.Stats))
                {
                    throw new NotImplementedException();
                    //this.Balance -= withdrawamount;
                }
                if (BetSettings.CheckTips(e.NewBet, win, Stats, out withdrawamount, CurrentSite.Stats))
                {
                    throw new NotImplementedException();
                    //this.Balance -= withdrawamount;
                }
                bool NewHigh = false;
                if (BetSettings.CheckResetSeed(e.NewBet, win, Stats, CurrentSite.Stats))
                {
                    if (CurrentSite.CanChangeSeed)
                        CurrentSite.ResetSeed("");
                }
                if (Running)
                    CalculateNextBet();
            }
            catch (Exception E)
            {
                _Logger?.LogError(E.ToString());
            }
        }

        void CalculateNextBet()
        {
            if (CurrentSite.ActiveActions.Count > 0 || ActiveErrors.Count > 0)
                return;
            if (Strategy is ProgrammerMode)
            {
                (Strategy as ProgrammerMode).UpdateSessionStats(CopyHelper.CreateCopy<SessionStats>(Stats));
                (Strategy as ProgrammerMode).UpdateSiteStats(CopyHelper.CreateCopy<SiteStats>(CurrentSite.Stats));
                (Strategy as ProgrammerMode).UpdateSite(CopyHelper.CreateCopy<SiteDetails>(CurrentSite.SiteDetails));
            }
            bool win = MostRecentBet.GetWin(CurrentSite);
            if (StopOnWin && win)
            {
                StopStrategy("Stop On Win enabled - Bet won");
                return;
            }
            if (NextBext ==null)
                NextBext = Strategy.CalculateNextBet(MostRecentBet, win);
            if (Running)
            {
                while (CurrentSite.TimeToBet(NextBext) > 0 
                    && (decimal)(DateTime.Now - MostRecentBetTime).TotalMilliseconds>= NextBext.BetDelay
                    && (!BetSettings.EnableBotSpeed || (decimal)(DateTime.Now - MostRecentBetTime).TotalMilliseconds >= (1m/BetSettings.BotSpeed))
                    )
                {
                    int TimeToBet = CurrentSite.TimeToBet(NextBext);
                    if (TimeToBet < 0)
                        TimeToBet = (10);
                    Thread.Sleep(TimeToBet);
                }
                if (Running)
                    PlaceBet(NextBext);
            }
        }

        private void BaseSite_ChatReceived(object sender, GenericEventArgs e)
        {
            OnSiteChat?.Invoke(sender, e);
        }

        private void BaseSite_Action(object sender, GenericEventArgs e)
        {
            OnSiteAction?.Invoke(sender, e);
        }

        public void Login(LoginParamValue[] LoginParams)
        {
            if (CurrentSite==null)
            {
                throw new Exception("Cannot login without a site. Assign a value to CurrentSite, then log in.");
            }
            CurrentSite.LogIn(LoginParams);
        }

        

        //Site Stuff
        #endregion

        private Strategies.BaseStrategy strategy;

        public Strategies.BaseStrategy Strategy
        {
            get { return strategy; }
            set
            {
                if (strategy != null)
                {
                    strategy.NeedBalance -= Strategy_NeedBalance;
                    strategy.Stop -= Strategy_Stop;
                    strategy.OnNeedStats -= Strategy_OnNeedStats;
                    if (strategy is ProgrammerMode)
                    {
                        (Strategy as ProgrammerMode).OnAlarm -= Doormat_OnAlarm;
                        (Strategy as ProgrammerMode).OnChing -= Doormat_OnChing;
                        (Strategy as ProgrammerMode).OnExportSim -= Doormat_OnExportSim;
                        (Strategy as ProgrammerMode).OnInvest -= Doormat_OnInvest;
                        (Strategy as ProgrammerMode).OnPrint -= Doormat_OnPrint;
                        (Strategy as ProgrammerMode).OnRead -= Doormat_OnRead;
                        (Strategy as ProgrammerMode).OnReadAdv -= Doormat_OnReadAdv;
                        //(Strategy as ProgrammerMode).OnResetBuiltIn -= Doormat_OnResetBuiltIn;
                        (Strategy as ProgrammerMode).OnResetSeed -= Doormat_OnResetSeed;
                        (Strategy as ProgrammerMode).OnResetStats -= Doormat_OnResetStats;
                        (Strategy as ProgrammerMode).OnRunSim -= Doormat_OnRunSim;
                        //(Strategy as ProgrammerMode).OnStop -= Doormat_OnStop;
                        (Strategy as ProgrammerMode).OnTip -= Doormat_OnTip;
                        (Strategy as ProgrammerMode).OnWithdraw -= Doormat_OnWithdraw;
                        (Strategy as ProgrammerMode).OnScriptError -= Doormat_OnScriptError;
                        (Strategy as ProgrammerMode).OnSetCurrency -= Doormat_OnSetCurrency;

                    }
                }
                strategy = value;
                if (strategy != null)
                {
                    strategy.NeedBalance += Strategy_NeedBalance;
                    strategy.Stop += Strategy_Stop;
                    strategy.OnNeedStats += Strategy_OnNeedStats;
                    if (strategy is ProgrammerMode)
                    {
                        (strategy as ProgrammerMode).CreateRuntime();
                        (Strategy as ProgrammerMode).OnAlarm += Doormat_OnAlarm;
                        (Strategy as ProgrammerMode).OnChing += Doormat_OnChing;
                        (Strategy as ProgrammerMode).OnExportSim += Doormat_OnExportSim;
                        (Strategy as ProgrammerMode).OnInvest += Doormat_OnInvest;
                        (Strategy as ProgrammerMode).OnPrint += Doormat_OnPrint;
                        (Strategy as ProgrammerMode).OnRead += Doormat_OnRead;
                        (Strategy as ProgrammerMode).OnReadAdv += Doormat_OnReadAdv;
                        //(Strategy as ProgrammerMode).OnResetBuiltIn += Doormat_OnResetBuiltIn;
                        (Strategy as ProgrammerMode).OnResetSeed += Doormat_OnResetSeed;
                        (Strategy as ProgrammerMode).OnResetStats += Doormat_OnResetStats;
                        (Strategy as ProgrammerMode).OnRunSim += Doormat_OnRunSim;
                        //(Strategy as ProgrammerMode).OnStop += Doormat_OnStop;
                        (Strategy as ProgrammerMode).OnTip += Doormat_OnTip;
                        (Strategy as ProgrammerMode).OnWithdraw += Doormat_OnWithdraw;
                        (Strategy as ProgrammerMode).OnScriptError += Doormat_OnScriptError;
                        (Strategy as ProgrammerMode).OnSetCurrency += Doormat_OnSetCurrency;
                    }
                }
                StoredBetSettings.SetStrategy(value);
                OnStrategyChanged?.Invoke(this, new EventArgs());
                
            }
        }

        private void Doormat_OnSetCurrency(object sender, PrintEventArgs e)
        {
            if (CurrentSite != null)
            {
                if (Array.IndexOf(this.CurrentSite.Currencies, e.Message) > 0)
                {
                    this.CurrentSite.Currency = Array.IndexOf(this.CurrentSite.Currencies, e.Message);
                }
            }
        }

        private void Doormat_OnScriptError(object sender, PrintEventArgs e)
        {
            StopStrategy("Error received from programmer mode, check console for more details.");
        }

        private void Doormat_OnWithdraw(object sender, WithdrawEventArgs e)
        {
            if (CurrentSite.AutoWithdraw)
                CurrentSite.Withdraw(e.Address, e.Amount);
        }

        private void Doormat_OnTip(object sender, TipEventArgs e)
        {
            if (CurrentSite.CanTip)
                CurrentSite.SendTip(e.Receiver, e.Amount);
        }

        private void Doormat_OnStop(object sender, EventArgs e)
        {
            StopStrategy("Programmer mode stop signal received.");
        }

        private void Doormat_OnRunSim(object sender, RunSimEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Doormat_OnResetStats(object sender, EventArgs e)
        {
            ResetStats();
        }

        private void Doormat_OnResetSeed(object sender, EventArgs e)
        {
            if (CurrentSite.CanChangeSeed)
                CurrentSite.ResetSeed(CurrentSite.R.Next(0, int.MaxValue).ToString());
        }

        private void Doormat_OnResetBuiltIn(object sender, EventArgs e)
        {
            Strategy.RunReset();
        }

        private void Doormat_OnReadAdv(object sender, ReadEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Doormat_OnRead(object sender, ReadEventArgs e)

        {
            throw new NotImplementedException();
        }

        private void Doormat_OnPrint(object sender, PrintEventArgs e)
        {
            //send print to UI
            
        }

        private void Doormat_OnInvest(object sender, InvestEventArgs e)
        {
            if (CurrentSite.AutoInvest)
                CurrentSite.Invest(e.Amount);
        }

        private void Doormat_OnExportSim(object sender, ExportSimEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Doormat_OnChing(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Doormat_OnAlarm(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private SessionStats Strategy_OnNeedStats(object sender, EventArgs e)
        {
            return CopyHelper.CreateCopy<SessionStats>(Stats);
        }

        private void Strategy_Stop(object sender, Strategies.StopEventArgs e)
        {
            StopStrategy(e.Reason);
        }

        private decimal Strategy_NeedBalance()
        {
            if (CurrentSite == null)
                return 0;
            else if (CurrentSite.Stats == null)
                return 0;
            else
                return CurrentSite.Stats.Balance;
        }
       
        public void Start()
        {
            StopOnWin = false;
            if (Running)
                throw new Exception("Cannot start bot while it's running");
            if (RunningSimulation)
                throw new Exception("Cannot start bot while it's running a simulation");

            CurrentSite.ActiveActions.Clear();
            ActiveErrors.Clear();
            
            if (!Running && !RunningSimulation)
            {
                if (Strategy is ProgrammerMode)
                {
                    (Strategy as ProgrammerMode).LoadScript();
                    (Strategy as ProgrammerMode).UpdateSessionStats(CopyHelper.CreateCopy<SessionStats>(Stats));
                    (Strategy as ProgrammerMode).UpdateSiteStats(CopyHelper.CreateCopy<SiteStats>(CurrentSite.Stats));
                    (Strategy as ProgrammerMode).UpdateSite(CopyHelper.CreateCopy<SiteDetails>(CurrentSite.SiteDetails));
                }
                Running = true;
                if (Stats == null)
                    Stats = new SessionStats();
                Stats.StartTime = DateTime.Now;
                //Indicate to the selected strategy to create a working set and start betting.
                OnStarted?.Invoke(this, new EventArgs());
                MostRecentBetTime = DateTime.Now;
                BetTimer.Enabled = true;
                PlaceBet(Strategy.Start());
            }
            /*
             * if not running
             * and not running simulator
             * get initial bet values (chance, high, amount)
             * mark bot as running
             * reset run variables
             * continue session variables
             * run dicebet thread to place initial bet
             */
        }

        public void Resume()
        {
            StopOnWin = false;
            if (Running)
                throw new Exception("Cannot start bot while it's running");
            if (RunningSimulation)
                throw new Exception("Cannot start bot while it's running a simulation");

            CurrentSite.ActiveActions.Clear();
            ActiveErrors.Clear();

            if (!Running && !RunningSimulation)
            {
                if (Strategy is ProgrammerMode)
                {
                    (Strategy as ProgrammerMode).LoadScript();
                    (Strategy as ProgrammerMode).UpdateSessionStats(CopyHelper.CreateCopy<SessionStats>(Stats));
                    (Strategy as ProgrammerMode).UpdateSiteStats(CopyHelper.CreateCopy<SiteStats>(CurrentSite.Stats));
                    (Strategy as ProgrammerMode).UpdateSite(CopyHelper.CreateCopy<SiteDetails>(CurrentSite.SiteDetails));
                }
                Running = true;
                Stats.StartTime = DateTime.Now;
                //Indicate to the selected strategy to create a working set and start betting.
                MostRecentBetTime = DateTime.Now;
                BetTimer.Enabled = true;
                OnStarted?.Invoke(this, new EventArgs());
                CalculateNextBet();
            }
        }
        
        private void BetTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((DateTime.Now-MostRecentBetTime).TotalSeconds> PersonalSettings.RetryDelay && (Retries< PersonalSettings.RetryAttempts || PersonalSettings.RetryAttempts<0))
            {
                if (NextBext != null && ((DateTime.Now - MostRecentBetTime).Milliseconds > NextBext.BetDelay))
                {
                    Retries++;
                    MostRecentBetTime = DateTime.Now;                    
                    PlaceBet(NextBext);
                }
            }
        }

        public void StopStrategy(string Reason)
        {
            
            bool wasrunning = Running;
            Running = false;
            if (Stats != null)
            {
                Stats.EndTime = DateTime.Now;
                Stats.RunningTime += (long)(Stats.EndTime - Stats.StartTime).TotalMilliseconds;
                if (wasrunning && DBInterface != null)
                {
                    if (Stats.SessionStatsId<=0)
                        DBInterface.Add(Stats);
                    DBInterface?.SaveChanges();
                }
            }
            
            //TotalRuntime +=Stats.EndTime - Stats.StartTime;
            _Logger?.LogInformation(Reason);
            OnStopped?.Invoke(this, new GenericEventArgs { Message = Reason });
        }

        public void ResetStats()
        {
            if (Running)
            {
                Stats.EndTime = DateTime.Now;
                Stats.RunningTime += (long)(Stats.EndTime - Stats.EndTime).TotalMilliseconds;
            }
            TotalRuntime += Stats.RunningTime;
            if (this.DBInterface != null)
            {
               this.DBInterface.Add(Stats);
                this.DBInterface.SaveChanges();
            }

            Stats = new SessionStats();
        }

        public void PlaceBet(PlaceBet Bet)
        {
            if (Bet != null)
            {
                Bet.GUID = Guid.NewGuid().ToString();
                LastBetGuid = Bet.GUID;
                CurrentSite.PlaceBet(Bet);
            }
        }

        void DiceBetThread(object Bet)
        {
            PlaceDiceBet tmpBet = Bet as PlaceDiceBet;
            CurrentSite.PlaceBet(tmpBet);
        }

        public class ExportBetSettings
        {
            public string Strategy { get; set; }
            public InternalBetSettings BetSettings { get; set; }
            public Strategies.DAlembert dAlembert { get; set; }
            public Strategies.Fibonacci Fibonacci { get; set; }
            public Strategies.Labouchere Labouchere { get; set; }
            public Strategies.PresetList PresetList { get; set; }
            public Strategies.Martingale Martingale { get; set; }
            public Strategies.ProgrammerCS ProgrammerCS { get; set; }
            public Strategies.ProgrammerJS ProgrammerJS { get; set; }
            public Strategies.ProgrammerLUA ProgrammerLUA { get; set; }
            public Strategies.ProgrammerPython ProgrammerPython { get; set; }
            //public Strategies.Dice.Programmer Programmer { get; set; }
            public Strategies.BaseStrategy GetStrat()
            {
                switch (Strategy)
                {
                    case "DAlembert": return dAlembert; 
                    case "PresetList": return PresetList; 
                    case "Labouchere": return Labouchere; 
                    case "Fibonacci": return Fibonacci; 
                    case "Martingale": return Martingale; 
                    case "ProgrammerCS": return ProgrammerCS; 
                    case "ProgrammerJS": return ProgrammerJS; 
                    case "ProgrammerLUA": return ProgrammerLUA; 
                    case "ProgrammerPython": return ProgrammerPython; 
                    default: return new Strategies.Martingale(null); 
                }               
            }
            public void SetStrategy(Strategies.BaseStrategy Strat)
            {
                /*dAlembert = null;
                Fibonacci = null;
                Labouchere = null;
                PresetList = null;
                Martingale = null;
                ProgrammerCS = null;
                ProgrammerJS = null;
                ProgrammerLUA = null;*/
                //ProgrammerPython = null;
                if (Strat is Strategies.DAlembert)
                {
                    dAlembert = Strat as Strategies.DAlembert;
                    Strategy = "DAlembert";
                }
                if (Strat is Strategies.Fibonacci)
                {                    
                    Fibonacci = Strat as Strategies.Fibonacci;
                    Strategy = "Fibonacci";
                }
                if (Strat is Strategies.Labouchere)
                {                    
                    Labouchere = Strat as Strategies.Labouchere;
                    Strategy = "Labouchere";
                }
                if (Strat is Strategies.PresetList)
                {                   
                    PresetList = Strat as Strategies.PresetList;
                    Strategy = "PresetList";
                }
                if (Strat is Strategies.Martingale)                
                {                    
                    Martingale = Strat as Strategies.Martingale;
                    Strategy = "Martingale";

                }
                if (Strat is Strategies.ProgrammerCS)
                {
                    ProgrammerCS = Strat as Strategies.ProgrammerCS;
                    Strategy = "ProgrammerCS";
                }
                if (Strat is Strategies.ProgrammerJS)
                {
                    ProgrammerJS = Strat as Strategies.ProgrammerJS;
                    Strategy = "ProgrammerJS";

                }
                if (Strat is Strategies.ProgrammerLUA)
                {
                    ProgrammerLUA = Strat as Strategies.ProgrammerLUA;

                    Strategy = "ProgrammerLUA";
                }
               if (Strat is Strategies.ProgrammerPython)
                {
                    ProgrammerPython = Strat as Strategies.ProgrammerPython;
                        Strategy = "ProgrammerPython";
                }
            }
        }

        public void SavePersonalSettings(string FileLocation)
        {
            string Settings = JsonSerializer.Serialize(PersonalSettings);
            using (StreamWriter sw = new StreamWriter(FileLocation, false))
            {
                sw.Write(Settings);
            }
            //incl proxy settings without password - prompt password on startup.

        }
        
        public void SaveBetSettings(string FileLocation)
        {
            /*ExportBetSettings tmp = new ExportBetSettings()
            {
                BetSettings = this.BetSettings
            };
            tmp.SetStrategy(Strategy);*/
            StoredBetSettings.SetStrategy(strategy);
            string Settings = JsonSerializer.Serialize(this.StoredBetSettings);
            if (!Directory.Exists(Path.GetDirectoryName(FileLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(FileLocation));
            using (StreamWriter sw = new StreamWriter(FileLocation, false)) 
            {
                sw.Write(Settings);
            }
            
        }

        public void LoadPersonalSettings(string FileLocation)
        {
            string Settings = "";
            var files = System.IO.Directory.GetFiles(Path.GetDirectoryName(FileLocation));
            
            using (StreamReader sr = new StreamReader(FileLocation))
            {
                Settings = sr.ReadToEnd();
            }
            _Logger?.LogInformation("Loaded Personal Settings File");
            PersonalSettings tmp = JsonSerializer.Deserialize<PersonalSettings>(Settings);
            _Logger?.LogInformation("Parsed Personal Settings File");
            this.PersonalSettings = tmp;
            string pw = "";

            if (tmp.EncryptConstring)
            {
                GetConstringPWEventArgs tmpArgs = new GetConstringPWEventArgs();
                NeedConstringPassword?.Invoke(this,tmpArgs);
                pw = tmpArgs.Password;
            }
            if (!string.IsNullOrWhiteSpace(tmp.KeepassDatabase))
            {
                try
                {
                    GetConstringPWEventArgs tmpArgs = new GetConstringPWEventArgs();
                    NeedKeepassPassword?.Invoke(this, tmpArgs);
                   /* var ioConnInfo = new IOConnectionInfo { Path = tmp.KeepassDatabase };
                    var compKey = new CompositeKey();
                    compKey.AddUserKey(new KcpPassword(tmpArgs.Password));
                    Passdb.Open(ioConnInfo, compKey, null);*/
                }
                catch (Exception exc)
                {
                    OnSiteNotify?.Invoke(this, new GenericEventArgs { Message="Failed to open KeePass Database." });
                }
            }
            try
            {
                _Logger?.LogInformation("Attempting DB Interface Creation: {DBProvider}", PersonalSettings.Provider);
                DBInterface = new BotContext(_Logger);//create a bot context here. 
                
                DBInterface.Settings = PersonalSettings;
                if (!DBInterface.Database.EnsureCreated())
                {
                    DBInterface.Database.Migrate();
                }
                _Logger?.LogInformation("DB Interface Created: {DBProvider}", PersonalSettings.Provider);
            }
            catch (Exception e)
            {
                _Logger?.LogError(e.ToString()); 
                DBInterface = null;
            }
        }

        public ExportBetSettings LoadBetSettings(string FileLocation, bool ApplySettings = true)
        {

            List<Trigger> trig = JsonSerializer.Deserialize<List<Trigger>>(@"[{""Action"":4,""Enabled"":true,""TriggerProperty"":""Wins"",""TargetType"":1,""Target"":""Wins"",""Comparison"":3,""Percentage"":50,""ValueType"":0,""ValueProperty"":null,""ValueValue"":0,""Destination"":null}]");
            string Settings = "";
            using (StreamReader sr = new StreamReader(FileLocation))
            {
                Settings = sr.ReadToEnd();
            }
            this.StoredBetSettings = JsonSerializer.Deserialize<ExportBetSettings>(Settings);
            if (StoredBetSettings.BetSettings!=null && ApplySettings)
                this.BetSettings = StoredBetSettings.BetSettings;
            else
                this.BetSettings = new InternalBetSettings();
            this.Strategy = StoredBetSettings.GetStrat();
            this.Strategy.SetLogger(_Logger);
            return StoredBetSettings;
        }
        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {

            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        }
        #region Accounts
        public KPHelper[] GetAccounts()
        {
            //var Entries = Passdb.RootGroup.GetEntries(true);
            List<KPHelper> tmpHelpers = new List<KPHelper>();
            /*int i = 0;
            foreach (var x in Entries)
            {
                tmpHelpers.Add(new KPHelper {
                    Index = i++,
                    Title = x.Strings.ReadSafe("Title"),
                    Username = x.Strings.ReadSafe("Username"),
                    URL = x.Strings.ReadSafe("URL"),
                    Id = x.Uuid.UuidBytes
                });
            }*/
            //Passdb.RootGroup.FindEntry(new PwUuid(tmpHelpers[0].Id),true);
            return tmpHelpers.ToArray();
        }

        public string GetPw(KPHelper Helper, out string Note)
        {
            /*PwEntry pwEntry = Passdb.RootGroup.FindEntry(new PwUuid(Helper.Id), true);
            Note = pwEntry.Strings.ReadSafe("Note");
            return pwEntry.Strings.ReadSafe("Password");*/
            throw new NotImplementedException();
        }
        #endregion

    }
}
