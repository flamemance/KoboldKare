using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Kobold))]
public class KoboldInventory : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback {
    private Dictionary<Equipment, List<GameObject[]>> equipmentDisplays = new Dictionary<Equipment, List<GameObject[]>>();
    private static List<Equipment> staticIncomingEquipment = new List<Equipment>();
    public int Count => equipment.Count;
    private List<Equipment> equipment = new List<Equipment>();
    public delegate void EquipmentChangedEvent(List<Equipment> newEquipment);
    public EquipmentChangedEvent equipmentChanged;
    private Kobold kobold;
    void Awake() {
        kobold = GetComponent<Kobold>();
    }
    public List<Equipment> GetAllEquipment() {
        return new List<Equipment>(equipment);
    }
    public int GetEquipmentInstanceCount(Equipment thing) {
        int count = 0;
        for(int i=0;i<equipment.Count;i++) {
            if (equipment[i] == thing) {
                count++;
            }
        }
        return count;
    }
    public void PickupEquipment(Equipment thing, GameObject groundPrefab) {
        equipment.Add(thing);
        GameObject[] displays = thing.OnEquip(kobold, groundPrefab);

        // Remember the created objects
        if (!equipmentDisplays.ContainsKey(thing)) {
            equipmentDisplays[thing] = new List<GameObject[]>();
        }
        equipmentDisplays[thing].Add(displays);

        equipmentChanged?.Invoke(equipment);
    }
    public void RemoveEquipment(Equipment thing, bool dropOnGround) {
        equipment.Remove(thing);
        thing.OnUnequip(kobold, dropOnGround);

        // Destroy the created objects
        foreach(GameObject obj in equipmentDisplays[thing][0]) {
            Destroy(obj);
        }
        equipmentDisplays[thing].RemoveAt(0);

        equipmentChanged?.Invoke(equipment);
    }
    void ReplaceEquipmentWith(List<Equipment> newEquipment) {
        bool same = newEquipment.Count == equipment.Count;
        for(int i=0;i<equipment.Count&&same;i++) {
            if (equipment[i] != newEquipment[i]) {
                same = false;
            }
        }
        if (same) {
            return;
        }
        while(equipment.Count != 0) {
            RemoveEquipment(equipment[0], false);
        }
        foreach(Equipment e in newEquipment) {
            PickupEquipment(e, null);
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext((short)equipment.Count);
            foreach(Equipment e in equipment) {
                stream.SendNext(EquipmentDatabase.GetID(e));
            }
        } else {
            short equipmentCount = (short)stream.ReceiveNext();
            staticIncomingEquipment.Clear();
            for(int i=0;i<equipmentCount;i++) {
                staticIncomingEquipment.Add(EquipmentDatabase.GetEquipment((short)stream.ReceiveNext()));
            }
            ReplaceEquipmentWith(staticIncomingEquipment);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        if (info.photonView.InstantiationData == null || !(info.photonView.InstantiationData[0] is ExitGames.Client.Photon.Hashtable)) {
            return;
        }
        ExitGames.Client.Photon.Hashtable s = (ExitGames.Client.Photon.Hashtable)info.photonView.InstantiationData[0];
        if (!s.ContainsKey("EquippedItems")) {
            return;
        }
        short[] equipmentList = (short[])s["EquippedItems"];

        staticIncomingEquipment.Clear();
        foreach(var id in equipmentList) {
            staticIncomingEquipment.Add(EquipmentDatabase.GetEquipment(id));
        }
        ReplaceEquipmentWith(staticIncomingEquipment);
    }
}
