using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public Animator animator;
    private int keyCount = 0;
    [SerializeField] int numKeys = 4;

    public void Increment() {
        keyCount++;
        if (keyCount == numKeys) {
            Debug.Log("animation playing");
            animator.SetTrigger("OpenDoor");
        }
    }
}
