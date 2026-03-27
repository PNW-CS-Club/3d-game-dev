using UnityEngine;
using Random = UnityEngine.Random;
using Mirror;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    public GameObject prefab;
    public int numCollected = 0;

    void Start()
    {
		if (!isServer) return;

		// indices = [0 .. childcount-1]
		int[] indices = new int[transform.childCount];
		for (int i = 0; i < transform.childCount; i++) {
			indices[i] = i;
		}

		int[] choices = PickSome(indices, 3);
		foreach (int index in choices)
		{
			var child = transform.GetChild(index);
			var pos = child.position + Vector3.up * 1.0f;
			// Instantiate the prefab locally on the server
			GameObject spawnedObject = Instantiate(prefab, pos, Quaternion.identity);
			// Spawn it on the network so all clients see it
			NetworkServer.Spawn(spawnedObject);
		}
    }
    
    public void PickUp(ItemPickup item) 
    {
	    // only the server is allowed to recognize an item pickup event
	    if (!isServer) return;
	    
	    numCollected++;
	    Debug.Log("numCollected = " + numCollected);
	    NetworkServer.Destroy(item.gameObject);
    }

	static int[] PickSome(int[] array, int numToChoose) 
	{
		List<int> list = new List<int>(array.Length);
		foreach (int val in array) {
			list.Add(val);
		}
		
		int n = Mathf.Min(array.Length, numToChoose);
		int[] result = new int[n];
		for (int i = 0; i < n; i++) {
			int randomIndex = Random.Range(0, list.Count);
			result[i] = list[randomIndex];
			list.RemoveAt(randomIndex);
		}

		return result;
	}
}
