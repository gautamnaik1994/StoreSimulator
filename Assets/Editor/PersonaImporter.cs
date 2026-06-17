#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public class PersonaImporter : EditorWindow
{
    [MenuItem("Tools/Import Personas to Single Database")]
    public static void ImportPersonasToDatabase()
    {
        string filePath = EditorUtility.OpenFilePanel("Select Personas JSON", "", "json");
        if (string.IsNullOrEmpty(filePath)) return;

        string jsonContent = File.ReadAllText(filePath);

        // 1. Parse into the plain C# wrapper instead of the ScriptableObject directly
        PersonaJsonWrapper wrapper = JsonUtility.FromJson<PersonaJsonWrapper>(jsonContent);

        if (wrapper == null || wrapper.personas == null || wrapper.personas.Count == 0)
        {
            Debug.LogError("Failed to parse JSON. Double check that your root JSON key is exactly 'personas'.");
            return;
        }

        string assetFolderPath = "Assets/Resources";
        string assetPath = $"{assetFolderPath}/PersonaDatabase.asset";

        if (!Directory.Exists(assetFolderPath))
        {
            Directory.CreateDirectory(assetFolderPath);
        }

        // 2. Safely generate the ScriptableObject instance via Unity's API
        PersonaDatabase databaseAsset = ScriptableObject.CreateInstance<PersonaDatabase>();

        // 3. Assign the parsed plain C# list data over to the asset
        databaseAsset.personas = wrapper.personas;

        // 4. Save asset to disk
        AssetDatabase.CreateAsset(databaseAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully built a single database asset with {databaseAsset.personas.Count} personas at {assetPath}!");
    }
}
#endif