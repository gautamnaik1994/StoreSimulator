using System;
using System.Collections.Generic;
using UnityEngine;

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

    public Color MoodColor; // New field for mood color
    public Color StateColor; // New field for state color
    public List<AgentHistoryEntryData> HistoryEntries;

    public AgentType AgentType; // New field for agent type
    public PrimaryTrait PrimaryTrait; // New field for primary trait
}

[Serializable]
public class AgentHistoryEntryData
{
    public float Timestamp;
    public string State;
    public string ActionDescription;
    public string Mood;
    public Color MoodColor;
    public Color StateColor;

}
[Serializable]
public class AgentModifiers
{
    public float BudgetModifier;
    public float PurchaseLikelihoodModifier;
    public float BrowseTimeModifier;
    public float SpeedModifier;
}
