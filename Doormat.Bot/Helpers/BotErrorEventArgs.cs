﻿using Gambler.Bot.Core.Events;
using Gambler.Bot.Helpers;

namespace Gambler.Bot.AutoBet.Helpers
{
    public class BotErrorEventArgs:ErrorEventArgs
    {
        public ErrorActions Action { get; set; }
        public BotErrorEventArgs(ErrorEventArgs args):base()
        {
            Action = ErrorActions.Retry;
            Message = args.Message;
            Type = args.Type;
            this.Handled = args.Handled;
            this.Fatal = args.Fatal;            
        }
    }
}
