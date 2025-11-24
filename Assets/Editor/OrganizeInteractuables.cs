using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OrganizeInteractuables : EditorWindow
{
    public static GeneralConfig CurrentConfig { get; private set; }


    private URLSalon _salonSeleccionado = URLSalon.SalonAmarillo;
    private GeneralConfig _config = new GeneralConfig();
    private List<InteractableEntry> _scanTemporal = new List<InteractableEntry>();
    private Vector2 _scroll;
    private string _rutaJsonActual;

    [MenuItem("Tools/Configuraciones/Exportar Interactuables")]
    public static void ShowWindow()
    {
        var win = GetWindow<OrganizeInteractuables>("Interactables Exporter");
        win.minSize = new Vector2(720, 480);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Exportar Interactuables a JSON por Salón", EditorStyles.boldLabel);

        _salonSeleccionado = (URLSalon)EditorGUILayout.EnumPopup(new GUIContent("Salón de la escena"), _salonSeleccionado);

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Escanear escenas abiertas", GUILayout.Height(30)))
            {
                EscanearEscenasAbiertas();
            }

            if (GUILayout.Button("Agregar/Reemplazar salón en JSON", GUILayout.Height(30)))
            {
                AgregarOReemplazarSalonEnConfig();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Cargar JSON...", GUILayout.Height(24)))
            {
                CargarJson();
            }

            GUI.enabled = _config.salones.Count > 0;
            if (GUILayout.Button("Guardar JSON como...", GUILayout.Height(24)))
            {
                GuardarJsonComo();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
        DibujarTablaScanTemporal();

        EditorGUILayout.Space();
        DibujarResumenConfigActual();
    }

    // ---------- UI Helpers ----------

    private void DibujarTablaScanTemporal()
    {
        EditorGUILayout.LabelField("Resultados del escaneo (editables antes de guardar):", EditorStyles.boldLabel);
        if (_scanTemporal.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay objetos escaneados aún. Presiona “Escanear escenas abiertas”.", MessageType.Info);
            return;
        }

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(260)))
        {
            _scroll = scroll.scrollPosition;

            // Encabezado
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Identifier (editable)", GUILayout.Width(220));
                GUILayout.Label("Tipo", GUILayout.Width(170));
                GUILayout.Label("Escena", GUILayout.Width(160));
                GUILayout.Label("Path (solo lectura)");
            }

            EditorGUILayout.Space(2);

            for (int i = 0; i < _scanTemporal.Count; i++)
            {
                var e = _scanTemporal[i];
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    e.identifier = EditorGUILayout.TextField(e.identifier, GUILayout.Width(220));
                    EditorGUILayout.LabelField(e.type, GUILayout.Width(170));
                    EditorGUILayout.LabelField(e.scene, GUILayout.Width(160));
                }
            }
        }
    }

    private void DibujarResumenConfigActual()
    {
        EditorGUILayout.LabelField("Resumen del JSON en memoria:", EditorStyles.boldLabel);
        if (_config.salones.Count == 0)
        {
            EditorGUILayout.HelpBox("Aún no hay datos en memoria. Carga un JSON o agrega uno escaneando y guardando un salón.", MessageType.None);
            return;
        }

        foreach (var s in _config.salones)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Salón: {s.salon}  |  Objetos: {s.objetos.Count}");
            }
        }
    }

    // ---------- Core ----------

    private void EscanearEscenasAbiertas()
    {
        _scanTemporal.Clear();

        int count = SceneManager.sceneCount;
        for (int i = 0; i < count; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var comp in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comp == null) continue;
                    var type = comp.GetType();
                    try
                    {
                        if (type.Name == "textureDisplay")      
                            _scanTemporal.Add(ConstruirDesdeTextureDisplay(comp, scene));
                        else if (type.Name == "paintingDisplay")  
                            _scanTemporal.Add(ConstruirDesdePaintingDisplay(comp, scene));
                        else if (type.Name == "ItemDisplay")     
                            _scanTemporal.AddRange(ConstruirDesdeItemDisplay(comp, scene));
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        // Orden amigable
        _scanTemporal = _scanTemporal
            .OrderBy(e => e.type)
            .ThenBy(e => e.scene)
            .ToList();

        Repaint();
        Debug.Log($"[Exporter] Escaneo completado. Encontrados: {_scanTemporal.Count} objetos.");
    }

    private void AgregarOReemplazarSalonEnConfig()
    {
        var nombreSalon = _salonSeleccionado.ToString();

        // Reemplazar entrada del salón si ya existe
        var existente = _config.salones.FirstOrDefault(s => s.salon == nombreSalon);
        if (existente != null)
        {
            existente.objetos = _scanTemporal.ToList();
        }
        else
        {
            _config.salones.Add(new SalonConfig
            {
                salon = nombreSalon,
                objetos = _scanTemporal.ToList()
            });
            CurrentConfig = _config;
        }

        Debug.Log($"[Exporter] Salón '{nombreSalon}' {(existente != null ? "actualizado" : "agregado")} en el JSON en memoria.");
        Repaint();
    }

    private void CargarJson()
    {
        var path = EditorUtility.OpenFilePanel("Cargar JSON de configuración", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var txt = File.ReadAllText(path, Encoding.UTF8);
            _config = JsonUtility.FromJson<GeneralConfig>(txt) ?? new GeneralConfig();
            CurrentConfig = _config;
            _rutaJsonActual = path;
            Debug.Log($"[Exporter] JSON cargado: {_rutaJsonActual}");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "No se pudo leer el JSON: " + ex.Message, "Ok");
        }
    }

    private void GuardarJsonComo()
    {
        var path = EditorUtility.SaveFilePanel("Guardar JSON de configuración", Application.dataPath, "configInteractuables.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var json = JsonUtility.ToJson(_config, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            _rutaJsonActual = path;
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[Exporter] JSON guardado en: {_rutaJsonActual}");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "No se pudo guardar el JSON: " + ex.Message, "Ok");
        }
    }

    // ---------- Builders por tipo ----------

    private InteractableEntry ConstruirDesdeTextureDisplay(MonoBehaviour comp, Scene scene)
    {
        var so = new SerializedObject(comp);

        var entry = BaseEntry(comp.gameObject, scene);
        entry.type = "textureDisplay";
        entry.eyeOffset = TryGetVector3(so, "eyeOffset");

        entry.videoScale = TryGetVector3(so, "displayScale");
        entry.salonDescarga = TryGetEnumString(so, "salon");

        entry.objectName = TryGetString(so, "objectName");
        if (string.IsNullOrWhiteSpace(entry.objectName))
            entry.objectName = comp.gameObject.name;
        entry.identifier = entry.objectName;

        // Nombre base de imagen (SIN extensión)
        var img = TryGetString(so, "imageName");
        entry.imageName = StripExt(img);

        return entry;
    }

    private InteractableEntry ConstruirDesdePaintingDisplay(MonoBehaviour comp, Scene scene)
    {
        var so = new SerializedObject(comp);
        Debug.Log($"[Exporter] Construyendo PaintingDisplay desde {comp.gameObject.name} en escena {scene.name}");
        var entry = BaseEntry(comp.gameObject, scene);
        entry.type = "paintingDisplay";
        entry.eyeOffset = TryGetVector3(so, "eyeOffset");

        entry.isInCarrousel = TryGetBool(so, "isInCarrousel");
        entry.carrouselIndex = TryGetInt(so, "indexInCarrousel");

        entry.salonDescarga = TryGetEnumString(so, "salon");

        entry.objectName = TryGetString(so, "objectName");
        if (string.IsNullOrWhiteSpace(entry.objectName))
            entry.objectName = comp.gameObject.name;
        entry.identifier = entry.objectName;

        // Nombre base de imagen (SIN extensión)
        var img = TryGetString(so, "imageName");
        entry.imageName = StripExt(img);

        return entry;
    }

    private IEnumerable<InteractableEntry> ConstruirDesdeItemDisplay(MonoBehaviour comp, Scene scene)
    {
        var so = new SerializedObject(comp);
        var entries = new List<InteractableEntry>();

        bool isSlide = TryGetBool(so, "isSlideShow");
        bool isVideo = TryGetBool(so, "isVideoDisplay");

        if (isSlide)
        {
            var entry = BaseEntry(comp.gameObject, scene);
            entry.type = "ItemDisplay.SlideShow";
            entry.eyeOffset = TryGetVector3(so, "eyeOffset");

            entry.objectName = TryGetString(so, "objectName");
            if (string.IsNullOrWhiteSpace(entry.objectName))
                entry.objectName = comp.gameObject.name;
            entry.identifier = entry.objectName;

            entry.salonDescarga = TryGetEnumString(so, "salon");

            var imageNamesProp = so.FindProperty("imageNames");
            entry.imageNames = new List<string>();

            if (imageNamesProp != null && imageNamesProp.isArray)
            {
                for (int i = 0; i < imageNamesProp.arraySize; i++)
                {
                    var img = imageNamesProp.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(img))
                        entry.imageNames.Add(StripExt(img));
                }
            }

            entries.Add(entry);
        }
        else if (isVideo)
        {
            var entry = BaseEntry(comp.gameObject, scene);
            entry.type = "ItemDisplay.Video";
            entry.isVideo = true;
            entry.eyeOffset = TryGetVector3(so, "eyeOffset");

            entry.objectName = TryGetString(so, "objectName");
            if (string.IsNullOrWhiteSpace(entry.objectName))
                entry.objectName = comp.gameObject.name;
            entry.identifier = entry.objectName;

            entry.salonDescarga = TryGetEnumString(so, "salon");

            // Nombres base (SIN extensión)
            entry.videoName = StripExt(TryGetString(so, "videoName"));
            entry.videoReverse = StripExt(TryGetString(so, "videoReverse"));

            entry.videoPosition = TryGetVector2(so, "videoPosition");
            entry.videoScale = TryGetVector3(so, "videoScale");

            entry.oscilate = TryGetBool(so, "oscilate");

            entries.Add(entry);
        }
        else
        {
        }

        return entries;
    }

    private InteractableEntry BaseEntry(GameObject go, Scene scene)
    {
        return new InteractableEntry
        {
            identifier = go.name,
            type = "",
            scene = scene.name,
            eyeOffset = Vector3.zero,
        };
    }

    // Utilidades de extracción 

    private static string TryGetString(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        return p != null ? p.stringValue : null;
    }

    private static bool TryGetBool(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        return p != null && p.boolValue;
    }

    private static int TryGetInt(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        return p != null ? p.intValue : 0;
    }

    private static string TryGetEnumString(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        if (p == null) return null;
        return Enum.GetName(typeof(URLSalon), p.enumValueIndex);
    }

    private static Vector3 TryGetVector3(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        return p != null ? p.vector3Value : Vector3.zero;
    }

    private static Vector2 TryGetVector2(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        return p != null ? p.vector2Value : Vector2.zero;
    }

    private static string StripExt(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var baseName = Path.GetFileNameWithoutExtension(s);
        return baseName;
    }

    private static string TrimSlash(string s) => string.IsNullOrEmpty(s) ? s : s.Trim();
}
