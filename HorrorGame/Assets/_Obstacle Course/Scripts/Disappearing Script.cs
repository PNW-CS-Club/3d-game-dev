using System.Drawing.Text;
using UnityEngine;

public class TogglePlatform : MonoBehaviour
{
    public bool startOn = false;
    [SerializeField] private GameObject objectToTouch; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetAllChildren(startOn);

        if (objectToTouch != null)
        {
            TriggerWatcher watcher = objectToTouch.AddComponent<TriggerWatcher>();
            watcher.targetScript = this;
        }
    }
    
    public void Activate()
    {
        SetAllChildren(!startOn);
    }

    void SetAllChildren(bool state)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(state);
        }
    }
   public class TriggerWatcher : MonoBehaviour{
    public TogglePlatform targetScript;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetScript.Activate();
        }
    }
   }

    
}
