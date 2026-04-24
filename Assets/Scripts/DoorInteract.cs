using UnityEngine;

public class DoorInteract : MonoBehaviour
{
    public Animator animator;
    private bool isOpen = false;

    public void Interact()
    {
        isOpen = !isOpen;
        animator.SetBool("isOpen", isOpen);
    }
}