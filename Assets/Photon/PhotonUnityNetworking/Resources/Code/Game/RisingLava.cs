using UnityEngine;

public class RisingLava : MonoBehaviour
{
    public float riseSpeed = 1f;

    void Update()
    {
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;
    }

    
}
