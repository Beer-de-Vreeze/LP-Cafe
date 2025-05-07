using UnityEngine;

public class Interfaces : Singleton<Interfaces>

{
    public interface IInteractable
    {
        void Interact();
    }
}
