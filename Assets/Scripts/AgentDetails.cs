using System;
using System.Collections.Generic;

[Serializable]
public class AgentDetails
{
    public string AgentName;
    public string Position;
    public int AgentAge;
    public string AgentGender;
    public string AgentIncomeLevel;
    public string AgentProfession;
    public string CurrentState;
    public string CurrentMood;
    public string ShoppingList;
    public string ImpulseFavorites;
    public string CostlyItems;
    public string CartItems;
    public int TotalMoneySpent;
    public int RemainingMoney;
    public int BaselineMoney;
    public string History;
    public List<AgentHistoryEntryData> HistoryEntries;
}

[Serializable]
public class AgentHistoryEntryData
{
    public float Timestamp;
    public string State;
    public string ActionDescription;
    public string Mood;

}