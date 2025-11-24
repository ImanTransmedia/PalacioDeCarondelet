using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(ScrollRect))]
public class ScrollViewController : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private Vector2 lastContentSize;

    [Header("Scroll Settings")]
    public float scrollSpeed = 0.1f; 

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        contentRect = scrollRect.content;
    }

    void OnEnable()
    {
        StartCoroutine(ResetScrollNextFrame());
    }

    void Update()
    {
        if (contentRect == null) return;

        if (lastContentSize != contentRect.sizeDelta)
        {
            lastContentSize = contentRect.sizeDelta;
            StartCoroutine(ResetScrollNextFrame());
        }

        HandleKeyboardScroll();
    }

    void HandleKeyboardScroll()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            scrollRect.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
    }

    IEnumerator ResetScrollNextFrame()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
