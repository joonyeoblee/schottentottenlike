using System.Collections.Generic;
using System.Linq;
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
            if (PhotonNetwork.IsMasterClient)
            {
                _battleField = PhotonNetwork.InstantiateSceneObject("Field", new Vector3(0, 0, 0), Quaternion.identity);
                Debug.Log("방장이므로 Field 생성됨.");
            }
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

    // public override void OnMasterClientSwitched(Player newMasterClient)
    // {
    //     Debug.Log("마스터 클라이언트 변경됨");
    //
    //     if (newMasterClient == PhotonNetwork.LocalPlayer)
    //     {
    //         // 필드가 이미 생성되어 있으면 새로 생성하지 않음
    //         if (_battleField == null)
    //         {
    //             // _battleField = PhotonNetwork.Instantiate("Field", new Vector3(0, 5, 0), Quaternion.identity);
    //             Debug.Log("새로운 마스터가 필드를 재생성함.");
    //
    //             // 소유권을 새로운 마스터에게 넘기기
    //             PhotonView photonView = _battleField.GetComponent<PhotonView>();
    //             if (photonView != null)
    //             {
    //                 photonView.TransferOwnership(newMasterClient);
    //                 Debug.Log("새로운 마스터에게 필드 소유권을 이전.");
    //             }
    //         }
    //         else
    //         {
    //             Debug.Log("필드가 이미 존재하므로 재생성하지 않음.");
    //         }
    //     }
    // }

}
