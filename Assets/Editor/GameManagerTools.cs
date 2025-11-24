using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameManagerTools
{
    [MenuItem("Tools/Carondelet/Borrar cache de configuración")]
    public static void ClearConfigCache()
    {
        string path = Path.Combine(Application.persistentDataPath, "configInteractuables.json");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[GameManager] Cache borrado: {path}");
        }
        else
        {
            Debug.Log("[GameManager] No hay cache para borrar.");
        }
    }
}