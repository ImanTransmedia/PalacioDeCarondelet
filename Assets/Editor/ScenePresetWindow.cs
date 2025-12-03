#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class ScenePreset
{
    public SceneAsset mainScene;
    public List<SceneAsset> subScenes = new List<SceneAsset>();
}

public class ScenePresetDatabase : ScriptableObject
{
    public List<ScenePreset> presets = new List<ScenePreset>();
}

public class ScenePresetWindow : EditorWindow
{
    private const string DatabaseFolder = "Assets/Editor/ScenePresets";
    private const string DatabasePath = DatabaseFolder + "/ScenePresetDatabase.asset";

    private ScenePresetDatabase database;
    private int selectedPresetIndex = -1;

    private Vector2 leftScroll;
    private Vector2 rightScroll;

    // Para agregar subescenas
    private SceneAsset newSubSceneToAdd;

    [MenuItem("Tools/Scene Presets")]
    public static void OpenWindow()
    {
        var window = GetWindow<ScenePresetWindow>("Scene Presets");
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrCreateDatabase();
    }

    private void LoadOrCreateDatabase()
    {
        database = AssetDatabase.LoadAssetAtPath<ScenePresetDatabase>(DatabasePath);

        if (database == null)
        {
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
            }

            database = ScriptableObject.CreateInstance<ScenePresetDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void OnGUI()
    {
        if (database == null)
        {
            LoadOrCreateDatabase();
            if (database == null)
            {
                EditorGUILayout.HelpBox("No se pudo crear/cargar la base de datos de presets.", MessageType.Error);
                return;
            }
        }

        EditorGUILayout.BeginHorizontal();

        DrawPresetList();   // Columna izquierda
        DrawPresetDetail(); // Columna derecha

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawBottomButtons();
    }

    private void DrawPresetList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));

        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

        if (database.presets.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay presets. Haz clic en 'Nuevo Preset'.", MessageType.Info);
        }

        for (int i = 0; i < database.presets.Count; i++)
        {
            var preset = database.presets[i];
            string label = preset.mainScene != null ? preset.mainScene.name : "(sin escena main)";

            GUIStyle style = new GUIStyle(EditorStyles.miniButton);
            if (i == selectedPresetIndex)
                style.normal = style.active;

            if (GUILayout.Button(label, style))
            {
                selectedPresetIndex = i;
                GUI.FocusControl(null);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("Nuevo Preset"))
        {
            database.presets.Add(new ScenePreset());
            selectedPresetIndex = database.presets.Count - 1;
            MarkDatabaseDirty();
        }

        if (selectedPresetIndex >= 0 && selectedPresetIndex < database.presets.Count)
        {
            if (GUILayout.Button("Eliminar Preset"))
            {
                if (EditorUtility.DisplayDialog("Eliminar Preset",
                    "Seguro que quieres eliminar este preset?", "S", "No"))
                {
                    database.presets.RemoveAt(selectedPresetIndex);
                    selectedPresetIndex = -1;
                    MarkDatabaseDirty();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPresetDetail()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Detalle del Preset", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (selectedPresetIndex < 0 || selectedPresetIndex >= database.presets.Count)
        {
            EditorGUILayout.HelpBox("Selecciona o crea un preset en la lista de la izquierda.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        ScenePreset preset = database.presets[selectedPresetIndex];

        // === BOT�N GRANDE PARA CARGAR EL PRESET ===
        GUI.enabled = preset.mainScene != null;
        GUIStyle bigButtonStyle = new GUIStyle(GUI.skin.button);
        bigButtonStyle.fontSize = 14;
        bigButtonStyle.fontStyle = FontStyle.Bold;
        bigButtonStyle.fixedHeight = 40;

        if (GUILayout.Button("CARGAR MAIN + SUBSCENES", bigButtonStyle, GUILayout.ExpandWidth(true)))
        {
            LoadSelectedPresetScenes();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Escena principal
        EditorGUI.BeginChangeCheck();
        preset.mainScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Main Scene",
            preset.mainScene,
            typeof(SceneAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            MarkDatabaseDirty();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sub Scenes", EditorStyles.boldLabel);

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        // Lista de subescenas existentes
        for (int i = 0; i < preset.subScenes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            preset.subScenes[i] = (SceneAsset)EditorGUILayout.ObjectField(
                preset.subScenes[i],
                typeof(SceneAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                MarkDatabaseDirty();
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                preset.subScenes.RemoveAt(i);
                MarkDatabaseDirty();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // A�adir nueva subescena
        EditorGUILayout.BeginHorizontal();
        newSubSceneToAdd = (SceneAsset)EditorGUILayout.ObjectField(
            "Agregar Sub Scene",
            newSubSceneToAdd,
            typeof(SceneAsset),
            false
        );

        if (GUILayout.Button("Agregar", GUILayout.Width(70)))
        {
            if (newSubSceneToAdd != null)
            {
                if (!preset.subScenes.Contains(newSubSceneToAdd))
                {
                    preset.subScenes.Add(newSubSceneToAdd);
                    MarkDatabaseDirty();
                }
                newSubSceneToAdd = null;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save", GUILayout.Width(80))
        )
        {
            SaveDatabase();
        }

        GUI.enabled = selectedPresetIndex >= 0 && selectedPresetIndex < database.presets.Count;
        if (GUILayout.Button("Load", GUILayout.Width(80)))
        {
            LoadSelectedPresetScenes();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void LoadSelectedPresetScenes()
    {
        if (selectedPresetIndex < 0 || selectedPresetIndex >= database.presets.Count)
            return;

        ScenePreset preset = database.presets[selectedPresetIndex];

        if (preset.mainScene == null)
        {
            EditorUtility.DisplayDialog("Error", "El preset no tiene main scene asignada.", "Ok");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string mainPath = AssetDatabase.GetAssetPath(preset.mainScene);
        if (string.IsNullOrEmpty(mainPath))
        {
            EditorUtility.DisplayDialog("Error", "No se encontr la ruta de la main scene.", "Ok");
            return;
        }

        EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);

        foreach (var subScene in preset.subScenes)
        {
            if (subScene == null) continue;

            string subPath = AssetDatabase.GetAssetPath(subScene);
            if (string.IsNullOrEmpty(subPath)) continue;

            EditorSceneManager.OpenScene(subPath, OpenSceneMode.Additive);
        }
    }

    private void SaveDatabase()
    {
        MarkDatabaseDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Scene Presets", "Presets guardados.", "Ok");
    }

    private void MarkDatabaseDirty()
    {
        if (database != null)
        {
            EditorUtility.SetDirty(database);
        }
    }
}
#endif
