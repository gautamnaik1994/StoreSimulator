using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class WorldManager : MonoBehaviour
{
    // public static WorldManager Instance;

    public enum MacroCycle { Standard, PaydayWeek, EndOfMonth, FestiveSeason }
    public enum StoreEvent { None, FlashSaleActive, PanicBuyingAlert }
    public enum WeatherCondition { Clear, Rainy, Hot, Cold }

    [Header("Current Global Settings")]
    [SerializeField] private MacroCycle currentCycle = MacroCycle.Standard;
    [SerializeField] private StoreEvent activeEvent = StoreEvent.None;
    [SerializeField] private WeatherCondition currentWeather = WeatherCondition.Clear;



    public UIDocument uiDocument;

    public MacroCycle CurrentCycle
    {
        get => currentCycle;
        set { currentCycle = value; }
    }

    public StoreEvent ActiveEvent
    {
        get => activeEvent;
        set { activeEvent = value; }
    }

    public WeatherCondition CurrentWeather
    {
        get => currentWeather;
        set { currentWeather = value; }
    }


    private EnumField macroCycleField, storeEventField, weatherConditionField;

    void Start()
    {
        // Initialize the UI with current world settings
        macroCycleField = uiDocument.rootVisualElement.Q<EnumField>("macroCycle");
        storeEventField = uiDocument.rootVisualElement.Q<EnumField>("storeEvent");
        weatherConditionField = uiDocument.rootVisualElement.Q<EnumField>("weatherCondition");


        macroCycleField?.Init(currentCycle);
        storeEventField?.Init(activeEvent);
        weatherConditionField?.Init(currentWeather);

        macroCycleField.RegisterValueChangedCallback(evt =>
        {
            currentCycle = (MacroCycle)evt.newValue;
            Debug.Log($"Macro Cycle changed to: {currentCycle}");

        });
        storeEventField.RegisterValueChangedCallback(evt =>
        {
            activeEvent = (StoreEvent)evt.newValue;
            Debug.Log($"Store Event changed to: {activeEvent}");
        });
        weatherConditionField.RegisterValueChangedCallback(evt =>
        {
            currentWeather = (WeatherCondition)evt.newValue;
            Debug.Log($"Weather Condition changed to: {currentWeather}");
        });
    }

    #region Retail Modifier Calculations

    public float GetGlobalSpeedModifier()
    {
        float modifier = 1.0f;

        // Weather impacts walking speeds (e.g., people escaping rain stroll less, hot weather slows people down)
        if (currentWeather == WeatherCondition.Rainy) modifier *= 1.15f;
        if (currentWeather == WeatherCondition.Hot) modifier *= 0.85f;

        if (activeEvent == StoreEvent.PanicBuyingAlert) modifier *= 1.3f;
        if (activeEvent == StoreEvent.None && currentCycle == MacroCycle.FestiveSeason) modifier *= 0.75f; // Leisurely holiday stroll

        return modifier;
    }

    public float GetGlobalBrowseTimeModifier()
    {
        if (currentCycle == MacroCycle.EndOfMonth) return 1.6f;      // Comparing prices carefully (Budget Conscious)
        if (currentCycle == MacroCycle.FestiveSeason) return 1.25f;  // Higher dwell times looking for gifts
        if (activeEvent == StoreEvent.PanicBuyingAlert) return 0.4f; // Direct to shelf, grab, and go
        return 1.0f;
    }

    public float GetGlobalPurchaseLikelihoodModifier()
    {
        float modifier = 1.0f;
        if (currentCycle == MacroCycle.PaydayWeek) modifier *= 1.35f;      // High disposable income injection
        if (currentCycle == MacroCycle.EndOfMonth) modifier *= 0.7f;       // Tight wallets
        if (activeEvent == StoreEvent.FlashSaleActive) modifier *= 1.6f;   // FOMO mechanics triggered
        return modifier;
    }

    /// <summary>
    /// Simulates macroeconomic wallet scaling based on pay cycles.
    /// </summary>
    public float GetBudgetModifier()
    {
        if (currentCycle == MacroCycle.PaydayWeek) return 1.25f;
        if (currentCycle == MacroCycle.EndOfMonth) return 0.80f;
        return 1.0f;
    }

    #endregion

}