// VideoUpdatter.cs
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class VideoUpdatter : MonoBehaviour
{
    [Header("URLs de Videos de Google Drive")]
    [Tooltip("Pega la URL COMPLETA de compartir.")]
    public List<string> videoPreviewUrls = new List<string>();

    private const string DRIVE_STREAM_BASE = "https://drive.google.com/uc?export=download&id=";


    public string GetVideoUrl(int index)
    {
        if (index >= 0 && index < videoPreviewUrls.Count)
        {
            string previewUrl = videoPreviewUrls[index];
            string fileId = ExtractFileId(previewUrl);

            if (!string.IsNullOrEmpty(fileId))
            {
                Debug.Log($"[VideoUpdatter] URL de video generada: {DRIVE_STREAM_BASE + fileId}");
                return DRIVE_STREAM_BASE + fileId;
            }
        }

        Debug.LogError($"[VideoUpdatter] Error: El índice ({index}) ");
        return string.Empty;
    }


    private string ExtractFileId(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        Match match = Regex.Match(url, @"/d/([a-zA-Z0-9_-]+)/");

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(url, @"id=([a-zA-Z0-9_-]+)");

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        Debug.LogError($"[VideoUpdatter] No se pudo extraer el ID de la URL: {url}.");
        return string.Empty;
    }
}