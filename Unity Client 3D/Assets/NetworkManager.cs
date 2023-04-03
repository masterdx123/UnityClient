using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Net.Http.Headers;
using UnityEngine.Rendering;
using System.Net.NetworkInformation;
//using System.Diagnostics;

public class NetworkManager : MonoBehaviour
{
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    //System.Diagnostics.Stopwatch pingTimer = new System.Diagnostics.Stopwatch();
    static public UdpClient client;
    static IPEndPoint ep;
    static UdpState state;
    //TimeSpan timer = new TimeSpan();
    public TextMeshProUGUI txt;

    [SerializeField] GameObject networkAvatar;

    List<NetworkGameObject> netObjects;
    public List<NetworkGameObject> worldState;

    public string receiveString="";

    string ipAdress = "100.76.113.2"; 

    // Start is called before the first frame update
    void Start()
    {

        netObjects = new List<NetworkGameObject>();
        netObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

        worldState = new List<NetworkGameObject>();
        worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());


        client = new UdpClient();
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipAdress), 9050); // endpoint where server is listening (testing localy)
        client.Connect(ep);

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

        byte[] receiveBytes = client.EndReceive(result, ref ep); //get the packet
        receiveString = Encoding.ASCII.GetString(receiveBytes); //decode the packet
        //Debug.Log("Received " + receiveString + " from " + ep.ToString()); //display the packet

        if (receiveString.Contains("Assigned UID:"))
        {

            int parseFrom = receiveString.IndexOf(':');
            int parseTo = receiveString.LastIndexOf(';');

            //we need to parse the string from the server back into ints to work with
            int localID = Int32.Parse(betweenStrings(receiveString, ":", ";"));
            int globalID = Int32.Parse(receiveString.Substring(receiveString.IndexOf(";") + 1));

            Debug.Log("Got assignment: " + localID + " local to: " + globalID + " global");

            foreach (NetworkGameObject netObject in netObjects)
            {
                //if the local ID sent by the server matches this game object
                if (netObject.localID == localID)
                {
                    //Debug.Log(localID + " : " + globalID);
                    //the global ID becomes the server-provided value
                    netObject.uniqueNetworkID = globalID;
                }
            }
        }

        client.BeginReceive(ReceiveAsyncCallback, state); //self-callback, meaning this loops infinitely
        string myMessage2 =   "" /*"PlayerName: " + playerName + " / Ping: " + timer.Milliseconds*/ ;
        byte[] array2 = Encoding.ASCII.GetBytes(myMessage2);
        client.Send(array2, array2.Length);
    }


    // Update is called once per frame
    void Update()
    {
        //txt.text = "Ping: " + timer.Milliseconds;
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

            yield return new WaitForEndOfFrame();
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

            //cache the recieved packet string - we'll use that later to suspend the couroutine until it changes
            string previousRecieveString = receiveString;

            //if it's an object update, process it, otherwise skip
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
                        for(int i = 0; i < worldState.Count; i++)
                        {
                            if (worldState[i].uniqueNetworkID == idOfNewObject)
                            {
                                isObjectAlreadyInstantiated = true;
                            }
                        }

                        if(!isObjectAlreadyInstantiated)
                        { 
                            GameObject otherPlayerAvatar = Instantiate(networkAvatar);
                            //update its component properties from the packet
                            otherPlayerAvatar.GetComponent<NetworkGameObject>().uniqueNetworkID = GetGlobalIDFromPacket(previousRecieveString);
                            otherPlayerAvatar.GetComponent<NetworkGameObject>().fromPacket(previousRecieveString);
                        }
                    }
                }

            }

            if (previousRecieveString.Contains("Id:;"))
            {
                    //for every networked gameobject in the world
                foreach (NetworkGameObject ngo in worldState)
                {

                    if (ngo.uniqueNetworkID == GetGlobalIDFromPacket(previousRecieveString) || ngo.uniqueNetworkID == 0)
                    {
                        ngo.ChangeHp(previousRecieveString);
                    }

                }
                    //guardar num int o id que perdeu vida
                    //fazer um loop por todos os network gameobjects e se o id for o mm que o recebido
                    //atualizar o hp
            }

            //wait until the incoming string with packet data changes then iterate again

            yield return new WaitUntil(() => !receiveString.Equals(previousRecieveString));
        }
    }    

}

