using System.Collections.Generic;
using CardGame;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class UI_RoomList : MonoBehaviourPunCallbacks
{
    public GameObject RoomPrefab;
    public GameObject RoomContainer;

    private TMPro.TextMeshProUGUI _emptyLabel;

    // 열린 방이 없을 때 안내 문구
    private void SetEmptyVisible(bool empty)
    {
        if (_emptyLabel == null)
        {
            _emptyLabel = UIKit.Text(transform, UI_MatchHud.FindSceneFont(), "", 40, UI_MatchHud.InkColor);
            UIKit.Stretch(_emptyLabel.rectTransform, 60, 60, 60, 60);
            _emptyLabel.text = "<b>열린 방이 없어요</b>\n<size=70%>오른쪽 '빠른 대전'으로 바로 상대를 찾거나\n'방만들기'로 친구를 기다려 보세요</size>";
        }
        _emptyLabel.gameObject.SetActive(empty);
    }

    private void Start() => SetEmptyVisible(RoomContainer == null || RoomContainer.transform.childCount == 0);

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
        SetEmptyVisible(RoomContainer.transform.childCount == 0 ||
                        roomList.TrueForAll(i => i.RemovedFromList || !i.IsVisible || !i.IsOpen));
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
