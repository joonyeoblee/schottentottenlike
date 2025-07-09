using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class UI_GameOver : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private GameObject _gameOverPanel;
    [SerializeField]
    private TextMeshProUGUI _resultText;
    [Header("Player1")]
    [SerializeField]
    private TextMeshProUGUI _player1NameText;
    [SerializeField]
    private Toggle _player1ReadyToggle;
    [Header("Player2")]
    [SerializeField]
    private TextMeshProUGUI _player2NameText;
    [SerializeField]
    private Toggle _player2ReadyToggle;
    [Header("Buttons")]
    [SerializeField]
    private Button _restartButton;
    [SerializeField]
    private Button _exitButton;

    private bool _isReady = false;
    private bool _otherReady = false;


    private void Awake()
    {
        _restartButton.onClick.AddListener(OnRestartButtonClicked);
        _exitButton.onClick.AddListener(OnExitButtonClicked);
    }


    private void OnEnable()
    {
        GameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= HandleGameOver;
    }

    /// <summary>
    /// 게임 종료 시점에 호출되어 UI를 세팅하고 패널을 활성화합니다.
    /// </summary>
    private void HandleGameOver(int winner)
    {
        // 플레이어1,2 정보 추출 (ActorNumber 기준 오름차순)
        var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        var player1 = players.Length > 0 ? players[0] : null;
        var player2 = players.Length > 1 ? players[1] : null;
        string player1Name = player1?.NickName ?? "Player1";
        string player2Name = player2?.NickName ?? "Player2";

        // 내 ActorNumber와 승자 비교
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int winnerActorNumber = (winner == 1) ? player1?.ActorNumber ?? -1 : (winner == 2) ? player2?.ActorNumber ?? -1 : -1;

        string result = "무승부";
        if (winner == 1 || winner == 2)
            result = (myActorNumber == winnerActorNumber) ? "승리" : "패배";

        Set(player1Name, player2Name, result);
    }

    /// <summary>
    /// UI 텍스트 및 토글 초기화, 결과 텍스트 표시, 패널 활성화
    /// </summary>
    public void Set(string player1name, string player2name, string result)
    {
        _player1NameText.text = player1name;
        _player2NameText.text = player2name;
        _player1ReadyToggle.isOn = false;
        _player2ReadyToggle.isOn = false;
        _restartButton.gameObject.SetActive(true);
        _exitButton.gameObject.SetActive(true);
        _resultText.text = result;
        _resultText.gameObject.SetActive(true);
        _gameOverPanel.SetActive(true);
    }

    private void OnRestartButtonClicked()
    {
        _isReady = true;
        UpdateReadyToggles();
        photonView.RPC(nameof(RPC_SetReady), RpcTarget.Others, true);
        CheckRestart();
    }

    private void OnExitButtonClicked()
    {
        photonView.RPC(nameof(RPC_OpponentExit), RpcTarget.Others);
        PhotonNetwork.LeaveRoom();
    }

    private void UpdateReadyToggles()
    {
        // 상대가 없을 경우 예외 방지
        if (PhotonNetwork.PlayerListOthers.Length == 0)
        {
            _player1ReadyToggle.isOn = _isReady;
            _player2ReadyToggle.isOn = false;
            return;
        }

        bool isPlayer1 = PhotonNetwork.LocalPlayer.ActorNumber < PhotonNetwork.PlayerListOthers[0].ActorNumber;
        if (isPlayer1)
        {
            _player1ReadyToggle.isOn = _isReady;
            _player2ReadyToggle.isOn = _otherReady;
        }
        else
        {
            _player1ReadyToggle.isOn = _otherReady;
            _player2ReadyToggle.isOn = _isReady;
        }
    }

    private void CheckRestart()
    {
        if (_isReady && _otherReady)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(2);
            }
        }
    }

    [PunRPC]
    private void RPC_SetReady(bool ready)
    {
        _otherReady = ready;
        UpdateReadyToggles();
        CheckRestart();
    }

    [PunRPC]
    private void RPC_OpponentExit()
    {
        _gameOverPanel.SetActive(false);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _gameOverPanel.SetActive(false);
    }
}
