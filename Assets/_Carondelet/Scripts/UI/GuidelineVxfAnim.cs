using UnityEngine;

public class GuidelineVxfAnim : MonoBehaviour
{
    [Header("Control de Velocidad")]
    public float speedX = 0.5f;
    public float speedY = 0.5f;

    private Material material;
    private Vector2 currentOffset;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            material = renderer.material;
            currentOffset = Vector2.zero;
        }
        else
        {
            Debug.LogError("[GuidelineVxfAnim] No se encontró Renderer en el objeto.");
        }
    }

    void Update()
    {
        if (material != null)
        {
            currentOffset.x += speedX * Time.deltaTime;
            currentOffset.y += speedY * Time.deltaTime;

            material.mainTextureOffset = currentOffset;
        }
    }
}
