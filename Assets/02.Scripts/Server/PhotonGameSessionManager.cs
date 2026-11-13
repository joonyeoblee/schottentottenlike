using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
public class PhotonGameSessionManager : SingletonPhoton<PhotonGameSessionManager>
{
    private bool _isFirstRun = true;

    private void Start()
    {
        _isFirstRun = false;
    }

    private void OnApplicationFocus(bool focus)
    {
        // 대전 중 재연결은 ReconnectManager가 담당
        if (focus && !_isFirstRun && !PhotonNetwork.IsConnected && !ReconnectManager.IsReconnecting)
        {
            TryReconnect();
        }
    }

    private void TryReconnect()
    {
        if (PhotonNetwork.ReconnectAndRejoin())
        {
            Debug.Log("Reconnect 성공");
        }
        else
        {
            Debug.LogWarning("Reconnect 실패 → 마스터 서버로 재접속 시도");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Photon 끊김: {cause}");
    }
}
