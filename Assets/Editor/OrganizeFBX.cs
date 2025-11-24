using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class OrganizeFBX : EditorWindow
{
    private string rootFolder = "Assets";
    private List<string> toProcess = new List<string>();
    private List<string> blacklisted = new List<string>();
    private List<string> processed = new List<string>();

    private Vector2 scrollA, scrollB, scrollC;
    private Rect rectA, rectB;
    private DragInfo dragging;
    private bool dragActive;

    private class DragInfo
    {
        public string path;
        public string fromList; 
    }

    [MenuItem("Tools/Limpiar y Organizar/Organizar FBX")]
    public static void ShowWindow()
    {
        var w = GetWindow<OrganizeFBX>("Organize FBX");
        w.minSize = new Vector2(700, 450);
    }

    private void OnGUI()
    {
        GUILayout.Label("Configuración de carpeta FBX", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        rootFolder = EditorGUILayout.TextField("Carpeta raú—", rootFolder);
        if (GUILayout.Button("...", GUILayout.MaxWidth(30)))
        {
            string abs = EditorUtility.OpenFolderPanel("Selecciona carpeta raú—", Application.dataPath, "");
            if (!string.IsNullOrEmpty(abs) && abs.StartsWith(Application.dataPath))
                rootFolder = "Assets" + abs.Substring(Application.dataPath.Length).Replace("\\", "/");
            else
                EditorUtility.DisplayDialog("Error", "La carpeta debe estar dentro de Assets.", "OK");
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        bool hasFBX = toProcess.Count > 0;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan FBX")) ScanFBX();
        GUI.enabled = hasFBX;
        if (GUILayout.Button("Apply Settings")) ApplySettings();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Label($"FBX a procesar ({toProcess.Count})", EditorStyles.boldLabel);
        rectA = GUILayoutUtility.GetRect(
            GUIContent.none, GUIStyle.none,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)
        );
        float itemH = EditorGUIUtility.singleLineHeight;
        Rect contentA = new Rect(0, 0, rectA.width, toProcess.Count * itemH);
        scrollA = GUI.BeginScrollView(rectA, scrollA, contentA);
        for (int i = 0; i < toProcess.Count; i++)
        {
            Rect local = new Rect(0, i * itemH, rectA.width, itemH);
            GUI.Label(local, Path.GetFileName(toProcess[i]), EditorStyles.label);
        }
        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Label($"Lista Negra ({blacklisted.Count})", EditorStyles.boldLabel);
        rectB = GUILayoutUtility.GetRect(
            GUIContent.none, GUIStyle.none,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)
        );
        Rect contentB = new Rect(0, 0, rectB.width, blacklisted.Count * itemH);
        scrollB = GUI.BeginScrollView(rectB, scrollB, contentB);
        for (int i = 0; i < blacklisted.Count; i++)
        {
            Rect local = new Rect(0, i * itemH, rectB.width, itemH);
            GUI.Label(local, Path.GetFileName(blacklisted[i]), EditorStyles.label);
        }
        GUI.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Label($"FBX procesados ({processed.Count})", EditorStyles.boldLabel);
        scrollC = EditorGUILayout.BeginScrollView(scrollC, GUILayout.ExpandHeight(true));
        foreach (var p in processed)
            EditorGUILayout.LabelField(Path.GetFileName(p), EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        HandleDragAndDrop(itemH);
    }

    private void HandleDragAndDrop(float itemHeight)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !dragActive)
        {
            for (int i = 0; i < toProcess.Count; i++)
            {
                Rect screen = new Rect(
                    rectA.x,
                    rectA.y + i * itemHeight - scrollA.y,
                    rectA.width,
                    itemHeight
                );
                if (screen.Contains(e.mousePosition))
                {
                    dragging = new DragInfo { path = toProcess[i], fromList = "toProcess" };
                    dragActive = true;
                    e.Use();
                    return;
                }
            }
            for (int i = 0; i < blacklisted.Count; i++)
            {
                Rect screen = new Rect(
                    rectB.x,
                    rectB.y + i * itemHeight - scrollB.y,
                    rectB.width,
                    itemHeight
                );
                if (screen.Contains(e.mousePosition))
                {
                    dragging = new DragInfo { path = blacklisted[i], fromList = "blacklist" };
                    dragActive = true;
                    e.Use();
                    return;
                }
            }
        }

        if (dragActive && e.type == EventType.MouseDrag)
        {
            e.Use();
        }

        if (dragActive && e.type == EventType.MouseUp)
        {
            if (dragging.fromList == "toProcess" && rectB.Contains(e.mousePosition))
            {
                toProcess.Remove(dragging.path);
                blacklisted.Add(dragging.path);
            }
            else if (dragging.fromList == "blacklist" && rectA.Contains(e.mousePosition))
            {
                blacklisted.Remove(dragging.path);
                toProcess.Add(dragging.path);
            }
            dragging = null;
            dragActive = false;
            Repaint();
            e.Use();
        }
    }

    private void ScanFBX()
    {
        toProcess.Clear();
        processed.Clear();

        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            EditorUtility.DisplayDialog("Error", $"La carpeta '{rootFolder}' no existe.", "OK");
            return;
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { rootFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetExtension(path).ToLower() != ".fbx") continue;
            if (blacklisted.Contains(path)) continue;

            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;

            bool needsFix =
                imp.materialImportMode != ModelImporterMaterialImportMode.None ||
                imp.importBlendShapes ||
                imp.importCameras ||
                imp.importLights ||
                imp.importAnimation ||
                imp.animationType != ModelImporterAnimationType.None ||
                HasImportConstraints(imp);

            if (needsFix) toProcess.Add(path);
            else processed.Add(path);
        }
    }

    private void ApplySettings()
    {
        foreach (var path in toProcess)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;

            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.importBlendShapes = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            SetImportConstraints(imp, false);

            imp.SaveAndReimport();
        }
        ScanFBX();
    }

    private bool HasImportConstraints(ModelImporter im)
    {
        var pi = typeof(ModelImporter).GetProperty("importConstraints");
        return pi != null && pi.PropertyType == typeof(bool) && (bool)pi.GetValue(im);
    }

    private void SetImportConstraints(ModelImporter im, bool val)
    {
        var pi = typeof(ModelImporter).GetProperty("importConstraints");
        if (pi != null && pi.PropertyType == typeof(bool))
            pi.SetValue(im, val);
    }
}
