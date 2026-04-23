using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public float distance = 0.8f;
    public float smoothSpeed = 5f;

    void Update()
    {
        

        Vector3 targetPos = cameraTransform.position + 
                           cameraTransform.forward * distance;
        
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPos, 
            Time.deltaTime * smoothSpeed
        );
        
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(transform.position - cameraTransform.position),
            Time.deltaTime * smoothSpeed
        );
    }
}