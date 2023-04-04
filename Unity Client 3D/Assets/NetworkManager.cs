using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    static public UdpClient client;
    static IPEndPoint ep;
    static UdpState state;
    public TextMeshProUGUI txt;

    [SerializeField] GameObject networkAvatar;

    List<NetworkGameObject> netObjects;
    public List<NetworkGameObject> worldState;

    public string receiveString = "";

    string ipAddress = "100.76.113.15";

    void Start()
    {
        netObjects = new List<NetworkGameObject>();
        netObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

        worldState = new List<NetworkGameObject>();
        worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

        client = new UdpClient();
        ep = new IPEndPoint(IPAddress.Parse(ipAddress), 9050);
        client.Connect(ep);

        state = new UdpState();
        state.u = client;
        state.e = ep;

        string myMessage = "FirstEntrance";
        byte[] array = Encoding.ASCII.GetBytes(myMessage);
        client.Send(array, array.Length);

        client.BeginReceive(ReceiveAsyncCallback, state);

        RequestUIDs();
        StartCoroutine(SendNetworkUpdates());
        StartCoroutine(updateWorldState());
    }

    void ReceiveAsyncCallback(IAsyncResult result)
    {
        byte[] receiveBytes = client.EndReceive(result, ref ep);
        receiveString = Encoding.ASCII.GetString(receiveBytes);

        if (receiveString.Contains("Assigned UID:"))
        {
            int parseFrom = receiveString.IndexOf(':');
            int parseTo = receiveString.LastIndexOf(';');
            int localID = Int32.Parse(betweenStrings(receiveString, ":", ";"));
            int globalID = Int32.Parse(receiveString.Substring(receiveString.IndexOf(";") + 1));

            Debug.Log("Got assignment: " + localID + " local to: " + globalID + " global");

            foreach (NetworkGameObject netObject in netObjects)
            {
                if (netObject.localID == localID)
                {
                    netObject.uniqueNetworkID = globalID;
                }
            }
        }

        client.BeginReceive(ReceiveAsyncCallback, state);
    }
    // Update is called once per frame
    void Update()
    {
       
    }

    void RequestUIDs()
    {
        netObjects = new List<NetworkGameObject>();
        netObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

        foreach (NetworkGameObject netObject in netObjects)
        {
            if (netObject.isLocallyOwned && netObject.uniqueNetworkID == 0)
            {
                string myMessage = "I need a UID for local object:" + netObject.localID;
                byte[] array = Encoding.ASCII.GetBytes(myMessage);
                client.Send(array, array.Length);
            }
        }
    }

    public static String betweenStrings(String text, String start, String end)
    {
        int p1 = text.IndexOf(start) + start.Length;
        int p2 = text.IndexOf(end, p1);

        if (end == "") return (text.Substring(p1));
        else return text.Substring(p1, p2 - p1);
    }

    IEnumerator SendNetworkUpdates()
    {
        while (true)
        {
            List<NetworkGameObject> netObjects = new List<NetworkGameObject>();
            netObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

            foreach (NetworkGameObject netObject in netObjects)
            {
                if (netObject.isLocallyOwned && netObject.uniqueNetworkID != 0)
                {
                    client.Send(netObject.toPacket(), netObject.toPacket().Length);
                }
            }

            yield return new WaitForSeconds(0.8f);
        }
    }

    int GetGlobalIDFromPacket(String packet)
    {
        return Int32.Parse(packet.Split(';')[1]);
    }

    IEnumerator updateWorldState()
    {
        while (true)
        {
            //read in the current world state as all network game objects in the scene
            worldState = new List<NetworkGameObject>();
            worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

           
            string previousRecieveString = receiveString;
            
            

            if (previousRecieveString.Contains("Id:;"))
            {
                Debug.Log("got it");
                    //for every networked gameobject in the world
                foreach (NetworkGameObject ngo in worldState)
                {

                    if (ngo.uniqueNetworkID == GetGlobalIDFromPacket(previousRecieveString) || ngo.uniqueNetworkID == 0)
                    {
                        ngo.ChangeHp(previousRecieveString);
                    }

                }
                    
            }
            if (previousRecieveString.Contains("Object data;"))
            {
                //we'll want to know if an object with this global id is already in the game world
                bool objectIsAlreadyInWorld = false;

                //we'll also want to exclude any invalid packets with a bad global id
                if (GetGlobalIDFromPacket(previousRecieveString) != 0)
                {
                    //for every networked gameobject in the world
                    foreach (NetworkGameObject ngo in worldState)
                    {
                        //if it's unique ID matches the packet, update it's position from the packet
                        if (ngo.uniqueNetworkID == GetGlobalIDFromPacket(previousRecieveString) || ngo.uniqueNetworkID == 0)
                        {
                            //only update it if we don't own it 
                            if (!ngo.isLocallyOwned)
                            {
                                ngo.fromPacket(previousRecieveString);

                            }
                            //if we have any uniqueID matches, our object is in the world
                            objectIsAlreadyInWorld = true;
                        }

                    }

                    //if it's not in the world, we need to spawn it
                    if (!objectIsAlreadyInWorld)
                    {
                        int idOfNewObject = GetGlobalIDFromPacket(previousRecieveString);
                        bool isObjectAlreadyInstantiated = false;
                        for (int i = 0; i < worldState.Count; i++)
                        {
                            if (worldState[i].uniqueNetworkID == idOfNewObject)
                            {
                                isObjectAlreadyInstantiated = true;
                            }
                        }

                        if (!isObjectAlreadyInstantiated)
                        {
                            GameObject otherPlayerAvatar = Instantiate(networkAvatar);
                            //update its component properties from the packet
                            otherPlayerAvatar.GetComponent<NetworkGameObject>().uniqueNetworkID = GetGlobalIDFromPacket(previousRecieveString);
                            otherPlayerAvatar.GetComponent<NetworkGameObject>().fromPacket(previousRecieveString);
                        }
                    }
                }

            }

            //wait until the endOfFrame if the udp available equals 0
            if (state.u.Available == 0)
                yield return new WaitForEndOfFrame();
           
            
            
        }
    }    

}

