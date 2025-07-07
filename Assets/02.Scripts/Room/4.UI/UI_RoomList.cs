using System.Collections.Generic;
using CardGame;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class UI_RoomList : MonoBehaviourPunCallbacks
{
    public GameObject RoomPrefab;
    public GameObject RoomContainer;

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"OnRoomListUpdate 호출됨. 방 개수: {roomList.Count}");


        foreach(Transform child in RoomContainer.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo info in roomList)
        {
            if(info.RemovedFromList || !info.IsVisible || !info.IsOpen)
            {
                continue;
            }

            Debug.Log($"{info.Name} | RemovedFromList: {info.RemovedFromList}, Visible: {info.IsVisible}, Open: {info.IsOpen}");
            GameObject roomObject = Instantiate(RoomPrefab, RoomContainer.transform);
            Room room = new Room(info.Name, "", ERoomState.Waiting, info.MaxPlayers, info.PlayerCount);
            UI_Room roomComp = roomObject.GetComponent<UI_Room>();
            roomComp.Refresh(room);
        }
    }
    private void Awake()
    {
        Debug.Log("UI_RoomList: Awake 호출됨");
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        Debug.Log("UI_RoomList: OnEnable 호출됨");

        // 로비 재참가 (이미 로비 안에 있어도 OK)
        // if (PhotonNetwork.InLobby)
        // {
        //     PhotonNetwork.LeaveLobby();
        //     PhotonNetwork.JoinLobby();
        // }
        // else
        // {
        //     PhotonNetwork.JoinLobby();
        // }
    }
}
