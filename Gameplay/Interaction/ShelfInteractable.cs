using UnityEngine;

public class ShelfInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "Sistema (E)";
    [SerializeField, Range(0.05f, 0.25f)] float cleanBoost = 0.12f;
    [SerializeField] float cooldown = 6f;
    [SerializeField] ShelfState shelf; // sullo stesso GO

    float cd;

    void Awake() { if (!shelf) shelf = GetComponent<ShelfState>(); }

    public string Prompt => cd <= 0 ? prompt : "";
    public bool CanInteract(Interactor who) => cd <= 0;

    void Update() { if (cd > 0) cd -= Time.deltaTime; }

    public void Interact(Interactor who)
    {
        if (cd > 0) return;

        // pulizia globale
        StoreAmbience.I?.Clean(cleanBoost);

        // riordino SOLO dell'ordine (non tocca lo stock)
        shelf?.Tidy(cleanBoost);

        cd = cooldown;
    }
}
