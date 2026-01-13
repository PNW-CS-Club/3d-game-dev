using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyScript : MonoBehaviour
{
    public DoorScript doorScript;
    public bool wasCollected = false;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !wasCollected) {
            wasCollected = true;
            Debug.Log(gameObject.name + " is collected");
            doorScript.Increment();
        }
    }
}
