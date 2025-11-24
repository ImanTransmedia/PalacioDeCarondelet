using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TextureAlbedoUpdater : MonoBehaviour
{
    [Header("Configuración del material")]
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private Texture2D newAlbedoTexture;

    [Header("Descarga desde URL (opcional)")]
    [SerializeField] private bool downloadFromURL = false;
    [SerializeField] private string commonURL = "https://palaciocarondelet360.presidencia.gob.ec/MediaResources/Textures/";
    [SerializeField] private URLSalon salon;
    [SerializeField] private string imageUrl;

    private Renderer objectRenderer;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer == null)
        {
            Debug.LogError("No se encontró un Renderer en el objeto.");
            return;
        }

        if (downloadFromURL && !string.IsNullOrEmpty(imageUrl))
        {
            imageUrl = $"{commonURL}{salon.ToString()}/{imageUrl}";
            StartCoroutine(DownloadAndApplyTexture(imageUrl));
        }
        else if (newAlbedoTexture != null)
        {
            ApplyTexture(newAlbedoTexture);
        }
        else
        {
            Debug.LogWarning("No se ha proporcionado una textura ni una URL.");
        }
    }

    private void ApplyTexture(Texture2D texture)
    {
        Material[] materials = objectRenderer.materials;

        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
            Debug.LogError("Índice de material fuera de rango.");
            return;
        }

        materials[materialIndex].SetTexture("_MainTex", texture);
    }

    private IEnumerator DownloadAndApplyTexture(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Error al descargar la textura desde {url}: {request.error}");
            }
            else
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                ApplyTexture(downloadedTexture);
            }
        }
    }
}
