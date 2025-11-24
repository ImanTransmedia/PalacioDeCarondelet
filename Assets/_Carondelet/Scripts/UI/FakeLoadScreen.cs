using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FakeLoadScreen : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI textPercentage;
    public float fakeLoadDuration = 5f;
    public float timeReload = 5f;
    public bool isSecondLoad = false;
    private float _elapsedTime = 0f;
    private bool _isLoadingComplete = false;
    

    private void OnEnable()
    {
        StartCoroutine(FakeLoadRoutine());

    }

    private void OnDisable()
    {
        _isLoadingComplete = false;
        _elapsedTime = 0f;
        slider.value = 0f;
    }

    private IEnumerator FakeLoadRoutine()
    {
        while (_elapsedTime < fakeLoadDuration)
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / fakeLoadDuration);

            slider.value = progress;
            textPercentage.text = Mathf.CeilToInt(progress * 99f) + "%";

            yield return null;
        }

        slider.value = 1f;
        textPercentage.text = "99%";
        _isLoadingComplete = true;

        Debug.Log("Carga fake completada.");

        yield return new WaitForSeconds(timeReload);
        _isLoadingComplete = false;
        _elapsedTime = 0f;
        slider.value = 0f;
        textPercentage.text = "0%";

        if (!isSecondLoad)isSecondLoad = true;
        else yield return new WaitForSeconds(10000f);
    }
}
