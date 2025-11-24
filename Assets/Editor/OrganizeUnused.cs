

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class OrganizeUnusedPro : EditorWindow
{
    // --- Configuración ---
    [SerializeField] private string rootFolder = "Assets/_Carondelet";
    [SerializeField] private string eliminarFolderName = "_ELIMINAR";
    [SerializeField] private bool showConfirmations = true;

    // --- Estado UI ---
    private int tabIndex = 0;
    private readonly string[] tabs = new[] { "Resumen", "Escenas", "Prefabs", "Modelos", "Texturas", "Materiales", "Opciones" };
    private SearchField searchField;
    private string search = "";
    private Vector2 scrollSummary, scrollScenes, scrollPrefabs, scrollModels, scrollTextures, scrollMaterials;

    // --- Datos ---
    private readonly List<string> sceneList = new List<string>();
    private readonly List<string> unusedPrefabs = new List<string>();
    private readonly List<string> unusedModels = new List<string>();
    private readonly List<string> unusedTextures = new List<string>();
    private readonly List<string> unusedMaterials = new List<string>();
    private readonly List<string> unusedAssets = new List<string>();

    // Selección (para acciones en bloque)
    private readonly HashSet<string> selected = new HashSet<string>();

    // Estilos
    private GUIStyle badgeStyle;
    private GUIStyle headerStyle;

    [MenuItem("Tools/Limpiar y Organizar/Activos No Usados (Pro)")]
    static void Init()
    {
        var window = GetWindow<OrganizeUnusedPro>("Activos No Usados (Pro)");
        window.minSize = new Vector2(620, 640);
    }

    private void OnEnable()
    {
        searchField ??= new SearchField();
        CreateStyles();
    }

    private void CreateStyles()
    {
        if (badgeStyle == null)
        {
            badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            badgeStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.75f, 0.85f, 1f) : new Color(0.12f, 0.32f, 0.65f);
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };
        }
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(2);
        DrawToolbar();
        EditorGUILayout.Space(8);

        switch (tabIndex)
        {
            case 0: DrawSummaryTab(); break;
            case 1: DrawScenesTab(); break;
            case 2: DrawListTab("Prefabs no usados", unusedPrefabs, ref scrollPrefabs); break;
            case 3: DrawListTab("Modelos no usados", unusedModels, ref scrollModels); break;
            case 4: DrawListTab("Texturas no usadas", unusedTextures, ref scrollTextures); break;
            case 5: DrawListTab("Materiales no usados", unusedMaterials, ref scrollMaterials); break;
            case 6: DrawOptionsTab(); break;
        }
    }

    // -------------------- UI Secciones --------------------

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Activos no usados en escenas", headerStyle);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    rootFolder = EditorGUILayout.TextField(new GUIContent("Carpeta raíz", "Carpeta dentro de Assets a analizar"), rootFolder);
                    if (GUILayout.Button("…", GUILayout.MaxWidth(28)))
                    {
                        string abs = EditorUtility.OpenFolderPanel("Selecciona carpeta raíz", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(abs) && abs.StartsWith(Application.dataPath))
                            rootFolder = "Assets" + abs.Substring(Application.dataPath.Length).Replace("\\", "/");
                        else
                            EditorUtility.DisplayDialog("Error", "La carpeta debe estar dentro de Assets.", "OK");
                    }
                }

                eliminarFolderName = EditorGUILayout.TextField(new GUIContent("Carpeta destino", "Subcarpeta para mover activos no usados"), eliminarFolderName);
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(210)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Escanear", GUILayout.Height(26)))
                        ScanUnusedAssets();

                    GUI.enabled = unusedAssets.Count > 0;
                    if (GUILayout.Button("Exportar", GUILayout.Height(26)))
                        ShowExportMenu();

                    GUI.enabled = unusedAssets.Count > 0 && selected.Count > 0;
                    if (GUILayout.Button("Mover Selección", GUILayout.Height(26)))
                        MoveSelectionToEliminar();

                    GUI.enabled = unusedAssets.Count > 0 && selected.Count > 0;
                    if (GUILayout.Button(new GUIContent("Eliminar", "Eliminar del proyecto (no se puede deshacer)"), GUILayout.Height(26)))
                        DeleteSelection();

                    GUI.enabled = true;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Seleccionar todo"))
                        SelectAll();
                    if (GUILayout.Button("Limpiar selección"))
                        ClearSelection();
                }
            }
        }

        // Search
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Buscar:", GUILayout.Width(48));
            search = searchField.OnToolbarGUI(search);
        }

        // Badges / contadores
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawBadge($"Escenas: {sceneList.Count}");
            DrawBadge($"Prefabs: {unusedPrefabs.Count}");
            DrawBadge($"Modelos: {unusedModels.Count}");
            DrawBadge($"Texturas: {unusedTextures.Count}");
            DrawBadge($"Materiales: {unusedMaterials.Count}");
            GUILayout.FlexibleSpace();
            DrawBadge($"Seleccionados: {selected.Count}");
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        tabIndex = GUILayout.Toolbar(tabIndex, tabs);
    }

    private void DrawSummaryTab()
    {
        using (var s = new EditorGUILayout.ScrollViewScope(scrollSummary))
        {
            scrollSummary = s.scrollPosition;

            EditorGUILayout.HelpBox(
                "El escaneo calcula dependencias partiendo **solo de las escenas** dentro de la carpeta raíz. " +
                "Si un Prefab no es referenciado (directa o indirectamente) por alguna escena, se listará como NO usado.\n\n" +
                "Usa las pestañas para revisar categorías. Puedes seleccionar ítems para moverlos a la carpeta de eliminación o borrarlos del proyecto.",
                MessageType.Info);

            DrawMiniSummary("Prefabs no usados", unusedPrefabs);
            DrawMiniSummary("Modelos no usados", unusedModels);
            DrawMiniSummary("Texturas no usadas", unusedTextures);
            DrawMiniSummary("Materiales no usados", unusedMaterials);
        }
    }

    private void DrawScenesTab()
    {
        DrawSimpleList("Escenas analizadas", sceneList, ref scrollScenes, selectable: false, showActions: true);
    }

    private void DrawListTab(string title, List<string> data, ref Vector2 scroll)
    {
        DrawSimpleList(title, data, ref scroll, selectable: true, showActions: true);
    }

    private void DrawOptionsTab()
    {
        EditorGUILayout.Space();
        GUILayout.Label("Opciones", EditorStyles.boldLabel);
        showConfirmations = EditorGUILayout.ToggleLeft("Pedir confirmación antes de mover/eliminar", showConfirmations);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Consejos:\n• Revisa la selección antes de mover o eliminar.\n" +
            "• Exporta TXT/CSV para auditar con tu equipo.\n" +
            "• Cambia la carpeta destino si deseas clasificar por lotes (p.ej. _ELIMINAR_RONDA1).",
            MessageType.None);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Escanear nuevamente"))
            ScanUnusedAssets();
    }

    // -------------------- Dibujo de listas --------------------

    private void DrawMiniSummary(string title, List<string> data)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{title} ({data.Count})", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (data.Count > 0)
                {
                    if (GUILayout.Button("Seleccionar todos", GUILayout.Width(130)))
                        AddToSelection(data);
                    if (GUILayout.Button("Copiar rutas", GUILayout.Width(100)))
                        CopyPathsToClipboard(data);
                }
            }

            int shown = 0;
            for (int i = 0; i < data.Count && i < 8; i++)
            {
                string p = data[i];
                if (!PassesSearch(p)) continue;
                shown++;
                DrawItemRow(p, selectable: true);
            }

            if (data.Count > 8)
                EditorGUILayout.LabelField($"… y {data.Count - 8} más");
        }
    }

    private void DrawSimpleList(string title, List<string> data, ref Vector2 scroll, bool selectable, bool showActions)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{title} ({data.Count})", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (showActions && data.Count > 0)
                {
                    if (selectable && GUILayout.Button("Seleccionar todos", GUILayout.Width(130)))
                        AddToSelection(data.Where(PassesSearch));

                    if (GUILayout.Button("Copiar rutas", GUILayout.Width(100)))
                        CopyPathsToClipboard(data.Where(PassesSearch));
                }
            }

            using (var sv = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.MinHeight(250)))
            {
                if (data.Count == 0)
                {
                    EditorGUILayout.HelpBox("No hay elementos.", MessageType.Info);
                }
                else
                {
                    foreach (var p in data)
                    {
                        if (!PassesSearch(p)) continue;
                        DrawItemRow(p, selectable);
                    }
                }
                scroll = sv.scrollPosition;
            }
        }
    }

    private void DrawItemRow(string assetPath, bool selectable)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (selectable)
            {
                bool isSel = selected.Contains(assetPath);
                bool newSel = GUILayout.Toggle(isSel, GUIContent.none, GUILayout.Width(18));
                if (newSel != isSel)
                {
                    if (newSel) selected.Add(assetPath);
                    else selected.Remove(assetPath);
                }
            }
            else
            {
                GUILayout.Space(18);
            }

            if (GUILayout.Button(new GUIContent(Path.GetFileName(assetPath), assetPath), EditorStyles.label))
            {
                Ping(assetPath);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Ping", GUILayout.Width(48)))
                Ping(assetPath);

            if (GUILayout.Button("Revelar", GUILayout.Width(64)))
                Reveal(assetPath);

            if (GUILayout.Button("Copiar", GUILayout.Width(58)))
                EditorGUIUtility.systemCopyBuffer = assetPath;

            // Menú contextual
            if (GUILayout.Button("⋮", GUILayout.Width(24)))
            {
                GenericMenu gm = new GenericMenu();
                gm.AddItem(new GUIContent("Seleccionar"), selected.Contains(assetPath), () => { selected.Add(assetPath); });
                gm.AddItem(new GUIContent("Quitar de selección"), !selected.Contains(assetPath), () => { selected.Remove(assetPath); });
                gm.AddSeparator("");
                gm.AddItem(new GUIContent("Mover a carpeta destino"), false, () => MovePathsToEliminar(new[] { assetPath }));
                gm.AddItem(new GUIContent("Eliminar del proyecto"), false, () => DeletePaths(new[] { assetPath }));
                gm.DropDown(GUILayoutUtility.GetLastRect());
            }
        }
    }

    private bool PassesSearch(string path)
    {
        if (string.IsNullOrEmpty(search)) return true;
        return path.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void DrawBadge(string text)
    {
        GUILayout.Label($" {text} ", badgeStyle, GUILayout.Height(18));
    }

    // -------------------- Lógica de escaneo --------------------

    private bool IsIgnored(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".ttf" || ext == ".otf" ||
               ext == ".fnt" || ext == ".fon" ||
               ext == ".asset" ||                // ScriptableObjects u otros
               ext == ".shader" || ext == ".shadergraph" ||
               ext == ".rendertexture";
    }

    private static readonly HashSet<string> kTextureExt = new HashSet<string>
    {
        ".png",".jpg",".jpeg",".tga",".psd",".tif",".tiff",".bmp",".gif",".exr",".hdr",".webp"
    };

    private static readonly HashSet<string> kModelExt = new HashSet<string> { ".fbx", ".obj", ".dae", ".blend", ".3ds" };

    private void ScanUnusedAssets()
    {
        sceneList.Clear();
        unusedPrefabs.Clear();
        unusedModels.Clear();
        unusedTextures.Clear();
        unusedMaterials.Clear();
        unusedAssets.Clear();
        selected.Clear();

        if (string.IsNullOrEmpty(rootFolder) || !AssetDatabase.IsValidFolder(rootFolder))
        {
            EditorUtility.DisplayDialog("Error", "Carpeta raíz inválida.", "OK");
            return;
        }

        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { rootFolder });
        foreach (var g in sceneGuids)
            sceneList.Add(AssetDatabase.GUIDToAssetPath(g));

        if (sceneList.Count == 0)
        {
            EditorUtility.DisplayDialog("Escaneo completado", "No se encontraron escenas en la carpeta indicada.", "OK");
            return;
        }

        // Barra de progreso
        try
        {
            EditorUtility.DisplayProgressBar("Escaneando", "Calculando dependencias desde escenas…", 0.25f);

            var used = new HashSet<string>();
            for (int i = 0; i < sceneList.Count; i++)
            {
                string scene = sceneList[i];
                EditorUtility.DisplayProgressBar("Escaneando",
                    $"Analizando dependencias: {Path.GetFileName(scene)} ({i + 1}/{sceneList.Count})",
                    0.25f + 0.65f * (i / Mathf.Max(1f, sceneList.Count - 1)));

                foreach (var dep in AssetDatabase.GetDependencies(scene, true))
                {
                    used.Add(dep);
                }
            }

            EditorUtility.DisplayProgressBar("Escaneando", "Listando categorías…", 0.95f);

            // Prefabs
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { rootFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (IsIgnored(path)) continue;
                if (!used.Contains(path))
                    unusedPrefabs.Add(path);
            }

            // Modelos
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { rootFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (IsIgnored(path)) continue;
                if (!used.Contains(path) && kModelExt.Contains(Path.GetExtension(path).ToLowerInvariant()))
                    unusedModels.Add(path);
            }

            // Texturas
            foreach (var g in AssetDatabase.FindAssets("t:Texture", new[] { rootFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (IsIgnored(path)) continue;
                if (!used.Contains(path) && kTextureExt.Contains(Path.GetExtension(path).ToLowerInvariant()))
                    unusedTextures.Add(path);
            }

            // Materiales
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { rootFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (IsIgnored(path)) continue;
                if (!used.Contains(path))
                    unusedMaterials.Add(path);
            }

            // Lista unificada
            unusedAssets.AddRange(unusedPrefabs);
            unusedAssets.AddRange(unusedModels);
            unusedAssets.AddRange(unusedTextures);
            unusedAssets.AddRange(unusedMaterials);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (unusedAssets.Count == 0)
            EditorUtility.DisplayDialog("Escaneo completado", "No se encontraron activos sin usar.", "OK");
        else
            ShowScanSummaryDialog();
    }

    private void ShowScanSummaryDialog()
    {
        EditorUtility.DisplayDialog(
            "Escaneo completado",
            $"Resumen dentro de «{rootFolder}»:\n\n" +
            $"- Prefabs no usados: {unusedPrefabs.Count}\n" +
            $"- Modelos no usados: {unusedModels.Count}\n" +
            $"- Texturas no usadas: {unusedTextures.Count}\n" +
            $"- Materiales no usados: {unusedMaterials.Count}\n\n" +
            "Revisa por pestañas o usa el buscador para filtrar.",
            "OK");
    }

    // -------------------- Acciones --------------------

    private void ShowExportMenu()
    {
        GenericMenu m = new GenericMenu();
        m.AddItem(new GUIContent("Exportar TXT"), false, ExportListToTxt);
        m.AddItem(new GUIContent("Exportar CSV"), false, ExportListToCsv);
        m.ShowAsContext();
    }

    private void ExportListToTxt()
    {
        string defaultName = "activos_no_usados.txt";
        string folderPath = Application.dataPath + "/" + rootFolder.Substring("Assets/".Length);
        string savePath = EditorUtility.SaveFilePanel("Guardar lista (TXT)", folderPath, defaultName, "txt");
        if (string.IsNullOrEmpty(savePath)) return;

        using (var w = new StreamWriter(savePath))
        {
            w.WriteLine($"Carpeta raíz: {rootFolder}");
            w.WriteLine($"Fecha: {System.DateTime.Now}");
            w.WriteLine();

            WriteSection(w, "=== Prefabs no usados ===", unusedPrefabs);
            WriteSection(w, "=== Modelos no usados ===", unusedModels);
            WriteSection(w, "=== Texturas no usadas ===", unusedTextures);
            WriteSection(w, "=== Materiales no usados ===", unusedMaterials);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Exportado", $"Lista guardada:\n{savePath}", "OK");
        EditorUtility.RevealInFinder(savePath);
    }

    private void ExportListToCsv()
    {
        string defaultName = "activos_no_usados.csv";
        string folderPath = Application.dataPath + "/" + rootFolder.Substring("Assets/".Length);
        string savePath = EditorUtility.SaveFilePanel("Guardar lista (CSV)", folderPath, defaultName, "csv");
        if (string.IsNullOrEmpty(savePath)) return;

        using (var w = new StreamWriter(savePath))
        {
            w.WriteLine("Category,Path,FileName,Extension");
            void writeRows(string cat, IEnumerable<string> items)
            {
                foreach (var p in items)
                {
                    var fn = Path.GetFileName(p);
                    var ext = Path.GetExtension(p);
                    w.WriteLine($"\"{cat}\",\"{p}\",\"{fn}\",\"{ext}\"");
                }
            }

            writeRows("Prefab", unusedPrefabs);
            writeRows("Model", unusedModels);
            writeRows("Texture", unusedTextures);
            writeRows("Material", unusedMaterials);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Exportado", $"CSV guardado:\n{savePath}", "OK");
        EditorUtility.RevealInFinder(savePath);
    }

    private void WriteSection(StreamWriter w, string title, List<string> rows)
    {
        w.WriteLine(title);
        foreach (var p in rows) w.WriteLine(p);
        w.WriteLine();
    }

    private void MoveSelectionToEliminar()
    {
        if (selected.Count == 0) return;
        if (showConfirmations && !EditorUtility.DisplayDialog("Mover selección",
            $"¿Mover {selected.Count} activo(s) a «{eliminarFolderName}» dentro de «{rootFolder}»?",
            "Mover", "Cancelar"))
            return;

        MovePathsToEliminar(selected.ToArray());
    }

    private void MovePathsToEliminar(IEnumerable<string> paths)
    {
        string eliminarFolder = $"{rootFolder}/{eliminarFolderName}";
        if (!AssetDatabase.IsValidFolder(eliminarFolder))
            AssetDatabase.CreateFolder(rootFolder, eliminarFolderName);

        int moved = 0;
        foreach (var a in paths)
        {
            if (IsIgnored(a)) continue;
            string fn = Path.GetFileName(a);
            string dest = UniqueDestination(eliminarFolder, fn);
            string err = AssetDatabase.MoveAsset(a, dest);
            if (string.IsNullOrEmpty(err)) moved++;
            else Debug.LogWarning($"No se pudo mover '{a}' → '{dest}': {err}");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Movimiento completado", $"Movidos {moved} activo(s) a «{eliminarFolder}».", "OK");
        RemoveFromResults(paths);
    }

    private string UniqueDestination(string folder, string fileName)
    {
        string dest = $"{folder}/{fileName}";
        if (!AssetDatabase.LoadAssetAtPath<Object>(dest)) return dest;

        string name = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);
        int i = 1;
        while (AssetDatabase.LoadAssetAtPath<Object>($"{folder}/{name}_{i}{ext}") != null) i++;
        return $"{folder}/{name}_{i}{ext}";
    }

    private void DeleteSelection()
    {
        if (selected.Count == 0) return;
        if (showConfirmations && !EditorUtility.DisplayDialog("Eliminar selección",
            $"Esto eliminará {selected.Count} activo(s) del proyecto.\n\nEsta acción no se puede deshacer.\n\n¿Continuar?",
            "Eliminar", "Cancelar"))
            return;

        DeletePaths(selected.ToArray());
    }

    private void DeletePaths(IEnumerable<string> paths)
    {
        int deleted = 0;
        foreach (var a in paths)
        {
            if (IsIgnored(a)) continue;
            if (AssetDatabase.DeleteAsset(a))
                deleted++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Eliminación completada", $"Eliminados {deleted} activo(s).", "OK");
        RemoveFromResults(paths);
    }

    private void RemoveFromResults(IEnumerable<string> paths)
    {
        var set = new HashSet<string>(paths);
        unusedPrefabs.RemoveAll(p => set.Contains(p));
        unusedModels.RemoveAll(p => set.Contains(p));
        unusedTextures.RemoveAll(p => set.Contains(p));
        unusedMaterials.RemoveAll(p => set.Contains(p));
        unusedAssets.RemoveAll(p => set.Contains(p));
        foreach (var p in set) selected.Remove(p);
        Repaint();
    }

    private void SelectAll()
    {
        selected.Clear();
        foreach (var p in unusedAssets)
            if (PassesSearch(p)) selected.Add(p);
    }

    private void ClearSelection()
    {
        selected.Clear();
    }

    private void AddToSelection(IEnumerable<string> paths)
    {
        foreach (var p in paths) selected.Add(p);
    }

    private void CopyPathsToClipboard(IEnumerable<string> paths)
    {
        EditorGUIUtility.systemCopyBuffer = string.Join("\n", paths);
    }

    private void CopyPathsToClipboard(List<string> paths)
    {
        CopyPathsToClipboard((IEnumerable<string>)paths);
    }

    private void Ping(string assetPath)
    {
        var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (obj != null)
        {
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }
    }

    private void Reveal(string assetPath)
    {
        string abs = Path.GetFullPath(assetPath);
        EditorUtility.RevealInFinder(abs);
    }
}
