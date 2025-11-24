using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReplacePlaneMeshColliders : MonoBehaviour
{
    [MenuItem("Tools/Limpiar y Organizar/Reemplazar MeshCollider por BoxCollider")]
    static void ReplaceMeshColliders()
    {
        int reemplazados = 0;
        List<GameObject> objetosReemplazados = new List<GameObject>();
        Mesh planeMesh = Resources.GetBuiltinResource<Mesh>("New-Plane.fbx");

        foreach (MeshCollider mc in FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mc.sharedMesh == planeMesh)
            {
                GameObject obj = mc.gameObject;

                bool wasEnabled = mc.enabled;
                DestroyImmediate(mc);

                BoxCollider bc = obj.AddComponent<BoxCollider>();
                bc.enabled = wasEnabled;

                // Añadir componente visual
                if (!obj.GetComponent<MeshUpdateIndicator>())
                    obj.AddComponent<MeshUpdateIndicator>();

                objetosReemplazados.Add(obj);
                reemplazados++;
                Debug.Log($"Reemplazado MeshCollider por BoxCollider en: {obj.name}");
            }
        }

        // Selección en editor
        if (objetosReemplazados.Count > 0)
        {
            Selection.objects = objetosReemplazados.ToArray();
        }

        Debug.Log($"Proceso completado. Colliders reemplazados: {reemplazados}");
    }
}
