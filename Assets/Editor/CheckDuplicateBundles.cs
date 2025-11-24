using UnityEditor;
using UnityEngine;
using System.Linq;

public class CheckDuplicateBundles
{
    [MenuItem("Tools/Addressables/Buscar AssetBundle duplicados")]
    static void FindDuplicates()
    {
        string[] allNames = AssetDatabase.GetAllAssetBundleNames();

        var duplicates = allNames
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (!duplicates.Any())
        {
            Debug.Log("No hay AssetBundle repetidos.");
        }
        else
        {
            foreach (var name in duplicates)
                Debug.LogError($"AssetBundle duplicado: \"{name}\"");
        }
    }
}
