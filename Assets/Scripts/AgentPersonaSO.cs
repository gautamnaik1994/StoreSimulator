using UnityEngine;

using System;
using System.Collections.Generic;

public enum AgentType { Regular, BargainHunter, ImpulseBuyer, WindowShopper, BulkShopper, FocusedShopper }
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

// 1. This is a plain class now—no longer its own separate asset file
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
    public List<string> base_shopping_list;
    public List<string> base_impulse_favorites;
}

// 2. This is the SINGLE asset file that will hold all 100+ profiles
[CreateAssetMenu(fileName = "PersonaDatabase", menuName = "AI/Persona Database")]
public class PersonaDatabase : ScriptableObject
{
    public List<AgentPersonaData> personas = new List<AgentPersonaData>();
}

// ADD THIS: A plain C# object used EXCLUSIVELY for the initial JSON parsing step
[Serializable]
public class PersonaJsonWrapper
{
    public List<AgentPersonaData> personas;
}