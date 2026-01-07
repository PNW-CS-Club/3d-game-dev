using System.Reflection;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public Animation Animation_here;

    void OnTriggerStay()
    {
        if (Input.GetKey(KeyCode.E))
        {
            Animation_here.Play();
        }
    }
}
