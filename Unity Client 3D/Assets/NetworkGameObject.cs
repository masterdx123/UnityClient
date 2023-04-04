using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using UnityEngine.UI;
using TMPro;

public class NetworkGameObject : MonoBehaviour
{
    [SerializeField] public bool isLocallyOwned;
    [SerializeField] public int uniqueNetworkID;
    [SerializeField] public int localID;
    [SerializeField] public int HP = 100;
    public TextMeshProUGUI HpText;
    static int lastAssignedLocalID = 0;

    private void Awake()
    {
        //assign local id
        if (isLocallyOwned) localID = lastAssignedLocalID++;

        if (isLocallyOwned)
            HpText.text = "Hp: 100";
    }
    public byte[] toPacket() //convert the relevant info on the gameobject to a packet
    {
        string a = gameObject.name;
        //return a string with all the object data to the server
       
        string returnVal = "Object data;" + uniqueNetworkID + ";" +
                            transform.position.x * 100 + ";" +
                            transform.position.z * -100 + ";" +
                            transform.position.y * 100 + ";" +
                            transform.rotation.x + ";" +
                            transform.rotation.z + ";" +
                            transform.rotation.y + ";" +
                            transform.rotation.w + ";" +
                            HP + ";"
                            ;
        return Encoding.ASCII.GetBytes(returnVal);
    }

    public void fromPacket(string packet) //convert a packet to the relevant data and apply it to the gameobject properties
    {
        string[] values = packet.Split(';');
        transform.position = new Vector3(float.Parse(values[2]) / 100, float.Parse(values[4]) / 100, float.Parse(values[3]) / -100);
        transform.rotation = new Quaternion(float.Parse(values[5]), float.Parse(values[7]), float.Parse(values[6]), float.Parse(values[8]));
    }

    public void ChangeHp(string packet) //change the hp of the object when taking dmg
    {
        string[] values = packet.Split(';');
        int dmg = int.Parse(values[2]);
        HP -= dmg;
        Debug.Log(this.uniqueNetworkID + "/" + this.HP);
        HpText.text = "Hp: " + HP;
    }

}
