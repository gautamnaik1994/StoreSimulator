#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json; // <-- Add this

public class PersonaImporter : EditorWindow
{
    [MenuItem("Tools/Import Personas to Single Database")]
    public static void ImportPersonasToDatabase()
    {
        string filePath = EditorUtility.OpenFilePanel("Select Personas JSON", "", "json");
        if (string.IsNullOrEmpty(filePath)) return;

        string jsonContent = File.ReadAllText(filePath);

        // CHANGE THIS LINE: Use Newtonsoft instead of JsonUtility
        PersonaJsonWrapper wrapper = JsonConvert.DeserializeObject<PersonaJsonWrapper>(jsonContent);

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

        PersonaDatabase databaseAsset = AssetDatabase.LoadAssetAtPath<PersonaDatabase>(assetPath);

        if (databaseAsset == null)
        {
            databaseAsset = ScriptableObject.CreateInstance<PersonaDatabase>();
            databaseAsset.personas = wrapper.personas;
            AssetDatabase.CreateAsset(databaseAsset, assetPath);
        }
        else
        {
            databaseAsset.personas = wrapper.personas;
            EditorUtility.SetDirty(databaseAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully built a single database asset with {databaseAsset.personas.Count} personas at {assetPath}!");
    }
}
#endif