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
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방 -> 로비로 이동");
        SceneManager.LoadScene(1);
    }
}
