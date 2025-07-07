using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using ExitGames.Client.Photon;

public class UI_Lobby : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI NickNameTextUI;
    public TMP_InputField RoomNameInputField;
    public TMP_InputField PasswordInputField;

    public GameObject RoomPanel;

    private void Start()
    {
        NickNameTextUI.text = AccountManager.Instance.CurrentAccount.Nickname;
    }

    public void OpenRoomPanel()
    {
        RoomPanel.SetActive(true);
    }

    public void CloseRoomPanel()
    {
        RoomPanel.SetActive(false);
    }

    public void CreateRoom()
    {
        Debug.Log("=== 방 생성 시작 ===");

        // 연결 상태 확인
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Photon에 연결되지 않았습니다.");
            return;
        }

        Debug.Log($"연결 상태: {PhotonNetwork.NetworkClientState}");

        // 로비에 참가하지 않았다면 참가
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("로비에 참가합니다.");
            PhotonNetwork.JoinLobby();
            return;
        }

        Debug.Log("로비에 이미 참가되어 있습니다.");

        string roomName = RoomNameInputField.text + "_" + (int)Time.time;
        Debug.Log($"방 이름: {roomName}");

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true, // 반드시 명시적으로 true로 설정
            IsOpen = true,
            CleanupCacheOnLeave = true, // 반드시 명시적으로 true로 설정

            CustomRoomProperties = new Hashtable
            {
                { "RoomState", (int)ERoomState.Waiting }
            },
            CustomRoomPropertiesForLobby = new string[] { "RoomState" }
        };
        bool result = PhotonNetwork.CreateRoom(roomName,
            options, TypedLobby.Default);

        Debug.Log($"방 생성 요청 결과: {result}");

        if (result)
        {
            CloseRoomPanel();
        }
        else
        {
            Debug.LogError("방 생성 요청이 실패했습니다.");
        }
    }
}
