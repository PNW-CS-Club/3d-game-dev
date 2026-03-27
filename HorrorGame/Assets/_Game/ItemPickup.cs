using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [HideInInspector] public ItemSpawner spawner;

    void Start() {
        spawner = FindFirstObjectByType<ItemSpawner>();
    }
    
    void OnTriggerEnter(Collider other) {
        spawner.PickUp(this);
    }
}
