using UnityEngine;

public class TestInteractible : MonoBehaviour, Interfaces.IInteractable
{
    public void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);
    }
}
