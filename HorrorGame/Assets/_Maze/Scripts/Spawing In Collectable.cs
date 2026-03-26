using UnityEngine;

public class SpawingInCollectable : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private GameObject collectable;


    void Start()
    {
        spawnObjectIn(collectable);
    }

    void spawnObjectIn(GameObject collectable)
    {
        Vector3 spawnpoint = new Vector3(23, 1, (float)-16.5);
        Instantiate(collectable, spawnpoint, Quaternion.identity, transform);
    }
}
