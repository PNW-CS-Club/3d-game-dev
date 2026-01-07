using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyScript : MonoBehaviour
{
    public Component DoorCollider;

    void OnTriggerStay()
    {
        if (Input.GetKey(KeyCode.E))
        {
            DoorCollider.GetComponent<BoxCollider>().enabled = true;
        }
    }
}
