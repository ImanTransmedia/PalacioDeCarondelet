using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ResetCamMenu : MonoBehaviour
{
    public RectTransform targetPanel;
    public UnityEvent onClickOutside;
    public bool anyTouchTriggers = true;
    public bool ignoreWhenOverAnyUI = false;

    private Canvas _canvas;
    private Camera _uiCamera;

    public List<RectTransform> excludedButtons;


    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;

        if (targetPanel == null)
        {
            Debug.Log("ResetCamMenu: targetPanel no asignado");
        }
    }

    private void Update()
    {
        // Detectar clic izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            TryTriggerFromScreenPoint(Input.mousePosition, -1);
        }

        // Detectar toques en pantalla
        if (Input.touchCount > 0)
        {
            if (anyTouchTriggers)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        if (TryTriggerFromScreenPoint(t.position, t.fingerId))
                            break;
                    }
                }
            }
            else
            {
                Touch t0 = Input.GetTouch(0);
                if (t0.phase == TouchPhase.Began)
                {
                    TryTriggerFromScreenPoint(t0.position, t0.fingerId);
                }
            }
        }

        // Detectar tecla ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ResetCamMenu: ESC presionado, cerrando menu");
            onClickOutside?.Invoke();
        }
    }

    private bool TryTriggerFromScreenPoint(Vector2 screenPos, int pointerId)
    {
        if (targetPanel == null) return false;

        if (ignoreWhenOverAnyUI && EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                // Verificar si está en botones excluidos
                if (IsOverExcludedButton(screenPos))
                    return false;
            }
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetPanel, screenPos, _uiCamera, out Vector2 localPos))
        {
            if (!targetPanel.rect.Contains(localPos))
            {
                if (!IsOverExcludedButton(screenPos)) // <- nueva condición
                {
                    Debug.Log("ResetCamMenu: click fuera del panel");
                    onClickOutside?.Invoke();
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsOverExcludedButton(Vector2 screenPos)
    {
        foreach (var rect in excludedButtons)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, _uiCamera))
            {
                Debug.Log("ResetCamMenu: click en botón excluido " + rect.name);
                return true;
            }
        }
        return false;
    }

}
