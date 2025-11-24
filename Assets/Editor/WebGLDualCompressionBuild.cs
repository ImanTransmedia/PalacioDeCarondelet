#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class WebGLMultiBuildWindow : EditorWindow
{
    const string PREF_OUT_DIR = "WebGLMultiBuild_OutDir";
    const string PREF_SCENE_ENABLE_PREFIX = "WebGLMultiBuild_Scene_"; 
    const string PREF_COMPRESS_PREFIX = "WebGLMultiBuild_Compress_";   

    Vector2 _scroll;
    string _outputDir;
    Dictionary<string, bool> _sceneEnable = new Dictionary<string, bool>(); 
    readonly Dictionary<WebGLTextureSubtarget, bool> _compressions = new Dictionary<WebGLTextureSubtarget, bool>()
    {
        { WebGLTextureSubtarget.DXT,  true },
        { WebGLTextureSubtarget.ASTC, true },
        { WebGLTextureSubtarget.ETC2, true }
    };

    [MenuItem("Tools/Game Build/WebGL Multi Build Redireccionador")]
    public static void ShowWindow()
    {
        var win = GetWindow<WebGLMultiBuildWindow>("WebGL Multi Build");
        win.minSize = new Vector2(560, 520);
        win.RefreshScenesFromBuildSettings();
        win.LoadPrefs();
        win.Focus();
    }

    void OnEnable()
    {
        RefreshScenesFromBuildSettings();
        LoadPrefs();
    }

    void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawOutputFolder();
        }

        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawScenesList();
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawCompressionOptions();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Abrir carpeta de salida"))
            {
                if (Directory.Exists(_outputDir))
                    EditorUtility.RevealInFinder(_outputDir);
                else
                    EditorUtility.DisplayDialog("Carpeta no encontrada", "La carpeta de salida aún no existe.", "Ok");
            }

            GUILayout.FlexibleSpace();

            GUI.enabled = IsBuildEnabled();
            var buildButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedHeight = 34 };
            if (GUILayout.Button("Construir WebGL (múltiple)", buildButtonStyle, GUILayout.Width(260)))
            {
                BuildAllSelected();
            }
            GUI.enabled = true;
        }

        GUILayout.Space(6);
        DrawFooterHelp();
    }


    void DrawHeader()
    {
        var title = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft
        };

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("WebGL Multi Build", title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Actualizar escenas", GUILayout.Width(150)))
            {
                RefreshScenesFromBuildSettings(true);
            }
        }

        var subt = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
        EditorGUILayout.LabelField(
            "<b>Consejo:</b> Las escenas se leen desde <i>File ▸ Build Settings…</i>. " +
            "Aquí activas cuáles incluir en la build sin modificar tu Build Settings.",
            subt);
    }

    void DrawOutputFolder()
    {
        var label = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("Carpeta de salida", label);

        using (new EditorGUILayout.HorizontalScope())
        {
            _outputDir = EditorGUILayout.TextField(_outputDir);
            if (GUILayout.Button("Elegir...", GUILayout.Width(90)))
            {
                string picked = EditorUtility.OpenFolderPanel("Elige carpeta raíz para builds WebGL", _outputDir, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    _outputDir = picked.Replace('/', Path.DirectorySeparatorChar);
                    SavePrefs();
                }
            }
        }

        var hint = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        EditorGUILayout.LabelField(
            "Se crearán subcarpetas por compresión (por ej. DXT/ ASTC/ ETC2). " +
            "También se generará un index.html redireccionador en la raíz.", hint);
    }

    void DrawScenesList()
    {
        var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("Escenas (desde Build Settings)", header);

        var scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            EditorGUILayout.HelpBox("No hay escenas en Build Settings. Ábrelo y añade escenas.", MessageType.Warning);
            if (GUILayout.Button("Abrir Build Settings"))
                EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Seleccionar todo", GUILayout.Width(140)))
            {
                foreach (var s in scenes) _sceneEnable[s.path] = true;
                SavePrefs();
            }
            if (GUILayout.Button("Deseleccionar todo", GUILayout.Width(140)))
            {
                foreach (var s in scenes) _sceneEnable[s.path] = false;
                SavePrefs();
            }
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(2);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            foreach (var s in scenes)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool current = _sceneEnable.ContainsKey(s.path) ? _sceneEnable[s.path] : s.enabled;
                    bool next = EditorGUILayout.ToggleLeft(new GUIContent(Path.GetFileNameWithoutExtension(s.path), s.path), current);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(RelativeProjectPath(s.path), EditorStyles.miniLabel, GUILayout.MaxWidth(320));

                    if (next != current)
                    {
                        _sceneEnable[s.path] = next;
                        SavePrefs();
                    }
                }
            }
        }

        // Nota
        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox("Esta selección no cambia tus ‘Enabled’ reales de Build Settings. Solo afecta a esta ventana.", MessageType.Info);
    }

    void DrawCompressionOptions()
    {
        var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUILayout.Label("Compresiones de Texturas WebGL", header);

        EditorGUILayout.LabelField("Marca qué variantes quieres construir:", EditorStyles.miniLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawCompressionToggle(WebGLTextureSubtarget.DXT, "DXT (Desktop)");
            DrawCompressionToggle(WebGLTextureSubtarget.ASTC, "ASTC (Móviles modernos)");
            DrawCompressionToggle(WebGLTextureSubtarget.ETC2, "ETC2 (Compatibilidad móvil amplia)");
        }
    }

    void DrawCompressionToggle(WebGLTextureSubtarget sub, string label)
    {
        bool val = _compressions[sub];
        bool next = EditorGUILayout.ToggleLeft(label, val);
        if (next != val)
        {
            _compressions[sub] = next;
            EditorPrefs.SetBool(PREF_COMPRESS_PREFIX + sub.ToString(), next);
        }
    }

    void DrawFooterHelp()
    {
        var help = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { richText = true };
        EditorGUILayout.LabelField(
            "Al finalizar, se genera un <b>index.html</b> en la carpeta raíz que redirige a la variante correcta:\n" +
            "• Si es escritorio  DXT\n" +
            "• Si es dispositivo móvil con soporte ASTC  ASTC\n" +
            "• Si es móvil sin ASTC  ETC2", help);
    }


    bool IsBuildEnabled()
    {
        if (string.IsNullOrEmpty(_outputDir)) return false;
        if (!_compressions.Values.Any(v => v)) return false;
        if (GetSelectedScenePaths().Length == 0) return false;
        return true;
    }

    void BuildAllSelected()
    {
        try
        {
            Directory.CreateDirectory(_outputDir);

            var scenes = GetSelectedScenePaths();
            var variants = _compressions.Where(kv => kv.Value).Select(kv => kv.Key).ToList();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Sin escenas", "Debes seleccionar al menos una escena.", "Ok");
                return;
            }
            if (variants.Count == 0)
            {
                EditorUtility.DisplayDialog("Sin variantes", "Debes seleccionar al menos una compresión.", "Ok");
                return;
            }

            int total = variants.Count;
            int step = 0;

            foreach (var sub in variants)
            {
                step++;
                string subFolder = Path.Combine(_outputDir, sub.ToString());
                Directory.CreateDirectory(subFolder);

                EditorUtility.DisplayProgressBar(
                    "Construyendo WebGL",
                    $"Variante {sub} ({step}/{total})",
                    (float)step / (float)total);

                var previousSub = EditorUserBuildSettings.webGLBuildSubtarget;
                try
                {
                    EditorUserBuildSettings.webGLBuildSubtarget = sub;

                    var opts = new BuildPlayerOptions
                    {
                        scenes = scenes,
                        locationPathName = subFolder,
                        target = BuildTarget.WebGL,
                        options = BuildOptions.None
                    };

                    var report = BuildPipeline.BuildPlayer(opts);
#if UNITY_2020_1_OR_NEWER
                    if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    {
                        throw new Exception($"Falló la build {sub}: {report.summary.result}");
                    }
#endif
                    Debug.Log($"✔ WebGL {sub} build completa en: {subFolder}");
                }
                finally
                {
                    EditorUserBuildSettings.webGLBuildSubtarget = previousSub;
                }
            }

            GenerateRedirectIndex(_outputDir);

            EditorUtility.ClearProgressBar();
            EditorUtility.RevealInFinder(_outputDir);
            EditorUtility.DisplayDialog("Listo", "Todas las builds WebGL finalizaron correctamente.", "Ok");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("Error en build", ex.Message, "Ok");
        }
    }

    string[] GetSelectedScenePaths()
    {
        var enabled = _sceneEnable.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        var order = EditorBuildSettings.scenes.Select(s => s.path).ToList();
        var ordered = enabled.OrderBy(p => order.IndexOf(p)).ToArray();
        return ordered;
    }


    static void GenerateRedirectIndex(string buildRoot)
    {
        string indexPath = Path.Combine(buildRoot, "index.html");
        string html = @"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Redirigiendo…</title>
<style>
html,body{height:100%;margin:0;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial}
.center{height:100%;display:flex;align-items:center;justify-content:center;flex-direction:column;gap:.5rem}
.spinner{width:28px;height:28px;border:3px solid #ddd;border-top-color:#555;border-radius:50%;animation:spin 1s linear infinite}
@keyframes spin{to{transform:rotate(360deg)}}
small{opacity:.7}
</style>
<script>
function getBuildFolder() {
  var ua = navigator.userAgent || '';
  var isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua);

  var canvas = document.createElement('canvas');
  var gl = canvas.getContext('webgl2') || canvas.getContext('webgl');
  var astcSupported = false;
  if (gl) {
    astcSupported = !!(gl.getExtension('WEBGL_compressed_texture_astc') ||
                       gl.getExtension('WEBKIT_WEBGL_compressed_texture_astc') ||
                       gl.getExtension('MOZ_WEBGL_compressed_texture_astc'));
  }

  if (isMobile) {
    if (astcSupported) return 'ASTC';
    return 'ETC2';
  } else {
    return 'DXT';
  }
}
function redirectToBuild() {
  var folder = getBuildFolder();
  var redirectUrl = folder + '/index.html';
  window.location.replace(redirectUrl);
}
window.onload = redirectToBuild;
</script>
</head>
<body>
<div class=""center"">
  <div class=""spinner""></div>
  <div>Detectando la mejor versión…</div>
  <small>Si no ocurre nada, abre manualmente DXT/ASTC/ETC2.</small>
</div>
</body>
</html>";
        File.WriteAllText(indexPath, html);
        Debug.Log("index.html redireccionador generado en: " + indexPath);
    }


    void RefreshScenesFromBuildSettings(bool notify = false)
    {
        var scenes = EditorBuildSettings.scenes;
        var toRemove = _sceneEnable.Keys.Where(k => !scenes.Any(s => s.path == k)).ToList();
        foreach (var k in toRemove) _sceneEnable.Remove(k);
        foreach (var s in scenes)
        {
            if (!_sceneEnable.ContainsKey(s.path))
                _sceneEnable[s.path] = s.enabled; 
        }
        if (notify) ShowNotification(new GUIContent("Escenas actualizadas desde Build Settings"));
    }

    string RelativeProjectPath(string absoluteOrProjectPath)
    {
        if (absoluteOrProjectPath.StartsWith("Assets")) return absoluteOrProjectPath;
        string proj = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
        string full = Path.GetFullPath(absoluteOrProjectPath).Replace("\\", "/");
        if (full.StartsWith(proj)) return full.Substring(proj.Length + 1);
        return absoluteOrProjectPath;
    }

    void LoadPrefs()
    {
        _outputDir = EditorPrefs.GetString(PREF_OUT_DIR,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WebGLBuilds"));

        foreach (var key in _compressions.Keys.ToList())
        {
            string prefKey = PREF_COMPRESS_PREFIX + key.ToString();
            if (EditorPrefs.HasKey(prefKey))
                _compressions[key] = EditorPrefs.GetBool(prefKey, _compressions[key]);
        }

        foreach (var s in EditorBuildSettings.scenes)
        {
            string k = PREF_SCENE_ENABLE_PREFIX + s.path;
            if (EditorPrefs.HasKey(k))
                _sceneEnable[s.path] = EditorPrefs.GetBool(k, s.enabled);
        }
    }

    void SavePrefs()
    {
        EditorPrefs.SetString(PREF_OUT_DIR, _outputDir ?? "");

        foreach (var kv in _compressions)
            EditorPrefs.SetBool(PREF_COMPRESS_PREFIX + kv.Key.ToString(), kv.Value);

        foreach (var kv in _sceneEnable)
            EditorPrefs.SetBool(PREF_SCENE_ENABLE_PREFIX + kv.Key, kv.Value);
    }
}
#endif
