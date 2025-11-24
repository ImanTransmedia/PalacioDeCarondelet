using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BDCMode : MonoBehaviour
{
    public static BDCMode Instance { get; private set; }

    [Header("Eventos del codigo Konami")]
    public UnityEvent OnModoBDCUp;
    public UnityEvent OnModoBDCDown;

    [Header("Configuracion")]
    public float inputTimeout = 1.25f;

    // Secuencia: ↑ ↑ ↓ ↓ ← → ← → B A
    private readonly KeyCode[] _konami = new KeyCode[]
    {
        KeyCode.UpArrow, KeyCode.UpArrow,
        KeyCode.DownArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.B, KeyCode.A
    };

    private int _index = 0;
    private float _lastInputTime = 0f;
    private bool _modoBalazoActivo = false;

    private readonly List<GameObject> _objetosBDC = new List<GameObject>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Timeout entre teclas
        if (_index > 0 && (Time.time - _lastInputTime) > inputTimeout)
        {
            Debug.Log("Tiempo excedido, reiniciando secuencia.");
            _index = 0;
        }

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(_konami[_index]))
            {
                _index++;
                _lastInputTime = Time.time;

                if (_index >= _konami.Length)
                {
                    _index = 0;
                    ToggleModo(); 
                }
            }
            else
            {
                _index = 0;
                if (Input.GetKeyDown(_konami[0]))
                {
                    _index = 1;
                    _lastInputTime = Time.time;
                }
            }
        }
    }

    public void ToggleModo()
    {
        _modoBalazoActivo = !_modoBalazoActivo;

        if (_modoBalazoActivo)
        {
            Debug.Log("¡Modo BDC ACTIVADO!");
            OnModoBDCUp?.Invoke();
        }
        else
        {
            Debug.Log("¡Modo BDC DESACTIVADO!");
            OnModoBDCDown?.Invoke();
        }

        GameManagerBDC.Instance.TogleBDCMode();

        CambiarEstadoObjetos(_modoBalazoActivo);
    }

    public void Registrar(GameObject obj)
    {
        if (!_objetosBDC.Contains(obj))
        {
            _objetosBDC.Add(obj);
            obj.SetActive(_modoBalazoActivo);
        }
    }

    public void Desregistrar(GameObject obj)
    {
        _objetosBDC.Remove(obj);
    }

    private void CambiarEstadoObjetos(bool estado)
    {
        _objetosBDC.RemoveAll(o => o == null);

        foreach (var obj in _objetosBDC)
        {
            obj.SetActive(estado);
        }
    }
}
