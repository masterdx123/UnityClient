using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
public class NetworkGameObject : MonoBehaviour
{
    [SerializeField] public bool isLocallyOwned;
    [SerializeField] public int uniqueNetworkID;
    [SerializeField] public int localID;
    static int lastAssignedLocalID = 0;

    private void Awake()
    {
        localID = lastAssignedLocalID++;
    }

    public byte[] toPacket() //convert the relevant info on the gameobject to a packet
    {
        //create a delimited string with the required data
        //note if we put strings in this we might want to check they don’t have a semicolon or use a different delimiter like |
        string returnVal = uniqueNetworkID + ";" +
                            transform.position.x + ";" +
                            transform.position.y + ";" +
                            transform.position.z + ";" +
                            transform.rotation.x + ";" +
                            transform.rotation.y + ";" +
                            transform.rotation.z + ";" +
                            transform.rotation.w + ";"
                            ;
        return Encoding.ASCII.GetBytes(returnVal);
    }

    public void fromPacket(byte[] packet) //convert a packet to the relevant data and apply it to the gameobject properties
    {
        string data = Encoding.ASCII.GetString(packet);
        string[] values = data.Split(';');
        transform.position = new Vector3(Int32.Parse(values[1]), Int32.Parse(values[2]), Int32.Parse(values[3]));
        transform.rotation = new Quaternion(Int32.Parse(values[4]), Int32.Parse(values[5]), Int32.Parse(values[6]), Int32.Parse(values[7]));
    }
}
