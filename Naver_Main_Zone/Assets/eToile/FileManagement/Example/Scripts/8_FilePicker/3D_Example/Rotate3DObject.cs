using UnityEngine;

public class Rotate3DObject : MonoBehaviour
{
    public bool active = false;
    float rotationSpeed = 0.75f;

    void OnMouseDrag()
    {
        if(active)
        {
            float XaxisRotation = Input.GetAxis("Mouse X") * rotationSpeed;
            float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSpeed;
            // select the axis by which you want to rotate the GameObject
            transform.Rotate(Vector3.down, XaxisRotation);
            transform.Rotate(Vector3.right, YaxisRotation);
        }
    }

    public void Restart()
    {
        transform.rotation = Quaternion.identity;
    }
}
