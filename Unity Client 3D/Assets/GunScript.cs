using System.Text;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;



public class GunScript : MonoBehaviour
{
    int dmg = 10;
    float range = 30;

    public Camera cam;
    static public NetworkGameObject netObject;
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
            Debug.Log("inRange");
            

            netObject = hit.transform.GetComponent<NetworkGameObject>();
            if (netObject != null)
            {

                string losHP = "lose hp;" + netObject.uniqueNetworkID + ";" + dmg + ";";
                byte[] HPData = Encoding.ASCII.GetBytes(losHP);
                NetworkManager.client.Send(HPData, HPData.Length);

                Debug.Log(netObject.transform.name);
            }
        }
    }
}
