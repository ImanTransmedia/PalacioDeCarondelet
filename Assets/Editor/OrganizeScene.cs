using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class OrganizeScenePro : EditorWindow
{
    [Header("Carpetas")]
    [SerializeField] private string rootFolder = "Assets/_Carondelet";
    [SerializeField] private string folder3DName = "_3D";
    [SerializeField] private string folderPrefabsName = "_Prefabs";
    [SerializeField] private string folderCommonName = "_COMMON";

    [Header("Opciones")]
    [SerializeField] private bool dryRun = false;                // Solo simular (no mueve/duplica)
    [SerializeField] private bool duplicateOverrides = true;     // Duplicar materiales override en vez de mover el original
    [SerializeField] private bool extractModelMaterials = true;  // Extraer materiales embebidos en FBX/OBJ a .mat
    [SerializeField] private bool verboseLog = true;

    // UI
    private Vector2 logScroll;
    private SearchField searchField;
    private readonly List<string> opLog = new List<string>();

    // Extensiones / tipos
    private static readonly HashSet<string> kTextureExt = new HashSet<string> {
        ".png",".jpg",".jpeg",".tga",".psd",".tif",".tiff",".bmp",".exr",".hdr",".dds",".ktx",".ktx2",".webp"
    };
    private static readonly HashSet<string> kModelExt = new HashSet<string> { ".fbx", ".obj", ".dae", ".blend", ".3ds" };
    private static readonly HashSet<string> kSceneExt = new HashSet<string> { ".unity" };
    private static readonly HashSet<string> kScriptExt = new HashSet<string> { ".cs", ".js", ".boo", ".cginc", ".hlsl", ".shader", ".shadergraph" };
    private static readonly HashSet<string> kSkipExt = new HashSet<string> { ".ttf", ".otf", ".fnt", ".fon", ".asset", ".rendertexture" };

    [MenuItem("Tools/Limpiar y Organizar/Organizar Escenas (Pro)")]
    static void ShowWindow()
    {
        var w = GetWindow<OrganizeScenePro>("Organizar Escenas (Pro)");
        w.minSize = new Vector2(620, 560);
    }

    private void OnEnable()
    {
        searchField ??= new SearchField();
    }

    private void Log(string msg)
    {
        if (verboseLog) opLog.Add(msg);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Organizador de escenas con manejo de overrides y COMMON", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Carpeta raíz
            using (new EditorGUILayout.HorizontalScope())
            {
                rootFolder = EditorGUILayout.TextField(new GUIContent("Carpeta raíz"), rootFolder);
                if (GUILayout.Button("…", GUILayout.Width(28)))
                {
                    string abs = EditorUtility.OpenFolderPanel("Selecciona carpeta raíz", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(abs) && abs.StartsWith(Application.dataPath))
                        rootFolder = "Assets" + abs.Substring(Application.dataPath.Length).Replace("\\", "/");
                    else
                        EditorUtility.DisplayDialog("Error", "La carpeta debe estar dentro de Assets.", "OK");
                }
            }

            // Nombres base
            using (new EditorGUILayout.HorizontalScope())
            {
                folder3DName = EditorGUILayout.TextField(new GUIContent("Nombre carpeta 3D"), folder3DName);
                folderPrefabsName = EditorGUILayout.TextField(new GUIContent("Nombre carpeta Prefabs"), folderPrefabsName);
                folderCommonName = EditorGUILayout.TextField(new GUIContent("Nombre carpeta COMMON"), folderCommonName, GUILayout.MaxWidth(220));
            }

            // Opciones
            using (new EditorGUILayout.HorizontalScope())
            {
                dryRun = EditorGUILayout.ToggleLeft(new GUIContent("Solo simulación (Dry-run)"), dryRun, GUILayout.Width(220));
                duplicateOverrides = EditorGUILayout.ToggleLeft(new GUIContent("Duplicar overrides"), duplicateOverrides, GUILayout.Width(180));
                extractModelMaterials = EditorGUILayout.ToggleLeft(new GUIContent("Extraer materiales de FBX"), extractModelMaterials, GUILayout.Width(220));
                verboseLog = EditorGUILayout.ToggleLeft(new GUIContent("Log detallado"), verboseLog);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Organizar escenas abiertas", GUILayout.Height(26)))
                    RunOrganize();

                if (GUILayout.Button("Exportar MULTIMEDIA de escenas abiertas…", GUILayout.Height(26)))
                    ExportOpenScenesMultimedia();
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Registro de operaciones", EditorStyles.boldLabel);

        using (var sv = new EditorGUILayout.ScrollViewScope(logScroll, GUILayout.MinHeight(200)))
        {
            foreach (var line in opLog)
                EditorGUILayout.LabelField("• " + line);
            logScroll = sv.scrollPosition;
        }

        if (GUILayout.Button("Limpiar log"))
            opLog.Clear();
    }


    private void RunOrganize()
    {
        opLog.Clear();

        if (string.IsNullOrEmpty(rootFolder) || !AssetDatabase.IsValidFolder(rootFolder))
        {
            EditorUtility.DisplayDialog("Error", "Carpeta raíz inválida.", "OK");
            return;
        }

        var loadedScenes = GetLoadedScenesWithPath();
        if (loadedScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin escenas", "No hay escenas cargadas en el Editor.", "OK");
            return;
        }

        // Usamos la primera escena abierta como "escena padre"
        string mainSceneName = Path.GetFileNameWithoutExtension(loadedScenes[0].path);

        // Bases
        string base3D = $"{rootFolder}/{folder3DName}";
        string basePref = $"{rootFolder}/{folderPrefabsName}";
        EnsureFolder(base3D); EnsureFolder(basePref);

        // COMMON bases
        string common3D = $"{base3D}/{folderCommonName}";
        string commonPref = $"{basePref}/{folderCommonName}";
        string commonObjs3D = $"{common3D}/Objetos";
        string commonMats3D = $"{common3D}/Materiales";
        string commonTexs3D = $"{common3D}/Texturas";
        EnsureFolder(common3D); EnsureFolder(commonPref);
        EnsureFolder(commonObjs3D); EnsureFolder(commonMats3D); EnsureFolder(commonTexs3D);

        // Por escena
        string scene3D = $"{base3D}/{MakeSafe(mainSceneName)}";
        string scenePref = $"{basePref}/{MakeSafe(mainSceneName)}";
        EnsureFolder(scene3D); EnsureFolder(scenePref);

        var loadedPaths = new HashSet<string>(loadedScenes.Select(s => s.path));
        var assetGuidToOtherScenes = BuildAssetToOtherScenesMap(loadedPaths);

        var meshToMats = new Dictionary<string, HashSet<string>>();   
        var prefabToMats = new Dictionary<string, HashSet<string>>(); 
        var overrideMatsInScene = new HashSet<string>();               

        foreach (var scene in loadedScenes)
            foreach (var root in scene.GetRootGameObjects())
                CollectGO(root, meshToMats, prefabToMats, overrideMatsInScene);

        var allMatPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var set in meshToMats.Values) foreach (var p in set) allMatPaths.Add(p);
        foreach (var set in prefabToMats.Values) foreach (var p in set) allMatPaths.Add(p);

        var texGuidToMatGuids = BuildTextureToMaterialsMap(allMatPaths);

        foreach (var kv in meshToMats)
        {
            string meshPath = kv.Key;
            if (IsFont(meshPath) || string.IsNullOrEmpty(meshPath)) continue;

            string meshGuid = AssetDatabase.AssetPathToGUID(meshPath);
            bool isCommonObj = IsCommonByOtherScenes(meshGuid, assetGuidToOtherScenes);

            string objFolder = isCommonObj ? commonObjs3D : $"{scene3D}/{MakeSafe(Path.GetFileNameWithoutExtension(meshPath))}";
            EnsureFolder(objFolder);

            MoveSmart(meshPath, objFolder, tag: "Model");

            foreach (var matPath in kv.Value)
                ProcessMaterialAndTextures(
                    matPath,
                    objFolder,
                    commonMats3D,
                    commonTexs3D,
                    texGuidToMatGuids,
                    assetGuidToOtherScenes,
                    isPrefabOverride: overrideMatsInScene.Contains(matPath)
                );
        }

        foreach (var kv in prefabToMats)
        {
            string prefabPath = kv.Key;
            if (IsFont(prefabPath) || string.IsNullOrEmpty(prefabPath)) continue;

            string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            bool isCommonPrefab = IsCommonByOtherScenes(prefabGuid, assetGuidToOtherScenes);

            string targetFolder = isCommonPrefab ? commonPref : scenePref;
            EnsureFolder(targetFolder);
            MoveSmart(prefabPath, targetFolder, tag: "Prefab");

            foreach (var matPath in kv.Value)
                ProcessMaterialAndTextures(
                    matPath,
                    objectFolderForLocal: scene3D,
                    commonMaterialsFolder: commonMats3D,
                    commonTexturesFolder: commonTexs3D,
                    texGuidToMatGuids: texGuidToMatGuids,
                    assetGuidToOtherScenes: assetGuidToOtherScenes,
                    isPrefabOverride: overrideMatsInScene.Contains(matPath)
                );
        }

        // Limpieza opcional de carpetas vacías
        CleanupEmptyFolders(rootFolder, new[]
        {
            rootFolder, base3D, basePref,
            common3D, commonObjs3D, commonMats3D, commonTexs3D,
            scene3D, scenePref, commonPref
        });

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Organización completada",
            dryRun ? "Simulación finalizada. Revisa el log para ver los cambios que se realizarían." :
                     "Operación terminada. Revisa el log para detalles.",
            "OK");
    }


    private static List<Scene> GetLoadedScenesWithPath()
    {
        var list = new List<Scene>();
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (s.isLoaded && !string.IsNullOrEmpty(s.path))
                list.Add(s);
        }
        return list;
    }

    private static void CollectGO(GameObject go,
        Dictionary<string, HashSet<string>> meshMap,
        Dictionary<string, HashSet<string>> prefabMap,
        HashSet<string> overrideMatsInScene)
    {
        // Meshes
        var mf = go.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
            Register(go, AssetDatabase.GetAssetPath(mf.sharedMesh), meshMap, overrideMatsInScene);

        var smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr && smr.sharedMesh)
            Register(go, AssetDatabase.GetAssetPath(smr.sharedMesh), meshMap, overrideMatsInScene);

        // Prefab instance
        string pPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        if (!string.IsNullOrEmpty(pPath))
            Register(go, pPath, prefabMap, overrideMatsInScene);

        // Hijos
        foreach (Transform c in go.transform)
            CollectGO(c.gameObject, meshMap, prefabMap, overrideMatsInScene);
    }

    private static void Register(GameObject go, string keyAssetPath,
        Dictionary<string, HashSet<string>> map,
        HashSet<string> overrideMatsInScene)
    {
        if (string.IsNullOrEmpty(keyAssetPath) || IsFont(keyAssetPath)) return;
        if (!map.TryGetValue(keyAssetPath, out var set))
        {
            set = new HashSet<string>();
            map[keyAssetPath] = set;
        }

        var rend = go.GetComponent<Renderer>();
        if (!rend) return;

        var mats = rend.sharedMaterials ?? System.Array.Empty<Material>();
        foreach (var m in mats)
        {
            if (!m) continue;
            string mPath = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(mPath)) continue;

            set.Add(mPath);

            var stagePrefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (stagePrefab != null)
                overrideMatsInScene.Add(mPath);
        }
    }

    private static Dictionary<string, HashSet<string>> BuildAssetToOtherScenesMap(HashSet<string> loadedScenePaths)
    {
        var dict = new Dictionary<string, HashSet<string>>();
        foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (loadedScenePaths.Contains(scenePath)) continue;

            var deps = AssetDatabase.GetDependencies(scenePath, true);
            foreach (var dep in deps)
            {
                string depGuid = AssetDatabase.AssetPathToGUID(dep);
                if (string.IsNullOrEmpty(depGuid)) continue;
                if (!dict.TryGetValue(depGuid, out var set))
                {
                    set = new HashSet<string>();
                    dict[depGuid] = set;
                }
                set.Add(scenePath);
            }
        }
        return dict;
    }

    private static bool IsCommonByOtherScenes(string assetGuid, Dictionary<string, HashSet<string>> map)
    {
        return !string.IsNullOrEmpty(assetGuid)
            && map.TryGetValue(assetGuid, out var scenes)
            && scenes != null
            && scenes.Count >= 2;
    }

    private static IEnumerable<string> GetMaterialTextureGuids(string matPath)
    {
        foreach (var dep in AssetDatabase.GetDependencies(matPath, true))
        {
            string ext = Path.GetExtension(dep).ToLowerInvariant();
            if (kTextureExt.Contains(ext))
            {
                string g = AssetDatabase.AssetPathToGUID(dep);
                if (!string.IsNullOrEmpty(g))
                    yield return g;
            }
        }
    }

    private static Dictionary<string, HashSet<string>> BuildTextureToMaterialsMap(HashSet<string> materialPaths)
    {
        var map = new Dictionary<string, HashSet<string>>();
        foreach (var matPath in materialPaths)
        {
            if (string.IsNullOrEmpty(matPath)) continue;
            string matGuid = AssetDatabase.AssetPathToGUID(matPath);
            if (string.IsNullOrEmpty(matGuid)) continue;

            foreach (var texGuid in GetMaterialTextureGuids(matPath))
            {
                if (!map.TryGetValue(texGuid, out var set))
                {
                    set = new HashSet<string>();
                    map[texGuid] = set;
                }
                set.Add(matGuid);
            }
        }
        return map;
    }


    private void ProcessMaterialAndTextures(
        string matPath,
        string objectFolderForLocal,
        string commonMaterialsFolder,
        string commonTexturesFolder,
        Dictionary<string, HashSet<string>> texGuidToMatGuids,
        Dictionary<string, HashSet<string>> assetGuidToOtherScenes,
        bool isPrefabOverride)
    {
        if (IsFont(matPath) || string.IsNullOrEmpty(matPath)) return;

        string fixedMatPath = EnsureMaterialIsExtractedAsset(matPath, objectFolderForLocal);
        if (fixedMatPath != matPath) Log($"Extraído material: {Path.GetFileName(matPath)} → {fixedMatPath}");
        matPath = fixedMatPath;

        string matGuid = AssetDatabase.AssetPathToGUID(matPath);
        bool isCommon = IsCommonByOtherScenes(matGuid, assetGuidToOtherScenes);

        string targetFolder;
        if (isCommon)
        {
            targetFolder = commonMaterialsFolder;
        }
        else
        {
            if (isPrefabOverride && duplicateOverrides && !IsUnderFolder(matPath, rootFolder))
            {
                targetFolder = $"{objectFolderForLocal}/{MakeSafe(Path.GetFileNameWithoutExtension(matPath))}";
            }
            else
            {
                targetFolder = objectFolderForLocal;
            }
        }
        EnsureFolder(targetFolder);

        string finalMatPath = matPath;

        if (isPrefabOverride && duplicateOverrides)
        {
            finalMatPath = DuplicateMaterialAsset(matPath, targetFolder);
            Log($"Duplicado override: {Path.GetFileName(matPath)} → {finalMatPath} (considera reasignar en escena)");
        }
        else
        {
            // Mover si procede
            finalMatPath = MoveSmart(matPath, targetFolder, tag: "Material");
        }

        // Texturas del material
        foreach (var texGuid in GetMaterialTextureGuids(finalMatPath))
        {
            string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
            if (string.IsNullOrEmpty(texPath)) continue;

            bool shared = texGuidToMatGuids.TryGetValue(texGuid, out var mats) && mats.Count >= 2;
            string texTarget = (isCommon || shared) ? commonTexturesFolder : Path.GetDirectoryName(finalMatPath).Replace("\\", "/");
            EnsureFolder(texTarget);

            MoveSmart(texPath, texTarget, tag: "Texture");
        }
    }

    private string EnsureMaterialIsExtractedAsset(string matPath, string fallbackFolder)
    {
        if (Path.GetExtension(matPath).ToLowerInvariant() == ".mat" && IsUnderFolder(matPath, "Assets"))
            return matPath;

        if (!extractModelMaterials) return matPath;

        var matObj = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (matObj == null) return matPath;

        string ownerPath = AssetDatabase.GetAssetPath(matObj);
        string ownerExt = Path.GetExtension(ownerPath).ToLowerInvariant();

        if (kModelExt.Contains(ownerExt))
        {
            string targetDir = $"{fallbackFolder}/Extracted_Materials";
            EnsureFolder(targetDir);

            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{targetDir}/{MakeSafe(matObj.name)}.mat");
            Log($"[EXTRACT] {matObj.name} → {newPath} (simulación:{dryRun})");

            if (!dryRun)
            {
 
                if (string.IsNullOrEmpty(AssetDatabase.ExtractAsset(matObj, newPath))) { 
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    return newPath;
                }
                else
                {
                    Log($"[WARN] Falló ExtractAsset de {matObj.name} desde {ownerPath}");
                }
            }
            return matPath;
        }

        return matPath;
    }

    private static string MakeProjectRelative(string absolute)
    {
        absolute = absolute.Replace("\\", "/");
        string data = Application.dataPath.Replace("\\", "/");
        if (absolute.StartsWith(data))
            return "Assets" + absolute.Substring(data.Length);
        return absolute;
    }

    private string DuplicateMaterialAsset(string sourceMatPath, string targetFolder)
    {
        string name = Path.GetFileName(sourceMatPath);
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{name}");
        if (!dryRun)
        {
            if (!AssetDatabase.CopyAsset(sourceMatPath, newPath))
                Log($"[WARN] No se pudo duplicar: {sourceMatPath}");
        }
        return newPath;
    }

    private string MoveSmart(string assetPath, string targetFolder, string tag)
    {
        if (string.IsNullOrEmpty(assetPath)) return assetPath;

        string fn = Path.GetFileName(assetPath);
        string destCandidate = $"{targetFolder}/{fn}";
        destCandidate = AssetDatabase.GenerateUniqueAssetPath(destCandidate);

        if (assetPath.Equals(destCandidate, System.StringComparison.OrdinalIgnoreCase))
            return assetPath;

        if (assetPath.StartsWith("Packages/"))
        {
            Log($"[{tag}] {fn} viene de Packages → se duplicará en {targetFolder}");
            if (!dryRun) AssetDatabase.CopyAsset(assetPath, destCandidate);
            return destCandidate;
        }

        if (!IsUnderFolder(assetPath, "Assets"))
        {
            Log($"[{tag}] {fn} está fuera de Assets → se omite mover.");
            return assetPath;
        }

        if (!dryRun)
        {
            string err = AssetDatabase.MoveAsset(assetPath, destCandidate);
            if (!string.IsNullOrEmpty(err))
            {
                Log($"[{tag}] MOVE fallo ({err}) → intento COPY a {targetFolder}");
                if (!AssetDatabase.CopyAsset(assetPath, destCandidate))
                    Log($"[{tag}] COPY también falló: {assetPath}");
            }
        }

        Log($"[{tag}] {fn} → {targetFolder}");
        return destCandidate;
    }

    private static bool IsUnderFolder(string assetPath, string folder)
    {
        assetPath = assetPath.Replace("\\", "/");
        folder = folder.Replace("\\", "/");
        if (!folder.EndsWith("/")) folder += "/";
        return assetPath.StartsWith(folder, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFont(string path)
    {
        var e = Path.GetExtension(path).ToLowerInvariant();
        return e == ".ttf" || e == ".otf" || e == ".fnt" || e == ".fon";
    }

    private static void EnsureFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;
        string parent = Path.GetDirectoryName(fullPath).Replace("\\", "/");
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(fullPath));
    }

    private static void CleanupEmptyFolders(string root, IEnumerable<string> protectedFolders)
    {
        var protect = new HashSet<string>(protectedFolders ?? Enumerable.Empty<string>()) { root };
        var all = GetAllFolders(root).OrderByDescending(p => p.Count(c => c == '/')).ToList();

        bool deleted;
        int guard = 0;
        do
        {
            deleted = false;
            foreach (var folder in all)
            {
                if (protect.Contains(folder)) continue;

                var guids = AssetDatabase.FindAssets("", new[] { folder });
                if (guids.Length == 0)
                {
                    var subs = AssetDatabase.GetSubFolders(folder);
                    if (subs == null || subs.Length == 0)
                    {
                        if (AssetDatabase.DeleteAsset(folder))
                            deleted = true;
                    }
                }
            }
            guard++;
        } while (deleted && guard < 50);
    }

    private static IEnumerable<string> GetAllFolders(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var f = stack.Pop();
            yield return f;
            var subs = AssetDatabase.GetSubFolders(f);
            if (subs != null)
                foreach (var s in subs) stack.Push(s);
        }
    }

    private static string MakeSafe(string s)
    {
        if (string.IsNullOrEmpty(s)) return "_";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }



    private void ExportOpenScenesMultimedia()
    {
        var scenes = GetLoadedScenesWithPath().Select(s => s.path).ToList();
        if (scenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin escenas", "No hay escenas abiertas.", "OK");
            return;
        }

        // Dependencias de escenas abiertas
        var deps = new HashSet<string>();
        foreach (var s in scenes)
            foreach (var d in AssetDatabase.GetDependencies(s, true))
                deps.Add(d);

        var exportList = deps.Where(IsMultimediaOrScene).ToList();

        if (exportList.Count == 0)
        {
            EditorUtility.DisplayDialog("Nada para exportar", "No se encontraron assets multimedia para exportar.", "OK");
            return;
        }

        string save = EditorUtility.SaveFilePanel("Exportar multimedia de escenas abiertas", "", "EscenasMultimedia.unitypackage", "unitypackage");
        if (string.IsNullOrEmpty(save)) return;

        if (!dryRun)
            AssetDatabase.ExportPackage(exportList.ToArray(), save, ExportPackageOptions.Interactive);
        EditorUtility.RevealInFinder(save);
    }

    private static bool IsMultimediaOrScene(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (kSceneExt.Contains(ext)) return true;              // Escenas
        if (kModelExt.Contains(ext)) return true;              // Modelos
        if (kTextureExt.Contains(ext)) return true;            // Texturas
        if (ext == ".mat") return true;                        // Materiales
        if (ext == ".prefab") return true;                     // Prefabs
        if (ext == ".anim" || ext == ".controller") return true; // Anim/Animator
        if (ext == ".wav" || ext == ".mp3" || ext == ".ogg") return true; // Audio
        if (kScriptExt.Contains(ext)) return false;            // Nada de scripts/shaders
        if (path.StartsWith("Packages/")) return false;        // Omitir paquetes
        if (path.EndsWith(".asmdef") || path.EndsWith(".asmref")) return false;
        if (path.Contains("/Editor/")) return false;
        return false;
    }
}
