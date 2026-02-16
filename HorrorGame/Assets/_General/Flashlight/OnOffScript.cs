using NUnit.Framework;
using UnityEngine;

public class OnOffScript : MonoBehaviour
{
    [SerializeField] GameObject FlashLightLight;
    private bool isOn = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FlashLightLight.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if(isOn == true)
            {
                FlashLightLight.gameObject.SetActive(false);
                isOn = false;
                Debug.Log("Flashlight is off");
            } else
            {
                FlashLightLight.gameObject.SetActive(true);
                isOn = true;
                Debug.Log("Flashlight is on");
            }
        }
    }
}
