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

        PersonaDatabase databaseAsset = AssetDatabase.LoadAssetAtPath<PersonaDatabase>(assetPath);

        // FIX: If the asset doesn't exist, create it properly. 
        // If it DOES exist, don't overwrite the file (which breaks the script reference); 
        // instead, just replace the inner list data!
        if (databaseAsset == null)
        {
            databaseAsset = ScriptableObject.CreateInstance<PersonaDatabase>();
            databaseAsset.personas = wrapper.personas;
            AssetDatabase.CreateAsset(databaseAsset, assetPath);
        }
        else
        {
            databaseAsset.personas = wrapper.personas;
            // Marks the existing file as modified so Unity knows it needs to be saved
            EditorUtility.SetDirty(databaseAsset);
        }

        // Force Unity to write the data changes out to the disk safely
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully built a single database asset with {databaseAsset.personas.Count} personas at {assetPath}!");
    }
}
#endif