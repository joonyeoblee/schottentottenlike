using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
public class AccountManager : SingletonPhoton<AccountManager>
{
    public Account CurrentAccount { get; private set; }

    public void TryLogin(Account account)
    {
        CurrentAccount = account;
        Debug.Log($"로그인 시도: {CurrentAccount.Nickname}");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnected()
    {
        Debug.Log("네임 서버 접속 완료");
        Debug.Log($"클라우드 리전: {PhotonNetwork.CloudRegion}");
        // 2. 닉네임 : 게임에서 사용할 사용자의 별명(중복가능) -> 판별을 위해서는 ActorID를 쓴다
        PhotonNetwork.NickName = CurrentAccount.Nickname;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버에 연결되었습니다.");
        Debug.Log($"현재 상태: {PhotonNetwork.NetworkClientState}");
        Debug.Log($"로비 참가 여부: {PhotonNetwork.InLobby}");
        SceneManager.LoadScene(1);
        // 로비에 자동으로 참가
        bool joinResult = PhotonNetwork.JoinLobby(TypedLobby.Default);
        Debug.Log($"로비 참가 요청 결과: {joinResult}");
    }

}
