using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CashRegisterInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] CashRegister cashRegister;

    void Awake()
    {
        if (!cashRegister)
            cashRegister = GetComponent<CashRegister>() ?? GetComponentInParent<CashRegister>();
    }

    public string Prompt
    {
        get
        {
            if (!cashRegister) return "";
            if (cashRegister.HasServeableCustomer()) return "Incassa (E)";
            if (cashRegister.HasTalkableHead()) return "Parla (E)";
            return "";
        }
    }

    public bool CanInteract(Interactor who)
        => cashRegister && (cashRegister.HasServeableCustomer() || cashRegister.HasTalkableHead());

    public void Interact(Interactor who)
        => cashRegister?.InteractPrimary();
}
