using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UI_Room : MonoBehaviour
{
    public TextMeshProUGUI RoomTitleTextUI;
    public TextMeshProUGUI RoomPersonTextUI;
    public TextMeshProUGUI RoomStatusTextUI;

    private RoomInfo _roomInfo;

    public void Refresh(RoomInfo roomInfo)
    {
        _roomInfo = roomInfo;
        string[] RoomTitleTexts = roomInfo.Name.Split("_");
        RoomTitleTextUI.text = RoomTitleTexts[0];
        RoomPersonTextUI.text = $"{roomInfo.PlayerCount} / {roomInfo.MaxPlayers}";

        var roomState = ERoomState.Waiting; // 기본값
        if (roomInfo.CustomProperties.TryGetValue("RoomState", out var stateObject))
        {
            if (stateObject is int stateInt)
            {
                roomState = (ERoomState)stateInt;
            }
        }

        RoomStatusTextUI.text = ChangeStateToKR(roomState);
    }
    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(_roomInfo.Name);
    }

    private string ChangeStateToKR(ERoomState roomState)
    {
        switch (roomState)
        {
            case ERoomState.Waiting:
                return "대기 중";
            case ERoomState.Playing:
                return "게임 중";
            default:
                return "잘못된 상태";
        }
    }
}
