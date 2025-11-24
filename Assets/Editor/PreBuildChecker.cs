using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PreBuildChecker : EditorWindow
{
    private Vector2 scroll;
    private List<Issue> issues = new List<Issue>();
    private bool checkMipmaps = true;

    private class Issue
    {
        public string path;
        public string description;
        public System.Action fixAction;
    }

    [MenuItem("Tools/Game Build/Pre-Build Checker")]
    public static void ShowWindow()
    {
        var window = GetWindow<PreBuildChecker>("Pre-Build Checker");
        window.minSize = new Vector2(500, 450);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        GUILayout.Label("PRE-BUILD CHECKER", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Ejecuta verificaciones antes del build.", MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Selecciona las verificaciones:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        checkMipmaps = EditorGUILayout.Toggle("Revisar mipmaps en Lightmaps", checkMipmaps);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Ejecutar Verificación", GUILayout.Height(30)))
        {
            RunChecks();
        }

        EditorGUILayout.Space(10);
        if (issues.Count > 0)
        {
            if (GUILayout.Button("Corregir Todos los Problemas", GUILayout.Height(25)))
            {
                FixAllIssues();
            }
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Resultados de la verificación:", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(240));

        if (issues.Count == 0)
        {
            GUILayout.Label("Sin problemas detectados.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(issue.description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(issue.path, EditorStyles.miniLabel);

                EditorGUILayout.Space(4);
                if (issue.fixAction != null)
                {
                    if (GUILayout.Button("Corregir este problema", GUILayout.Height(22)))
                    {
                        issue.fixAction.Invoke();
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Corregido", $"Se corrigió:\n{issue.path}", "OK");
                        RunChecks();
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunChecks()
    {
        issues.Clear();

        if (checkMipmaps)
            CheckLightmapMipmaps();

        if (issues.Count == 0)
            Debug.Log("PreBuild Check completado: sin problemas.");
        else
            Debug.LogWarning($"PreBuild Check detectó {issues.Count} problema(s).");

        Repaint();
    }

    private void CheckLightmapMipmaps()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D Lightmap");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            if (path.ToLower().Contains("lightmap") && importer.mipmapEnabled)
            {
                var issue = new Issue()
                {
                    path = path,
                    description = "Lightmap con mipmaps activados",
                    fixAction = () => FixLightmapMipmaps(path)
                };
                issues.Add(issue);
            }
        }
    }

    private void FixLightmapMipmaps(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            Debug.Log($"Mipmaps desactivados en Lightmap: {path}");
        }
    }

    private void FixAllIssues()
    {
        int fixedCount = 0;

        foreach (var issue in issues)
        {
            issue.fixAction?.Invoke();
            fixedCount++;
        }

        issues.Clear();
        AssetDatabase.Refresh();
        RunChecks();

        EditorUtility.DisplayDialog("Pre-Build Checker", $"Corrección completada.\nProblemas corregidos: {fixedCount}", "OK");
    }
}
