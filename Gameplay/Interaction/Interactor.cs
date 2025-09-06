using System;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] float radius = 0.9f;
    [SerializeField] LayerMask interactableMask;
    readonly Collider2D[] _hits = new Collider2D[8];

    public event Action<string> OnPromptChanged;

    Collider2D _current;

    void Update()
    {
        RefreshTarget();

        if (Input.GetKeyDown(KeyCode.E) && _current &&
            _current.TryGetComponent<IInteractable>(out var target) &&
            target.CanInteract(this))
        {
            target.Interact(this);
        }
    }

    void RefreshTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _hits, interactableMask);

        Collider2D best = null; float bestD = float.MaxValue; string prompt = "";
        Vector2 myPos = (Vector2)transform.position;

        for (int i = 0; i < count; i++)
        {
            var h = _hits[i]; if (!h) continue;
            if (!h.TryGetComponent<IInteractable>(out var it)) continue;
            if (!it.CanInteract(this)) continue;

            // Usa l'API 2D: Collider2D.ClosestPoint -> Vector2
            Vector2 cp = h.ClosestPoint(myPos);
            float d = (cp - myPos).sqrMagnitude;

            if (d < bestD) { bestD = d; best = h; prompt = it.Prompt; }
        }

        if (best != _current)
        {
            _current = best;
            OnPromptChanged?.Invoke(_current ? prompt : "");
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
