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
            ReconnectManager.LeaveIntentionally(); // 자리 보존 없이 나감 (상대가 기다리지 않음)
        }
        else
        {
            // 솔로(AI) 모드: 방이 없으므로 바로 로비로
            SceneManager.LoadScene(1);
        }
    }

    public override void OnLeftRoom()
    {
        // 연결 끊김으로 방에서 빠진 경우엔 로비로 가지 않고 재연결을 기다림
        if (ReconnectManager.ExpectingReconnect) return;
        Debug.Log("방 -> 로비로 이동");
        SceneManager.LoadScene(1);
    }
}
