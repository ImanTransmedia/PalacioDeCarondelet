using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;
using System.Linq;
using UnityEditor.AddressableAssets;

public class CheckAddressablesDuplicates
{
    [MenuItem("Tools/Addressables/Buscar Addressables duplicados")]
    static void FindDuplicates()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("No se encontró la configuración de Addressables");
            return;
        }

        // 1) Bundles duplicados
        var allEntries = settings.groups.SelectMany(g => g.entries);
        var bundleNames = allEntries
            .Where(e => e != null && !string.IsNullOrEmpty(e.BundleFileId))
            .Select(e => e.BundleFileId);

        var dupBundles = bundleNames
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (!dupBundles.Any())
            Debug.Log("No hay AssetBundle repetidos en Addressables.");
        else
            foreach (var n in dupBundles)
                Debug.LogError($"Bundle repetido en Addressables: \"{n}\"");

        // 2) Direcciones (addresses) duplicadas
        var dupAddresses = allEntries
            .Where(e => !string.IsNullOrEmpty(e.address))
            .GroupBy(e => e.address)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (!dupAddresses.Any())
            Debug.Log("No hay direcciones duplicadas en Addressables.");
        else
            foreach (var addr in dupAddresses)
                Debug.LogError($"Address duplicada: \"{addr}\"");
    }
}
