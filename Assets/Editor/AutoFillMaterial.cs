using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class MaterialRelinkerPro : EditorWindow
{
    // ---------- DATA ----------

    [Serializable]
    public class MaterialRef
    {
        public string materialGuid;
        public string materialName;
    }

    [Serializable]
    public class MeshSig
    {
        public string meshGuid;
        public long meshLocalId;
        public string meshName;
        public int vertexCount;
        public int subMeshCount;
        public int[] triCounts;
        public Vector3 boundsCenter;
        public Vector3 boundsExtents;
    }

    [Serializable]
    public class RendererEntry
    {
        public string normalizedPath;
        public MeshSig meshSig;
        public MaterialRef[] slots;
    }

    [Serializable]
    public class MapJson
    {
        public string unityVersion;
        public string datetimeIso;
        public string rootName;
        public RendererEntry[] entries;
    }

    [Serializable]
    public class Pair
    {
        public string relPath;
        public Renderer source;
        public Renderer target;
        public bool selected;
        public bool foldout;
    }

    private string jsonPath = "Assets/MaterialMap_Robust.json";
    private Transform sourceRoot;
    private Transform targetRoot;

    private List<Pair> pairs = new List<Pair>();
    private Vector2 scroll;
    private string filter = "";
    private int copySlotIndex = -1;
    private Material bulkMaterial;
    private int bulkSlot = -1;
    private bool bulkAllSlots = true;

    private bool foldSetup = true;
    private bool foldReview = true;
    private bool foldJson = false;

    // ---------- WINDOW ----------

    [MenuItem("Tools/Materiales/Material Relinker Pro")]
    public static void ShowWindow() => GetWindow<MaterialRelinkerPro>("Material Relinker");

    void OnGUI()
    {
        DrawHeader();

        // Setup
        foldSetup = EditorGUILayout.BeginFoldoutHeaderGroup(foldSetup, "Setup (elige source y target)");
        if (foldSetup)
        {
            sourceRoot = (Transform)EditorGUILayout.ObjectField("Source Root", sourceRoot, typeof(Transform), true);
            targetRoot = (Transform)EditorGUILayout.ObjectField("Target Root", targetRoot, typeof(Transform), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = sourceRoot && targetRoot;
                if (GUILayout.Button("Rebuild Pairs", GUILayout.Height(24))) BuildPairs();
                if (GUILayout.Button("Auto Pair", GUILayout.Height(24))) { BuildPairs(); Repaint(); }
                if (GUILayout.Button("Transfer Now (source -> target)", GUILayout.Height(24))) TransferNow();
                GUI.enabled = true;
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Review
        foldReview = EditorGUILayout.BeginFoldoutHeaderGroup(foldReview, "Review y copia de materiales");
        if (foldReview)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                filter = EditorGUILayout.TextField("Filtro", filter);
                if (GUILayout.Button("Clear", GUILayout.Width(60))) filter = "";
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select All", GUILayout.Width(90))) SetSelection(true);
                if (GUILayout.Button("None", GUILayout.Width(60))) SetSelection(false);
                if (GUILayout.Button("Invert", GUILayout.Width(70))) InvertSelection();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                copySlotIndex = EditorGUILayout.IntField("Slot", copySlotIndex, GUILayout.Width(150));
                if (GUILayout.Button("Copy Selected", GUILayout.Height(22))) CopyMaterials(true);
                if (GUILayout.Button("Copy All", GUILayout.Height(22))) CopyMaterials(false);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Bulk asignacion rapida", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                bulkMaterial = (Material)EditorGUILayout.ObjectField("Material", bulkMaterial, typeof(Material), false);
                bulkAllSlots = EditorGUILayout.ToggleLeft("Todos los slots", bulkAllSlots, GUILayout.Width(130));
                using (new EditorGUI.DisabledScope(bulkAllSlots))
                    bulkSlot = EditorGUILayout.IntField("Slot", bulkSlot, GUILayout.Width(120));
                if (GUILayout.Button("Asignar a seleccionados", GUILayout.Height(22))) BulkAssignToSelected();
            }

            EditorGUILayout.Space(6);
            DrawPairsList();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // JSON mode (opcional)
        foldJson = EditorGUILayout.BeginFoldoutHeaderGroup(foldJson, "Modo JSON (opcional)");
        if (foldJson)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                jsonPath = EditorGUILayout.TextField("JSON Asset Path", jsonPath);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var p = EditorUtility.SaveFilePanelInProject("Guardar JSON", "MaterialMap", "json", "Elige donde guardar/cargar el JSON");
                    if (!string.IsNullOrEmpty(p)) jsonPath = p;
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Map (desde source seleccionado)", GUILayout.Height(24))) SaveMapFromSelection();
                if (GUILayout.Button("Restore Map (al target seleccionado)", GUILayout.Height(24))) RestoreMapToSelection();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawHeader()
    {
        EditorGUILayout.Space(4);
        var r = EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("Source -> Target", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Abrir review con 2 seleccionados", GUILayout.Height(20)))
        {
            var sel = Selection.transforms;
            if (sel != null && sel.Length >= 2)
            {
                sourceRoot = sel[0];
                targetRoot = sel[1];
                BuildPairs();
            }
            else EditorUtility.DisplayDialog("Relinker", "Selecciona dos raices en la jerarquia.", "OK");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    // ---------- PAIRS UI ----------

    void DrawPairsList()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < pairs.Count; i++)
        {
            var p = pairs[i];
            if (!PassFilter(p)) continue;

            EditorGUILayout.BeginVertical("box");
            using (new EditorGUILayout.HorizontalScope())
            {
                p.selected = EditorGUILayout.Toggle(p.selected, GUILayout.Width(18));
                p.foldout = EditorGUILayout.Foldout(p.foldout, p.relPath, true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy", GUILayout.Width(60))) CopyOne(p);
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    if (p.source) EditorGUIUtility.PingObject(p.source);
                    if (p.target) EditorGUIUtility.PingObject(p.target);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Source");
                p.source = (Renderer)EditorGUILayout.ObjectField(p.source, typeof(Renderer), true);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Target");
                p.target = (Renderer)EditorGUILayout.ObjectField(p.target, typeof(Renderer), true);
                EditorGUILayout.EndVertical();
            }

            if (p.foldout) DrawSlotEditor(p);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawSlotEditor(Pair p)
    {
        using (new EditorGUI.DisabledScope(!(p.source && p.target)))
        {
            var sMats = p.source ? p.source.sharedMaterials : Array.Empty<Material>();
            var tMats = p.target ? (p.target.sharedMaterials ?? Array.Empty<Material>()) : Array.Empty<Material>();
            int max = Mathf.Max(sMats.Length, tMats.Length);
            if (max == 0) return;

            var newTarget = new Material[max];
            tMats.CopyTo(newTarget, 0);

            for (int i = 0; i < max; i++)
            {
                Material s = i < sMats.Length ? sMats[i] : null;
                Material t = i < tMats.Length ? tMats[i] : null;

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Slot {i}", GUILayout.Width(60));

                // campo source (sirve como origen para arrastrar)
                Rect srcRect;
                using (new EditorGUI.DisabledScope(true))
                {
                    var prev = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 12;
                    s = (Material)EditorGUILayout.ObjectField("S", s, typeof(Material), false);
                    EditorGUIUtility.labelWidth = prev;
                }
                srcRect = GUILayoutUtility.GetLastRect();

                // campo target y drop
                Rect tgtRect;
                {
                    var prev = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 12;
                    newTarget[i] = (Material)EditorGUILayout.ObjectField("T", t, typeof(Material), false);
                    EditorGUIUtility.labelWidth = prev;
                }
                tgtRect = GUILayoutUtility.GetLastRect();

                // boton flecha
                if (GUILayout.Button("←", GUILayout.Width(28)) && s) newTarget[i] = s;

                // drag from anywhere (srcRect) to target (tgtRect)
                HandleDragAndDrop(tgtRect, ref newTarget[i]);

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Aplicar a Target"))
            {
                Undo.RecordObject(p.target, "Apply Materials (Pair)");
                p.target.sharedMaterials = newTarget;
                EditorUtility.SetDirty(p.target);
            }
        }
    }

    void HandleDragAndDrop(Rect dropRect, ref Material targetMat)
    {
        var evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            var hasMat = DragAndDrop.objectReferences.Any(o => o is Material);
            if (!hasMat) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var m = DragAndDrop.objectReferences.FirstOrDefault(o => o is Material) as Material;
                if (m)
                {
                    targetMat = m;
                    GUI.changed = true;
                }
            }
            evt.Use();
        }
    }

    // ---------- BUILD / MATCH ----------

    void BuildPairs()
    {
        pairs.Clear();
        if (!sourceRoot || !targetRoot) return;

        var sourceRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true)
            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToList();
        var targetRenderers = targetRoot.GetComponentsInChildren<Renderer>(true)
            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToList();

        var dictTargetByRel = new Dictionary<string, Renderer>();
        foreach (var tr in targetRenderers)
        {
            var rp = NormalizePath(RelativeChildPath(targetRoot, tr.transform));
            if (!dictTargetByRel.ContainsKey(rp)) dictTargetByRel[rp] = tr;
        }

        var targetSigIndex = targetRenderers
            .Select(tr => (r: tr, sig: BuildMeshSig(GetRendererMesh(tr))))
            .Where(t => t.sig != null).ToList();

        foreach (var sr in sourceRenderers)
        {
            Renderer match = null;
            var rel = NormalizePath(RelativeChildPath(sourceRoot, sr.transform));
            if (dictTargetByRel.TryGetValue(rel, out var byPath)) match = byPath;

            if (!match)
            {
                var sig = BuildMeshSig(GetRendererMesh(sr));
                match = FindClosestBySignature(targetSigIndex.Select(t => t.r).ToList(), sig);
                if (!match)
                {
                    var name = NormalizeName(sr.name).ToLowerInvariant();
                    match = targetRenderers.FirstOrDefault(t => NormalizeName(t.name).ToLowerInvariant() == name);
                }
            }

            pairs.Add(new Pair { relPath = rel, source = sr, target = match, selected = true, foldout = false });
        }
    }

    void CopyMaterials(bool selectedOnly)
    {
        foreach (var p in pairs)
        {
            if (selectedOnly && !p.selected) continue;
            CopyOne(p);
        }
    }

    void CopyOne(Pair p)
    {
        if (!(p.source && p.target)) return;
        var s = p.source.sharedMaterials ?? Array.Empty<Material>();
        var t = p.target.sharedMaterials ?? Array.Empty<Material>();
        if (copySlotIndex >= 0)
        {
            int idx = copySlotIndex;
            var arr = t.ToArray();
            if (arr.Length < Mathf.Max(1, idx + 1)) Array.Resize(ref arr, idx + 1);
            var mat = idx < s.Length ? s[idx] : null;
            Undo.RecordObject(p.target, "Copy Material Slot");
            arr[idx] = mat;
            p.target.sharedMaterials = arr;
            EditorUtility.SetDirty(p.target);
        }
        else
        {
            Undo.RecordObject(p.target, "Copy Materials All");
            p.target.sharedMaterials = s.ToArray();
            EditorUtility.SetDirty(p.target);
        }
    }

    void BulkAssignToSelected()
    {
        if (!bulkMaterial) return;
        foreach (var p in pairs)
        {
            if (!p.selected || !p.target) continue;
            var arr = p.target.sharedMaterials ?? Array.Empty<Material>();
            if (bulkAllSlots)
            {
                if (arr.Length == 0) arr = new[] { bulkMaterial };
                else for (int i = 0; i < arr.Length; i++) arr[i] = bulkMaterial;
            }
            else
            {
                int idx = Mathf.Max(0, bulkSlot);
                if (arr.Length < idx + 1) Array.Resize(ref arr, idx + 1);
                arr[idx] = bulkMaterial;
            }
            Undo.RecordObject(p.target, "Bulk Assign Materials");
            p.target.sharedMaterials = arr;
            EditorUtility.SetDirty(p.target);
        }
    }

    void SetSelection(bool state) { foreach (var p in pairs) p.selected = state; }
    void InvertSelection() { foreach (var p in pairs) p.selected = !p.selected; }
    bool PassFilter(Pair p)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var f = filter.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(p.relPath) && p.relPath.ToLowerInvariant().Contains(f)) return true;
        if (p.source && p.source.name.ToLowerInvariant().Contains(f)) return true;
        if (p.target && p.target.name.ToLowerInvariant().Contains(f)) return true;
        return false;
    }

    // ---------- JSON MODE ----------

    void SaveMapFromSelection()
    {
        var root = Selection.activeTransform;
        if (!root)
        {
            EditorUtility.DisplayDialog("Relinker", "Selecciona el root del FBX/prefab en la escena o Prefab Mode.", "OK");
            return;
        }
        var map = BuildMap(root);
        WriteJson(map, jsonPath);
        EditorUtility.DisplayDialog("Relinker", $"Mapa guardado:\n{jsonPath}", "OK");
    }

    void RestoreMapToSelection()
    {
        if (!File.Exists(jsonPath))
        {
            EditorUtility.DisplayDialog("Relinker", $"No existe JSON:\n{jsonPath}", "OK");
            return;
        }
        var root = Selection.activeTransform;
        if (!root)
        {
            EditorUtility.DisplayDialog("Relinker", "Selecciona el root destino donde restaurar.", "OK");
            return;
        }
        var map = ReadJson(jsonPath);
        if (map?.entries == null || map.entries.Length == 0)
        {
            EditorUtility.DisplayDialog("Relinker", "JSON invalido o vacio.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Material Relinker (Robust Restore)");

        var targetIndex = IndexRenderers(root);
        int matched = 0, slots = 0, misses = 0;

        foreach (var e in map.entries)
        {
            var candidates = FindCandidates(targetIndex, e);
            if (candidates.Count == 0) { misses++; continue; }
            foreach (var r in candidates)
            {
                slots += ApplySlots(r, e.slots);
                matched++;
            }
        }

        EditorUtility.DisplayDialog("Relinker",
            $"Renderers emparejados: {matched}\nSlots reasignados: {slots}\nEntradas sin match: {misses}",
            "OK");
    }

    void TransferNow()
    {
        if (!sourceRoot || !targetRoot)
        {
            EditorUtility.DisplayDialog("Relinker", "Asigna Source Root y Target Root.", "OK");
            return;
        }
        var map = BuildMap(sourceRoot);
        var tmp = Path.Combine("Assets", "MaterialMap_Robust_TMP.json");
        WriteJson(map, tmp);

        Undo.RegisterFullObjectHierarchyUndo(targetRoot.gameObject, "Material Relinker (Robust Transfer)");

        var targetIndex = IndexRenderers(targetRoot);
        int matched = 0, slots = 0, misses = 0;

        foreach (var e in map.entries)
        {
            var candidates = FindCandidates(targetIndex, e);
            if (candidates.Count == 0) { misses++; continue; }
            foreach (var r in candidates)
            {
                slots += ApplySlots(r, e.slots);
                matched++;
            }
        }

        EditorUtility.DisplayDialog("Relinker",
            $"Transferencia terminada.\nRenderers emparejados: {matched}\nSlots reasignados: {slots}\nSin match: {misses}",
            "OK");
    }

    // ---------- INDEX / MATCHING CORE ----------

    public class TargetIndex
    {
        public readonly Dictionary<string, List<Renderer>> byMeshGuidLocal = new();
        public readonly Dictionary<string, List<Renderer>> bySignature = new();
        public readonly Dictionary<string, List<Renderer>> byNormPath = new();
        public readonly List<Renderer> all = new();
    }

    TargetIndex IndexRenderers(Transform root)
    {
        var idx = new TargetIndex();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            var mesh = GetRendererMesh(r);
            if (!mesh) continue;

            idx.all.Add(r);

            if (TryGetMeshIds(mesh, out var guid, out var local))
            {
                var key = guid + "|" + local;
                Add(idx.byMeshGuidLocal, key, r);
            }

            var sig = BuildMeshSig(mesh);
            Add(idx.bySignature, SigKey(sig), r);

            var np = NormalizePath(RelativeChildPath(root, r.transform));
            Add(idx.byNormPath, np, r);
        }
        return idx;
    }

    List<Renderer> FindCandidates(TargetIndex idx, RendererEntry e)
    {
        if (!string.IsNullOrEmpty(e.meshSig.meshGuid) && e.meshSig.meshLocalId != 0)
        {
            var key = e.meshSig.meshGuid + "|" + e.meshSig.meshLocalId;
            if (idx.byMeshGuidLocal.TryGetValue(key, out var list) && list.Count > 0)
                return list;
        }

        if (idx.bySignature.TryGetValue(SigKey(e.meshSig), out var list2) && list2.Count > 0)
            return list2;

        var childPath = ChildOnly(e.normalizedPath);
        if (!string.IsNullOrEmpty(childPath) && idx.byNormPath.TryGetValue(childPath, out var list3) && list3.Count > 0)
            return list3;

        var best = FindClosestBySignature(idx.all, e.meshSig);
        return best != null ? new List<Renderer> { best } : new List<Renderer>();
    }

    int ApplySlots(Renderer r, MaterialRef[] refs)
    {
        if (refs == null || refs.Length == 0) return 0;
        var arr = r.sharedMaterials ?? Array.Empty<Material>();
        if (arr.Length != refs.Length) Array.Resize(ref arr, refs.Length);
        int set = 0;
        for (int i = 0; i < refs.Length; i++)
        {
            var m = ResolveMaterial(refs[i]);
            if (i < arr.Length)
            {
                arr[i] = m;
                set++;
            }
        }
        Undo.RecordObject(r, "Apply Materials");
        r.sharedMaterials = arr;
        EditorUtility.SetDirty(r);
        return set;
    }

    // ---------- MAP BUILD ----------

    MapJson BuildMap(Transform root)
    {
        var entries = new List<RendererEntry>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            var mesh = GetRendererMesh(r);
            if (!mesh) continue;

            var sig = BuildMeshSig(mesh);
            var shared = r.sharedMaterials ?? Array.Empty<Material>();
            var slotRefs = new MaterialRef[shared.Length];
            for (int i = 0; i < shared.Length; i++)
                slotRefs[i] = ToMaterialRef(shared[i]);

            entries.Add(new RendererEntry
            {
                normalizedPath = NormalizePath(RelativePath(root, r.transform)),
                meshSig = sig,
                slots = slotRefs
            });
        }

        return new MapJson
        {
            unityVersion = Application.unityVersion,
            datetimeIso = DateTime.Now.ToString("o"),
            rootName = root.name,
            entries = entries.ToArray()
        };
    }

    // ---------- UTILS ----------

    public static Mesh GetRendererMesh(Renderer r)
    {
        if (r is SkinnedMeshRenderer sk) return sk.sharedMesh;
        var mf = r.GetComponent<MeshFilter>();
        return mf ? mf.sharedMesh : null;
    }

    public static MeshSig BuildMeshSig(Mesh m)
    {
        if (!m) return null;

        var sig = new MeshSig
        {
            meshName = m.name,
            vertexCount = m.vertexCount,
            subMeshCount = m.subMeshCount,
            boundsCenter = m.bounds.center,
            boundsExtents = m.bounds.extents
        };

        if (TryGetMeshIds(m, out var g, out var l))
        {
            sig.meshGuid = g;
            sig.meshLocalId = l;
        }

        int cap = Mathf.Min(sig.subMeshCount, 8);
        sig.triCounts = new int[cap];
        for (int i = 0; i < cap; i++)
            sig.triCounts[i] = (int)(m.GetIndexCount(i) / 3);

        return sig;
    }

    public static bool TryGetMeshIds(UnityEngine.Object o, out string guid, out long localId)
    {
        guid = null;
        localId = 0;
        try
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out guid, out long lid))
            {
                localId = lid;
                if (string.IsNullOrEmpty(guid) || localId == 0) return false;
                return true;
            }
        }
        catch { }
        return false;
    }

    public static MaterialRef ToMaterialRef(Material m)
    {
        var mr = new MaterialRef { materialName = m ? m.name : null, materialGuid = null };
        if (m)
        {
            var path = AssetDatabase.GetAssetPath(m);
            if (!string.IsNullOrEmpty(path))
                mr.materialGuid = AssetDatabase.AssetPathToGUID(path);
        }
        return mr;
    }

    public static Material ResolveMaterial(MaterialRef mr)
    {
        if (!string.IsNullOrEmpty(mr.materialGuid))
        {
            var path = AssetDatabase.GUIDToAssetPath(mr.materialGuid);
            if (!string.IsNullOrEmpty(path))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat) return mat;
            }
        }
        if (!string.IsNullOrEmpty(mr.materialName))
        {
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
                if (m && m.name == mr.materialName) return m;

            var guids = AssetDatabase.FindAssets($"t:Material {mr.materialName}");
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m && m.name == mr.materialName) return m;
            }

            var folder = "Assets/GeneratedMaterials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "GeneratedMaterials");
            var safe = MakeSafeFileName(mr.materialName);
            var assetPath = $"{folder}/{safe}.mat";
            var exists = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (exists) return exists;

            var created = new Material(Shader.Find("Standard")) { name = mr.materialName };
            AssetDatabase.CreateAsset(created, assetPath);
            AssetDatabase.SaveAssets();
            return created;
        }
        return null;
    }

    public static string RelativePath(Transform root, Transform t)
    {
        var stack = new Stack<string>();
        var cur = t;
        while (cur && cur != root) { stack.Push(cur.name); cur = cur.parent; }
        stack.Push(root.name);
        return string.Join("/", stack);
    }

    public static string RelativeChildPath(Transform root, Transform t)
    {
        var stack = new Stack<string>();
        var cur = t;
        while (cur && cur != root) { stack.Push(cur.name); cur = cur.parent; }
        return string.Join("/", stack);
    }

    public static string NormalizePath(string path)
    {
        var segs = path.Split('/');
        for (int i = 0; i < segs.Length; i++)
            segs[i] = NormalizeName(segs[i]).ToLowerInvariant();
        return string.Join("/", segs);
    }

    public static string NormalizeName(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var r = Regex.Replace(s, @"([._\- ])\d+$", "");
        r = Regex.Replace(r, @"\(Instance\)$", "", RegexOptions.IgnoreCase);
        return r;
    }

    public static string SigKey(MeshSig s)
    {
        var triPart = s.triCounts != null && s.triCounts.Length > 0 ? string.Join(",", s.triCounts) : "";
        return $"{s.vertexCount}|{s.subMeshCount}|{triPart}|{RoundV(s.boundsCenter)}|{RoundV(s.boundsExtents)}";
    }

    public static string RoundV(Vector3 v)
    {
        return $"{Mathf.Round(v.x * 1000f) / 1000f},{Mathf.Round(v.y * 1000f) / 1000f},{Mathf.Round(v.z * 1000f) / 1000f}";
    }

    public static void Add<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue val)
    {
        if (!dict.TryGetValue(key, out var list)) { list = new List<TValue>(); dict[key] = list; }
        list.Add(val);
    }

    static void WriteJson(MapJson map, string path)
    {
        var json = JsonUtility.ToJson(map, true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }

    static MapJson ReadJson(string path)
    {
        try { return JsonUtility.FromJson<MapJson>(File.ReadAllText(path)); }
        catch { return null; }
    }

    public static string MakeSafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    public static string ChildOnly(string normalizedPath)
    {
        if (string.IsNullOrEmpty(normalizedPath)) return normalizedPath;
        var i = normalizedPath.IndexOf('/');
        return i >= 0 && i + 1 < normalizedPath.Length ? normalizedPath.Substring(i + 1) : "";
    }

    public static Renderer FindClosestBySignature(List<Renderer> all, MeshSig sig)
    {
        if (sig == null) return null;
        Renderer best = null;
        float bestScore = float.MaxValue;

        foreach (var r in all)
        {
            var m = GetRendererMesh(r);
            if (!m) continue;

            float score = 0f;
            score += Mathf.Abs((m.vertexCount - sig.vertexCount));
            score += Mathf.Abs((m.subMeshCount - sig.subMeshCount)) * 10f;

            for (int i = 0; i < Mathf.Min(m.subMeshCount, sig.triCounts?.Length ?? 0); i++)
            {
                int tris = (int)(m.GetIndexCount(i) / 3);
                score += Mathf.Abs(tris - sig.triCounts[i]) * 0.5f;
            }

            var b = m.bounds;
            score += (Vector3.Distance(b.center, sig.boundsCenter) * 0.1f);
            score += (Vector3.Distance(b.extents, sig.boundsExtents) * 0.1f);

            if (score < bestScore) { bestScore = score; best = r; }
        }
        return bestScore < 50f ? best : null;
    }
}
