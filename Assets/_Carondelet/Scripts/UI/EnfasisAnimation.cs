using UnityEngine;
using UnityEngine.UI;

public class EnfasisAnimation : MonoBehaviour
{
    [Header("Scale Animation")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private Vector2 scaleFromTo = new Vector2(0.9f, 1.1f);
    [SerializeField] private float scaleSpeed = 1f;

    [Header("Color Animation")]
    [SerializeField] private bool useColorAnimation = false;
    [SerializeField] private Image targetImage;
    [SerializeField] private Color colorFrom = Color.white;
    [SerializeField] private Color colorTo = Color.yellow;
    [SerializeField] private float colorSpeed = 1f;

    private void Update()
    {
        if (useScaleAnimation)
        {
            AnimateScale();
        }

        if (useColorAnimation && targetImage != null)
        {
            AnimateColor();
        }
    }

    private void AnimateScale()
    {
        float t = Mathf.PingPong(Time.time * scaleSpeed, 1f);
        float scale = Mathf.Lerp(scaleFromTo.x, scaleFromTo.y, t);
        transform.localScale = Vector3.one * scale;
    }

    private void AnimateColor()
    {
        float t = Mathf.PingPong(Time.time * colorSpeed, 1f);
        targetImage.color = Color.Lerp(colorFrom, colorTo, t);
    }
}
