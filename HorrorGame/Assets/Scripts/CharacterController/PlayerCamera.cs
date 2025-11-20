using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class PlayerCamera : NetworkBehaviour
{
    public GameObject cameraHolder;
    public Vector3 offset;

    public override void OnStartAuthority()
    {
        cameraHolder.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(SceneManager.GetActiveScene().name == "NetcodeTest")
        {
            cameraHolder.transform.position = transform.position + offset;
        }
        
    }
}
