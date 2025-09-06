using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CashRegisterInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "Incassa (E)";
    [SerializeField] CashRegister cashRegister;

    void Awake()
    {
        if (!cashRegister)
            cashRegister = GetComponent<CashRegister>() ?? GetComponentInParent<CashRegister>();
    }

    public string Prompt => prompt;

    public bool CanInteract(Interactor who)
    {
        return cashRegister && cashRegister.HasServeableCustomer();
    }

    public void Interact(Interactor who)
    {
        cashRegister?.TryCheckout();
    }
}
