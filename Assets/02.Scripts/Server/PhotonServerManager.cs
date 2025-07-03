using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// Photon API 네임스페이

// 역할: 포톤 서버 관리자(서버 연결, 로비 입장, 방 입장, 게임 입장)
public class PhotonServerManager : SingletonPhoton<PhotonServerManager>
{
    // MonoBehaviourPunCallbacks : 유니티 이벤트 말고도 Pun 서버 이벤트를 받을 수 있다.
    private readonly string _gameVersion = "0.0.1";
    private AddressablesPool _pool = new  AddressablesPool();
    private bool _shouldSpawnField = false;

    private GameObject _battleField;
    private void Start()
    {
        // 설정

        // 0. 데이터 송수신 빈도를 매 초당 60회로 설정한다. (기본은 10)
        PhotonNetwork.SendRate = 60; // 선호하는 값이지 보장 X
        PhotonNetwork.SerializationRate = 60;
        // 1. 버전 : 버전이 다르면 다른 서버로 접속이 된다.
        PhotonNetwork.GameVersion = _gameVersion;



        //방장이 로드한 씬으로 다른 참여자가 똑같이 이동하게끔 동기화 해주는 옵션
        //방장 : 방을 만든 소유자이자 "마스터 클라이언트" (방마다 한명의 마스터 클라이언트가 존재)
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.PrefabPool = _pool;
        _pool.Preload("Field");
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");

        Debug.Log($"현재 방 이름: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"방 안의 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"플레이어: {player.NickName}, ActorNumber: {player.ActorNumber}");
        }
        _shouldSpawnField = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        PhotonNetwork.LoadLevel(2);

    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_shouldSpawnField && scene.buildIndex == 2)
        {
            _shouldSpawnField = false;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; // 리스너 해제
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {returnCode} - {message}");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공!");
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"플레이어 입장: {newPlayer.NickName}");

        if (PhotonNetwork.IsMasterClient)
        {
            BattleField.Instance.GameStart();
        }
    }

//     // 포톤 마스터 서버에 접속 후 호출되는 콜백 함수
    // public override void OnConnectedToMaster()
    // {
    //     Debug.Log("Connected to Master!");
    //     Debug.Log(PhotonNetwork.CloudRegion);
    //     Debug.Log($"Is in Lobby: {PhotonNetwork.InLobby}"); // 로비 입장 유무
    //
    //     PhotonNetwork.JoinLobby();
    //     //PhotonNetwork.JoinLobby(TypedLobby.Default);
    // }
    //
    // public override void OnJoinedLobby()
    // {
    //     Debug.Log("로비 (채널) 입장 완료!");
    //     Debug.Log($"Is in Lobby: {PhotonNetwork.InLobby}"); // 로비 입장 유무
    // }

//     // 랜덤 룸 입장에 실패했을 경우 호출되는 콜백 함수
//     public override void OnJoinRandomFailed(short returnCode, string message)
//     {
//         Debug.Log($"랜덤방 입장에 실패 했습니다 {returnCode}:{message}");

//         // 룸 속성 정의
//         RoomOptions roomOptions = new RoomOptions();
//         roomOptions.MaxPlayers = 20; // 룸에 입장할 수 있는 최대 접속자 수
//         roomOptions.IsOpen = true; // 룸의 오픈 여부
//         roomOptions.IsVisible = true; // 로비에서 룸 목록에 노출시킬지 여부

//         // 룸 생성
//         // PhotonNetwork.CreateRoom("test", roomOptions);
//         // 룸 입장 또는 생성
//         // PhotonNetwork.JoinOrCreateRoom("test", roomOptions, TypedLobby.Default);
//     }

//     // 룸에 입장한 후 호출되는 콜백 함수

//     // 룸 생성에 실패하면 호출되는 콜백 함수
//     public override void OnCreateRoomFailed(short returnCode, string message)
//     {
//         Debug.Log($"CreatRoom Failed {returnCode}:{message}");
//     }

//     // 룸 생성이 성공했을 때 호출되는 콜백 함수
//     public override void OnCreatedRoom()
//     {
//         Debug.Log("Created Room");
//         // 생성된 룸 이름 확인
//         Debug.Log($"Room Name = {PhotonNetwork.CurrentRoom.Name}");
//     }
}
