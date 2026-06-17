using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    public enum MacroCycle { Standard, PaydayWeek, EndOfMonth, FestiveSeason }
    public enum StoreEvent { None, FlashSaleActive, PanicBuyingAlert }

    public enum WeatherCondition { Clear, Rainy, Hot, Cold }

    [Header("Current Global Settings")]
    public MacroCycle currentCycle = MacroCycle.Standard;
    public StoreEvent activeEvent = StoreEvent.None;
    public WeatherCondition currentWeather = WeatherCondition.Clear;

    void Awake() => Instance = this;

    // A unified lookup for agents to query their physics modifiers
    public float GetGlobalSpeedModifier()
    {
        float modifier = 1.0f;
        if (activeEvent == StoreEvent.PanicBuyingAlert) modifier *= 1.3f;
        if (activeEvent == StoreEvent.None && currentCycle == MacroCycle.FestiveSeason) modifier *= 0.85f; // Casual crowd pacing
        return modifier;
    }

    public float GetGlobalBrowseTimeModifier()
    {
        if (currentCycle == MacroCycle.EndOfMonth) return 1.5f; // Comparing prices carefully
        if (activeEvent == StoreEvent.PanicBuyingAlert) return 0.5f; // Grab and go!
        return 1.0f;
    }

    public float GetGlobalPurchaseLikelihoodModifier()
    {
        if (currentCycle == MacroCycle.PaydayWeek) return 1.2f; // More disposable income
        if (activeEvent == StoreEvent.FlashSaleActive) return 1.5f; // Can't miss out on deals!
        return 1.0f;
    }

    // 
}