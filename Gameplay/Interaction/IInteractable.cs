public interface IInteractable
{
    bool CanInteract(Interactor who);
    void Interact(Interactor who);
    string Prompt { get; }
}
