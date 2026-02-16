using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyScript : MonoBehaviour
{
    public DoorScript doorScript; //this is the reference variable that is being held for each of those keys
    public bool wasCollected = false;

    void OnTriggerStay(Collider other) //once you come in contact with the keys the references of the keys will then allow for the method to run which variables are in reference to that
    {
        if (other.CompareTag("Player") && !wasCollected) {
            wasCollected = true;
            Debug.Log(gameObject.name + " is collected");
            doorScript.Increment();
        }
    }
}
