using UnityEngine;

[DisallowMultipleComponent]
public class SimpleSeparation2D : MonoBehaviour
{
    [Tooltip("Raggio di 'spazio personale'")]
    public float radius = 0.5f;
    [Tooltip("Intensità spinta")]
    public float force = 1.2f;
    [Tooltip("Spostamento massimo per frame")]
    public float maxStepPerFrame = 0.02f;
    [Tooltip("Considera al massimo N vicini più prossimi")]
    public int maxNeighbors = 4;
    [Tooltip("Ignora distanze minuscole per evitare jitter")]
    public float deadZone = 0.08f;
    [Tooltip("Limita l'effetto ai soli Customer")]
    public bool onlyAffectCustomers = true;

    static readonly Collider2D[] _buf = new Collider2D[32];
    Transform _tr;
    Vector2 _bias;

    void Awake()
    {
        _tr = transform;
        _bias = Random.insideUnitCircle * 0.015f; // rompe le simmetrie
    }

    void LateUpdate()
    {
        Vector2 p = _tr.position;
        int count = Physics2D.OverlapCircleNonAlloc(p, radius, _buf);
        if (count <= 1) return;

        // accumula dalle 4 entità più vicine (ordine non garantito, ma va bene)
        int used = 0;
        Vector2 repel = Vector2.zero;

        for (int i = 0; i < count && used < maxNeighbors; i++)
        {
            var c = _buf[i];
            if (!c) continue;
            if (c.attachedRigidbody && c.attachedRigidbody.transform == _tr) continue;
            if (onlyAffectCustomers && !c.GetComponentInParent<CustomerController>()) continue;

            Vector2 delta = p - (Vector2)c.transform.position;
            float d = delta.magnitude;
            if (d <= deadZone) continue;

            float w = 1f - Mathf.Clamp01(d / radius); // 0..1
            w *= w; // ammorbidisce (quadratico)
            repel += (delta / Mathf.Max(d, 1e-4f)) * w;
            used++;
        }

        if (repel.sqrMagnitude > 0f)
        {
            Vector2 step = repel.normalized * force * Time.deltaTime + _bias;
            if (step.magnitude > maxStepPerFrame)
                step = step.normalized * maxStepPerFrame;

            _tr.position = (Vector2)_tr.position + step;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
