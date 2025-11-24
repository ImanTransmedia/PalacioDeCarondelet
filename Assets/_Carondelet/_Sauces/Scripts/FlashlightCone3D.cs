using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlashlightCone3D : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float radius = 12f;             // alcance
    [Range(1f, 179f)]
    [SerializeField] private float spotAngle = 45f;          // grados del cono (total)
    [SerializeField] private LayerMask detectableMask;       // capas que pueden ser reveladas (colliders)
    [SerializeField] private LayerMask occludersMask;        // capas que bloquean la visión (paredes, etc.)
    [SerializeField] private float losPadding = 0.05f;       // pequeño margen para el raycast

    [Header("Visual")]
    [SerializeField] private bool controlSpotLight = true;   // sincroniza con el Light
    private Light _light;

    // buffer para evitar GC
    private readonly Collider[] _overlapBuffer = new Collider[64];

    // track de revelados activos este frame
    private readonly HashSet<IRevealable> _revealedThisFrame = new HashSet<IRevealable>();
    private readonly HashSet<IRevealable> _revealedPrevFrame = new HashSet<IRevealable>();

    private void Awake()
    {
        _light = GetComponent<Light>();
        if (controlSpotLight && _light)
        {
            _light.type = LightType.Spot;
            _light.spotAngle = spotAngle;
            _light.range = radius;
            _light.enabled = false; // la prendo solo cuando se usa
        }
    }

    private void Update()
    {
        bool usingLantern = GameManagerBDC.Instance != null && GameManagerBDC.Instance.isUsingLantern;

        if (controlSpotLight && _light)
            _light.enabled = usingLantern;

        if (!usingLantern)
        {
            // Apaga todo lo que estaba revelado
            if (_revealedPrevFrame.Count > 0)
            {
                foreach (var r in _revealedPrevFrame) r?.Reveal(false);
                _revealedPrevFrame.Clear();
            }
            return;
        }

        // Sincroniza valores visuales con la lógica
        if (controlSpotLight && _light)
        {
            _light.spotAngle = spotAngle;
            _light.range = radius;
        }

        // Escaneo bruto por esfera
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _overlapBuffer, detectableMask, QueryTriggerInteraction.Collide);

        _revealedThisFrame.Clear();
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        float halfAngle = spotAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (!col) continue;

            // Busca un IRevealable en el collider o arriba
            if (!col.TryGetComponent<IRevealable>(out var reve))
                reve = col.GetComponentInParent<IRevealable>();

            if (reve == null) continue;

            Vector3 toTarget = (col.bounds.center - origin);
            float dist = toTarget.magnitude;
            Vector3 dir = toTarget / Mathf.Max(dist, 0.0001f);

            // Test ángulo (cono)
            float angle = Vector3.Angle(forward, dir);
            if (angle > halfAngle) continue;

            // Línea de visión (raycast)
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist + losPadding, occludersMask, QueryTriggerInteraction.Ignore))
            {
                // Hay algo entre medio que bloquea
                reve.Reveal(false);
                continue;
            }

            // Pasa filtros -> Revelado
            reve.Reveal(true);
            _revealedThisFrame.Add(reve);
        }

        // Todo lo que estaba revelado y no se reveló este frame -> ocultar
        if (_revealedPrevFrame.Count > 0)
        {
            foreach (var r in _revealedPrevFrame)
            {
                if (!_revealedThisFrame.Contains(r))
                    r?.Reveal(false);
            }
        }

        // swap sets
        _revealedPrevFrame.Clear();
        foreach (var r in _revealedThisFrame) _revealedPrevFrame.Add(r);
    }

    // Gizmos para depurar el cono
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);

        Vector3 o = transform.position;
        Vector3 f = transform.forward;
        float half = spotAngle * 0.5f;

        // dibuja bordes del cono
        Quaternion left = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(half, Vector3.up);
        Vector3 l = left * f;
        Vector3 r = right * f;
        Gizmos.DrawLine(o, o + l * radius);
        Gizmos.DrawLine(o, o + r * radius);
    }
}
