﻿decimal baseb = 0.00000001m;
void CalculateBet()
{
    if (Win)
    {
        NextBet.Amount = baseb;
        NextBet.High = !NextBet.High;
    }
    else
    {
        NextBet.Amount = PreviousBet.TotalAmount * 2m;
    }
    if (Stats.Profit > SiteDetails.Wagered * 0.0001m)
    {
        Withdraw("your address here", Stats.Balance * 0.01m);
    }

}

void Reset()
{
    NextBet.Amount = baseb;
    NextBet.Chance = 49.5m;
    NextBet.High = True;
}