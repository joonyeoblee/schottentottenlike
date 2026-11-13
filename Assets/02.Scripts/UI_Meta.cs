using System;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Meta : MonoBehaviourPunCallbacks
{
    public void ExitRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("방 나가기");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // 솔로(AI) 모드: 방이 없으므로 바로 로비로
            SceneManager.LoadScene(1);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방 -> 로비로 이동");
        SceneManager.LoadScene(1);
    }
}
