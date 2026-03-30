using UnityEngine;
using UnityEngine.InputSystem;

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
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            isOn = !isOn;
            FlashLightLight.SetActive(isOn);
        }
    }
}
