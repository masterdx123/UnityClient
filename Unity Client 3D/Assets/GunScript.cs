using System.Text;
using UnityEngine;



public class GunScript : MonoBehaviour
{
    int dmg = 10;
    float range = 30;

    public Camera cam;
    static public NetworkGameObject netObject;
    void Update()
    { 
        //Id mouse left was clicked
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
            
        }
    }

    void Shoot()
    {
        RaycastHit hit;
        
        //perfrom a raycast forward from the player position to hit a tasrget at a certain range
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit , range))
        {
            Debug.Log("inRange");
            

            //if the object hit is a network gameobject than send an information to the server for that player to lose hp
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
