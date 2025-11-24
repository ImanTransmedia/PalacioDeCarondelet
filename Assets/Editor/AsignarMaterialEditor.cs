#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AsignarMaterialWindow : EditorWindow
{
    private Material materialAAsignar;

    [MenuItem("Tools/Materiales/Asignar Material a Mallas en la Escena")]
    public static void MostrarVentana()
    {
        GetWindow<AsignarMaterialWindow>("Asignar Material");
    }

    private void OnGUI()
    {
        GUILayout.Label("Selecciona un Material:");
        materialAAsignar = (Material)EditorGUILayout.ObjectField(materialAAsignar, typeof(Material), false);

        if (GUILayout.Button("Asignar Material"))
        {
            AsignarMaterialEnEscena(materialAAsignar);
        }
    }

    private void AsignarMaterialEnEscena(Material material)
    {
        if (material == null)
        {
            Debug.LogWarning("Por favor, selecciona un material antes de asignarlo.");
            return;
        }

        GameObject[] objetosEnEscena = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject objeto in objetosEnEscena)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Material[] materiales = renderer.materials;

                for (int i = 0; i < materiales.Length; i++)
                {
                    materiales[i] = material;
                }

                renderer.materials = materiales;
            }
        }
    }
}
#endif

