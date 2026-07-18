using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using LLama;
using LLama.Common;

public class UnityLLMContextHasher : MonoBehaviour
{
    private Dictionary<GUID, NPCContext_intf> npcContext= new Dictionary<GUID, NPCContext_intf>();
    public static UnityLLMContextHasher Instance { get; private set; }
    private bool applicationOver = false;
    public void Awake()
    {
        Instance = this;
    }

    public bool containsNPC(GUID npcID){
        return npcContext.ContainsKey(npcID);
    }   

    public NPCContext_intf getNPCContext(GUID npcID){
        
        if(containsNPC(npcID)){
            return npcContext[npcID];
        }
        return null;
    }

    private void displayHashedNPCs(){
        foreach (KeyValuePair<GUID, NPCContext_intf> kvp in npcContext){
            Debug.Log("Key: " + kvp.Key + " Value: " + kvp.Value.SystemPrompt);
        }
    }

    void OnApplicationQuit(){
        foreach (KeyValuePair<GUID, NPCContext_intf> kvp in npcContext){
            kvp.Value.Close();
        }
        npcContext.Clear();
        applicationOver = true;
        Debug.Log("Hasher cleared");
    }

    // private void Update()
    // {
    // }

    public bool HashNPC(GUID npcID, NPCContext_intf npcContextEntry)
    {
        //Establish connection and then hash the NPC with clientID
        if (applicationOver || this == null || gameObject == null)
        {
            if (npcContext.Count != 0)
            {
                foreach (KeyValuePair<GUID, NPCContext_intf> kvp in npcContext)
                {
                    kvp.Value.Close();
                }
                npcContext.Clear();
            }
            return false; // Application is quitting or object is destroyed
        }
        if (!containsNPC(npcID))
        {
            Debug.Log("Hashing NPC with ID: " + npcID);
            Debug.Log("Client hashed with NPC context: " + npcContextEntry.SystemPrompt);

            npcContext[npcID] = npcContextEntry;
            return true;
        }
        return false;
    }
}
