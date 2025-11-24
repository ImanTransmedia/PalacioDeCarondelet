using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;

[System.Serializable]
public class SceneGroup
{
    public string name;
    public SceneAsset mainScene;
    public List<SceneAsset> subScenes = new List<SceneAsset>();
    public bool enabled = true;

    public IEnumerable<string> Paths()
    {
        if (mainScene) yield return AssetDatabase.GetAssetPath(mainScene);
        foreach (var s in subScenes)
            if (s) yield return AssetDatabase.GetAssetPath(s);
    }
}

public class SceneOrgPreset : ScriptableObject
{
    public string rootFolder = "Assets/_Carondelet";
    public string folder3DName = "_3D";
    public string folderPrefabsName = "_Prefabs";
    public string folderCommonName = "_COMMON";

    public bool dryRun = true;
    public bool duplicateOverrides = true;
    public bool extractModelMaterials = true;
    public bool verboseLog = true;

    public List<SceneGroup> groups = new List<SceneGroup>();
    public List<SceneAsset> blacklist = new List<SceneAsset>();
}

public class OrganizeProject : EditorWindow
{
    [MenuItem("Tools/Limpiar y Organizar/Organizar Proyecto (Pro)")]
    static void Open()
    {
        var w = GetWindow<OrganizeProject>("Organizar Proyecto (Pro)");
        w.minSize = new Vector2(760, 600);
    }

    // Preset seleccionado (persistente)
    private SceneOrgPreset preset;
    private Vector2 leftScroll, rightScroll, logScroll;
    private int selectedGroup = -1;
    private readonly List<string> opLog = new List<string>();

    // Extensiones / tipos
    private static readonly HashSet<string> kTextureExt = new HashSet<string> {
        ".png",".jpg",".jpeg",".tga",".psd",".tif",".tiff",".bmp",".exr",".hdr",".dds",".ktx",".ktx2",".webp"
    };
    private static readonly HashSet<string> kModelExt = new HashSet<string> { ".fbx", ".obj", ".dae", ".blend", ".3ds" };
    private static readonly HashSet<string> kSceneExt = new HashSet<string> { ".unity" };
    private static readonly HashSet<string> kScriptExt = new HashSet<string> { ".cs", ".js", ".boo", ".cginc", ".hlsl", ".shader", ".shadergraph" };

    // -------- UI --------

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPresetPanel();
            GUILayout.Space(8);
            DrawGroupsPanel();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Registro de operaciones", EditorStyles.boldLabel);
        using (var sv = new EditorGUILayout.ScrollViewScope(logScroll, GUILayout.MinHeight(180)))
        {
            foreach (var line in opLog) EditorGUILayout.LabelField("• " + line);
            logScroll = sv.scrollPosition;
        }
        if (GUILayout.Button("Limpiar log")) opLog.Clear();
    }

    private void DrawPresetPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(340)))
        {
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            preset = (SceneOrgPreset)EditorGUILayout.ObjectField(new GUIContent("Preset asset"), preset, typeof(SceneOrgPreset), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nuevo"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Crear preset", "OrganizePreset", "asset", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        preset = ScriptableObject.CreateInstance<SceneOrgPreset>();
                        AssetDatabase.CreateAsset(preset, path);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }

                GUI.enabled = preset;
                if (GUILayout.Button("Guardar"))
                {
                    EditorUtility.SetDirty(preset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Log("Preset guardado.");
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(6);

            GUI.enabled = preset;
            EditorGUILayout.LabelField("Carpetas destino", EditorStyles.boldLabel);
            preset.rootFolder = EditorGUILayout.TextField("Carpeta raíz", preset.rootFolder);
            preset.folder3DName = EditorGUILayout.TextField("Carpeta 3D", preset.folder3DName);
            preset.folderPrefabsName = EditorGUILayout.TextField("Carpeta Prefabs", preset.folderPrefabsName);
            preset.folderCommonName = EditorGUILayout.TextField("Carpeta COMMON", preset.folderCommonName);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Opciones", EditorStyles.boldLabel);
            preset.dryRun = EditorGUILayout.ToggleLeft("Solo simulación (Dry-run)", preset.dryRun);
            preset.duplicateOverrides = EditorGUILayout.ToggleLeft("Duplicar overrides", preset.duplicateOverrides);
            preset.extractModelMaterials = EditorGUILayout.ToggleLeft("Extraer materiales embebidos (FBX/OBJ)", preset.extractModelMaterials);
            preset.verboseLog = EditorGUILayout.ToggleLeft("Log detallado", preset.verboseLog);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Blacklist de escenas", EditorStyles.boldLabel);
            using (var sv = new EditorGUILayout.ScrollViewScope(leftScroll, GUILayout.Height(180)))
            {
                int remove = -1;
                for (int i = 0; i < preset.blacklist.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        preset.blacklist[i] = (SceneAsset)EditorGUILayout.ObjectField(preset.blacklist[i], typeof(SceneAsset), false);
                        if (GUILayout.Button("X", GUILayout.Width(22))) remove = i;
                    }
                }
                if (remove >= 0) preset.blacklist.RemoveAt(remove);
                if (GUILayout.Button("+ Agregar escena")) preset.blacklist.Add(null);
                leftScroll = sv.scrollPosition;
            }

            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cargar Build Settings como 1 grupo"))
                    LoadBuildSettingsAsSingleGroup();
                if (GUILayout.Button("Usar escenas abiertas como 1 grupo"))
                    LoadOpenScenesAsSingleGroup();
            }

            GUI.enabled = true;
        }
    }

    private void DrawGroupsPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Grupos de escenas (principal + subescenas)", EditorStyles.boldLabel);

            GUI.enabled = preset;
            using (var sv = new EditorGUILayout.ScrollViewScope(rightScroll, GUILayout.Height(320)))
            {
                if (preset != null)
                {
                    for (int i = 0; i < preset.groups.Count; i++)
                    {
                        var g = preset.groups[i];
                        EditorGUILayout.Space(2);
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                g.enabled = EditorGUILayout.Toggle(g.enabled, GUILayout.Width(16));
                                g.name = EditorGUILayout.TextField(g.name ?? $"Grupo {i + 1}");
                                if (selectedGroup == i) GUILayout.Label("◉", GUILayout.Width(18));
                                else if (GUILayout.Button("Seleccionar", GUILayout.Width(90))) selectedGroup = i;

                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0) { var tmp = preset.groups[i - 1]; preset.groups[i - 1] = g; preset.groups[i] = tmp; }
                                if (GUILayout.Button("↓", GUILayout.Width(24)) && i < preset.groups.Count - 1) { var tmp = preset.groups[i + 1]; preset.groups[i + 1] = g; preset.groups[i] = tmp; }
                                if (GUILayout.Button("X", GUILayout.Width(24))) { preset.groups.RemoveAt(i); i--; continue; }
                            }

                            g.mainScene = (SceneAsset)EditorGUILayout.ObjectField("Escena principal", g.mainScene, typeof(SceneAsset), false);

                            EditorGUILayout.LabelField("Subescenas");
                            int remove = -1;
                            for (int j = 0; j < g.subScenes.Count; j++)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    g.subScenes[j] = (SceneAsset)EditorGUILayout.ObjectField(g.subScenes[j], typeof(SceneAsset), false);
                                    if (GUILayout.Button("X", GUILayout.Width(22))) remove = j;
                                }
                            }
                            if (remove >= 0) g.subScenes.RemoveAt(remove);
                            if (GUILayout.Button("+ Agregar subescena", GUILayout.Width(180))) g.subScenes.Add(null);

                            EditorGUILayout.Space(2);
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Organizar este grupo"))
                                {
                                    if (ValidateConfig(out string err)) OrganizeGroup(g);
                                    else EditorUtility.DisplayDialog("Config inválida", err, "OK");
                                }
                                if (GUILayout.Button("Abrir escenas de este grupo"))
                                    OpenScenesTemp(g);
                            }
                        }
                    }
                }
                rightScroll = sv.scrollPosition;
            }

            if (GUILayout.Button("+ Nuevo grupo", GUILayout.Width(160)) && preset != null)
                preset.groups.Add(new SceneGroup { name = $"Grupo {preset.groups.Count + 1}" });

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Organizar grupos seleccionados/activos"))
                {
                    if (ValidateConfig(out string err)) OrganizeActiveGroups();
                    else EditorUtility.DisplayDialog("Config inválida", err, "OK");
                }

                if (GUILayout.Button("Organizar TODOS los grupos"))
                {
                    if (ValidateConfig(out string err)) OrganizeAllGroups();
                    else EditorUtility.DisplayDialog("Config inválida", err, "OK");
                }
            }
            GUI.enabled = true;
        }
    }

    // ------- Helpers UI -------
    private void LoadBuildSettingsAsSingleGroup()
    {
        if (preset == null) return;
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)).Where(s => s).ToList();
        if (scenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin escenas", "No hay escenas habilitadas en Build Settings.", "OK");
            return;
        }
        preset.groups.Clear();
        var g = new SceneGroup { name = "Build Settings", mainScene = scenes[0] };
        for (int i = 1; i < scenes.Count; i++) g.subScenes.Add(scenes[i]);
        preset.groups.Add(g);
        Log("Cargado Build Settings como 1 grupo.");
    }

    private void LoadOpenScenesAsSingleGroup()
    {
        if (preset == null) return;
        var open = GetOpenSceneAssets();
        if (open.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin escenas abiertas", "Abre al menos una escena.", "OK");
            return;
        }
        preset.groups.Clear();
        var g = new SceneGroup { name = "Escenas Abiertas", mainScene = open[0] };
        for (int i = 1; i < open.Count; i++) g.subScenes.Add(open[i]);
        preset.groups.Add(g);
        Log("Cargadas escenas abiertas como 1 grupo.");
    }

    private List<SceneAsset> GetOpenSceneAssets()
    {
        var list = new List<SceneAsset>();
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (s.isLoaded && !string.IsNullOrEmpty(s.path))
                list.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path));
        }
        return list;
    }

    private void Log(string msg)
    {
        if (preset != null && preset.verboseLog) opLog.Add(msg);
    }

    private bool ValidateConfig(out string error)
    {
        error = null;
        if (preset == null) { error = "Selecciona o crea un preset."; return false; }
        if (string.IsNullOrEmpty(preset.rootFolder) || !preset.rootFolder.StartsWith("Assets"))
        { error = "Carpeta raíz inválida. Debe estar dentro de Assets/"; return false; }
        if (preset.groups.Count == 0) { error = "No hay grupos definidos."; return false; }
        foreach (var g in preset.groups)
        {
            if (!g.enabled) continue;
            if (!g.mainScene) { error = $"Grupo «{g.name}»: falta escena principal."; return false; }
        }
        return true;
    }


    private void OrganizeActiveGroups()
    {
        foreach (var g in preset.groups)
            if (g.enabled) OrganizeGroup(g);
        EditorUtility.DisplayDialog("Listo", "Organización completada para grupos activos.", "OK");
    }

    private void OrganizeAllGroups()
    {
        foreach (var g in preset.groups)
            OrganizeGroup(g);
        EditorUtility.DisplayDialog("Listo", "Organización completada para todos los grupos.", "OK");
    }

    private void OrganizeGroup(SceneGroup group)
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var groupPaths = group.Paths()
                                  .Where(p => !string.IsNullOrEmpty(p))
                                  .Where(p => !IsBlacklisted(p))
                                  .Distinct()
                                  .ToList();
            if (groupPaths.Count == 0)
            {
                Log($"[Grupo {group.name}] No hay escenas (o todas en blacklist).");
                return;
            }

            OpenScenesForProcessing(groupPaths);

            RunOrganizeForCurrentlyOpenScenes(group.name);
        }
        finally
        {
            // Restaurar setup
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private void OpenScenesTemp(SceneGroup group)
    {
        var groupPaths = group.Paths()
                              .Where(p => !string.IsNullOrEmpty(p))
                              .Where(p => !IsBlacklisted(p))
                              .Distinct()
                              .ToList();
        if (groupPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Aviso", "No hay escenas (o todas en blacklist).", "OK");
            return;
        }
        var save = EditorUtility.DisplayDialogComplex("Abrir escenas",
            $"Se abrirán {groupPaths.Count} escena(s). ¿Deseas guardar las escenas actuales antes?",
            "Guardar y abrir", "Cancelar", "Abrir sin guardar");

        if (save == 0) EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Abrir
        OpenScenesForProcessing(groupPaths);
    }

    private bool IsBlacklisted(string scenePath)
    {
        if (preset == null) return false;
        var bl = new HashSet<string>(preset.blacklist.Where(s => s).Select(s => AssetDatabase.GetAssetPath(s)));
        return bl.Contains(scenePath);
    }

    private void OpenScenesForProcessing(List<string> scenePaths)
    {
        for (int i = 0; i < scenePaths.Count; i++)
        {
            var mode = (i == 0) ? OpenSceneMode.Single : OpenSceneMode.Additive;
            var opened = EditorSceneManager.OpenScene(scenePaths[i], mode);
            if (!opened.IsValid()) Log($"[WARN] No se pudo abrir: {scenePaths[i]}");
        }
    }

    private void RunOrganizeForCurrentlyOpenScenes(string labelForLogs)
    {
        if (preset == null) return;

        string base3D = $"{preset.rootFolder}/{preset.folder3DName}";
        string basePref = $"{preset.rootFolder}/{preset.folderPrefabsName}";
        EnsureFolder(base3D); EnsureFolder(basePref);

        // COMMON
        string common3D = $"{base3D}/{preset.folderCommonName}";
        string commonPref = $"{basePref}/{preset.folderCommonName}";
        string commonObjs3D = $"{common3D}/Objetos";
        string commonMats3D = $"{common3D}/Materiales";
        string commonTexs3D = $"{common3D}/Texturas";
        EnsureFolder(common3D); EnsureFolder(commonPref);
        EnsureFolder(commonObjs3D); EnsureFolder(commonMats3D); EnsureFolder(commonTexs3D);

        var loaded = GetLoadedScenesWithPath();
        if (loaded.Count == 0) { Log("[WARN] No hay escenas abiertas para procesar."); return; }
        string mainSceneName = Path.GetFileNameWithoutExtension(loaded[0].path);

        string scene3D = $"{base3D}/{MakeSafe(mainSceneName)}";
        string scenePref = $"{basePref}/{MakeSafe(mainSceneName)}";
        EnsureFolder(scene3D); EnsureFolder(scenePref);

        var loadedPaths = new HashSet<string>(loaded.Select(s => s.path));
        var guidToOtherScenes = BuildAssetToOtherScenesMap(loadedPaths);

        var meshToMats = new Dictionary<string, HashSet<string>>();
        var prefabToMats = new Dictionary<string, HashSet<string>>();
        var overrideMats = new HashSet<string>();

        foreach (var sc in loaded)
            foreach (var root in sc.GetRootGameObjects())
                CollectGO(root, meshToMats, prefabToMats, overrideMats);

        var allMatPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var set in meshToMats.Values) foreach (var p in set) allMatPaths.Add(p);
        foreach (var set in prefabToMats.Values) foreach (var p in set) allMatPaths.Add(p);
        var texGuidToMatGuids = BuildTextureToMaterialsMap(allMatPaths);

        foreach (var kv in meshToMats)
        {
            string meshPath = kv.Key;
            if (IsFont(meshPath) || string.IsNullOrEmpty(meshPath)) continue;
            string meshGuid = AssetDatabase.AssetPathToGUID(meshPath);
            bool isCommonObj = IsCommonByOtherScenes(meshGuid, guidToOtherScenes);
            string objFolder = isCommonObj ? commonObjs3D : $"{scene3D}/{MakeSafe(Path.GetFileNameWithoutExtension(meshPath))}";
            EnsureFolder(objFolder);

            MoveSmart(meshPath, objFolder, tag: $"[{labelForLogs}] Model");

            foreach (var matPath in kv.Value)
                ProcessMaterialAndTextures(
                    matPath, objFolder, commonMats3D, commonTexs3D,
                    texGuidToMatGuids, guidToOtherScenes,
                    isPrefabOverride: overrideMats.Contains(matPath)
                );
        }

        // Prefabs
        foreach (var kv in prefabToMats)
        {
            string prefabPath = kv.Key;
            if (IsFont(prefabPath) || string.IsNullOrEmpty(prefabPath)) continue;

            string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            bool isCommonPrefab = IsCommonByOtherScenes(prefabGuid, guidToOtherScenes);
            string targetFolder = isCommonPrefab ? commonPref : scenePref;
            EnsureFolder(targetFolder);
            MoveSmart(prefabPath, targetFolder, tag: $"[{labelForLogs}] Prefab");

            foreach (var matPath in kv.Value)
                ProcessMaterialAndTextures(
                    matPath, scene3D, commonMats3D, commonTexs3D,
                    texGuidToMatGuids, guidToOtherScenes,
                    isPrefabOverride: overrideMats.Contains(matPath)
                );
        }

        // Limpieza
        CleanupEmptyFolders(preset.rootFolder, new[]
        {
            preset.rootFolder, base3D, basePref,
            common3D, commonObjs3D, commonMats3D, commonTexs3D,
            scene3D, scenePref, commonPref
        });

        AssetDatabase.Refresh();
        Log($"[{labelForLogs}] Organización completada {(preset.dryRun ? "(Dry-run)" : "")}.");
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
        var mf = go.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
            Register(go, AssetDatabase.GetAssetPath(mf.sharedMesh), meshMap, overrideMatsInScene);

        var smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr && smr.sharedMesh)
            Register(go, AssetDatabase.GetAssetPath(smr.sharedMesh), meshMap, overrideMatsInScene);

        string pPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        if (!string.IsNullOrEmpty(pPath))
            Register(go, pPath, prefabMap, overrideMatsInScene);

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
        if (isCommon) targetFolder = commonMaterialsFolder;
        else if (isPrefabOverride && preset.duplicateOverrides && !IsUnderFolder(matPath, preset.rootFolder))
            targetFolder = $"{objectFolderForLocal}/{MakeSafe(Path.GetFileNameWithoutExtension(matPath))}";
        else targetFolder = objectFolderForLocal;

        EnsureFolder(targetFolder);

        string finalMatPath = matPath;
        if (isPrefabOverride && preset.duplicateOverrides)
        {
            finalMatPath = DuplicateMaterialAsset(matPath, targetFolder);
            Log($"Duplicado override: {Path.GetFileName(matPath)} → {finalMatPath} (considera reasignar en escena)");
        }
        else
        {
            finalMatPath = MoveSmart(matPath, targetFolder, tag: "Material");
        }

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

        if (!preset.extractModelMaterials) return matPath;

        var matObj = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (matObj == null) return matPath;

        string ownerPath = AssetDatabase.GetAssetPath(matObj);
        string ownerExt = Path.GetExtension(ownerPath).ToLowerInvariant();

        if (kModelExt.Contains(ownerExt))
        {
            string targetDir = $"{fallbackFolder}/Extracted_Materials";
            EnsureFolder(targetDir);

            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{targetDir}/{MakeSafe(matObj.name)}.mat");
            Log($"[EXTRACT] {matObj.name} → {newPath} (simulación:{preset.dryRun})");

            if (!preset.dryRun)
            {
                string extractResult = AssetDatabase.ExtractAsset(matObj, newPath);
                if (string.IsNullOrEmpty(extractResult))
                {
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

    private string DuplicateMaterialAsset(string sourceMatPath, string targetFolder)
    {
        string name = Path.GetFileName(sourceMatPath);
        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{name}");
        if (!preset.dryRun)
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
        string destCandidate = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{fn}");

        if (assetPath.Equals(destCandidate, System.StringComparison.OrdinalIgnoreCase))
            return assetPath;

        if (assetPath.StartsWith("Packages/"))
        {
            Log($"[{tag}] {fn} viene de Packages → se duplicará en {targetFolder}");
            if (!preset.dryRun) AssetDatabase.CopyAsset(assetPath, destCandidate);
            return destCandidate;
        }

        if (!IsUnderFolder(assetPath, "Assets"))
        {
            Log($"[{tag}] {fn} está fuera de Assets → se omite mover.");
            return assetPath;
        }

        if (!preset.dryRun)
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
            if (subs != null) foreach (var s in subs) stack.Push(s);
        }
    }

    private static string MakeSafe(string s)
    {
        if (string.IsNullOrEmpty(s)) return "_";
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
