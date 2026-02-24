using UnityEngine;

public class RandomSpin : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float spinDuration = 1f;
    [SerializeField] float spinSpeed = 180f;
    
    private float timer = 0f;
    private Vector3 spinAxis = Vector3.right;

    void Update()
    {
        transform.RotateAround(transform.position, spinAxis, spinSpeed * Time.deltaTime);
        
        timer += Time.deltaTime;
        if (timer > spinDuration) {
            timer = 0f;
            spinAxis = Random.onUnitSphere;
        }
    }
}
