using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json; // <-- Add this
using Newtonsoft.Json.Converters; // <-- Add this

[JsonConverter(typeof(StringEnumConverter))] // <-- Add this
public enum AgentType { Regular, BargainHunter, ImpulseBuyer, WindowShopper, BulkShopper, FocusedShopper }

[JsonConverter(typeof(StringEnumConverter))] // <-- Add this
public enum PrimaryTrait { Impulsive, Cautious, BudgetConscious, TimeSensitive, BrandLoyalist, Indecisive }

[Serializable]
public class Demographics
{
    public int age;
    public string profession;
    public string income_level;
    public string gender;
}

[Serializable]
public class BaselinePhysics
{
    public float base_speed;
    public float base_acceleration;
    public int base_avoidance_priority;
}

[Serializable]
public class PsychologicalProfile
{
    public float base_random_browse_probability;
    public float base_impulse_probability;
}

[Serializable]
public class ShoppingListItem
{
    public string item_name;
    public int quantity;
}

[Serializable]
public class AgentPersonaData
{
    public string persona_name;
    public Demographics demographics;
    public AgentType agent_type;
    public PrimaryTrait primary_trait;
    public BaselinePhysics baseline_physics;
    public int base_total_money;
    public PsychologicalProfile psychological_profile;
    public List<ShoppingListItem> base_shopping_list;
    public List<ShoppingListItem> base_impulse_favorites;
}

[CreateAssetMenu(fileName = "PersonaDatabase", menuName = "AI/Persona Database")]
public class PersonaDatabase : ScriptableObject
{
    public List<AgentPersonaData> personas = new List<AgentPersonaData>();
}

[Serializable]
public class PersonaJsonWrapper
{
    public List<AgentPersonaData> personas;
}