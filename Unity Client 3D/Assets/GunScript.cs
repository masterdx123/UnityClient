using Unity.VisualScripting;
using UnityEngine;

public class GunScript : MonoBehaviour
{
    public float dmg = 0;
    public float range = 0;

    public Camera cam;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit hit;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit , range))
        {
            Debug.Log(hit.transform.name);

            NetworkGameObject netObject = hit.transform.GetComponent<NetworkGameObject>();
            if (netObject != null)
            {
                Debug.Log(netObject.transform.name);
            }
        }
    }
}
