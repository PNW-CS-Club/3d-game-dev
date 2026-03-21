using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InputDebug : MonoBehaviour
{
    public bool isVisible = false;
    private TMP_Text textBox;

    void Start() {
        textBox = GetComponent<TMP_Text>();
    }
    
    void Update() {
        if (Keyboard.current.backquoteKey.wasPressedThisFrame) {
            isVisible = !isVisible;
        }
        if (!isVisible) {
            textBox.text = "";
            return;
        }
        
        float fps = 1 / Time.deltaTime;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        textBox.text = $"FPS: {fps:F0}\nMouse Delta: ({mouseDelta.x:F0}, {mouseDelta.y:F0})";
    }
}
