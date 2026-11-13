#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 멀티플레이 자동 테스트 클라이언트 (에디터/개발 빌드 전용 — 릴리스 빌드에는 포함되지 않음).
/// - 플레이어 실행 인자 "-autotest guest" : 자동 로그인 → 로비에서 autotest 방 입장 → 자기 차례마다 카드를 냄
/// - 에디터에서는 AutoTestClient.Begin("host") 호출 : 자동 로그인 → autotest 방 생성 → 자기 차례마다 카드를 냄
/// 카드는 실제 드래그와 같은 경로(UI_CardDragger)로 내고, 매 수/결과를 persistentDataPath/autotest_{role}.log 에 기록합니다.
/// </summary>
public class AutoTestClient : MonoBehaviourPunCallbacks
{
    public static AutoTestClient Instance { get; private set; }

    private string _role;
    private int _gamesToPlay = 2;
    private int _gamesDone;
    private int _moves;
    private string _logPath;
    private readonly List<RoomInfo> _rooms = new List<RoomInfo>();

    // 장애 시나리오 (첫 판에서만)
    private int _dropAtMove = -1;   // 이 수를 둔 직후 네트워크 끊김 흉내 → 자동 재연결 검증
    private int _idleAtTurn = -1;   // 이 번째 내 차례에 아무것도 안 함 → 턴 시간 초과 자동 진행 검증
    private int _myTurnCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FromCommandLine()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-autotest");
        if (i < 0) return;
        int Arg(string name, int def) { int k = System.Array.IndexOf(args, name); return k >= 0 && k + 1 < args.Length && int.TryParse(args[k + 1], out var v) ? v : def; }
        Begin(i + 1 < args.Length ? args[i + 1] : "guest", Arg("-drop", -1), Arg("-idle", -1), Arg("-games", 2));
    }

    public static string Begin(string role) => Begin(role, -1, -1, 2);

    public static string Begin(string role, int dropAtMove, int idleAtTurn, int games)
    {
        if (Instance != null) return "already running as " + Instance._role;
        var go = new GameObject("AutoTestClient");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<AutoTestClient>();
        Instance._role = role;
        Instance._dropAtMove = dropAtMove;
        Instance._idleAtTurn = idleAtTurn;
        Instance._gamesToPlay = games;
        Instance._logPath = Path.Combine(Application.persistentDataPath, $"autotest_{role}.log");
        File.WriteAllText(Instance._logPath, "");
        Instance.Log($"start role={role} unity={Application.unityVersion} drop={dropAtMove} idle={idleAtTurn} games={games}");
        Instance.StartCoroutine(Instance.Run());
        return "started " + role + " log=" + Instance._logPath;
    }

    public static string Status()
    {
        if (Instance == null) return "not running";
        var gm = GameManager.Instance;
        var bf = BattleField.Instance;
        return $"role={Instance._role} scene={SceneManager.GetActiveScene().name} net={PhotonNetwork.NetworkClientState} room={(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name + " players=" + PhotonNetwork.CurrentRoom.PlayerCount : "-")} " +
               $"games={Instance._gamesDone} moves={Instance._moves} myTurn={(bf != null && bf.IsMyTurn)} gameOver={(gm != null && gm.IsGameOver)} owners={(gm != null && gm.RoundOwners != null ? string.Join(",", gm.RoundOwners) : "-")}";
    }

    private void Log(string msg)
    {
        string line = $"[{System.DateTime.Now:HH:mm:ss.fff}] [{_role}] {msg}";
        Debug.Log("[AutoTest] " + line);
        try { File.AppendAllText(_logPath, line + "\n"); } catch { }
    }

    // ------------------------------------------------------------------
    private IEnumerator Run()
    {
        // 1) 메인 화면에서 로그인
        yield return WaitFor(() => FindAnyObjectByType<UI_Account>() != null, 30, "main menu");
        var account = FindAnyObjectByType<UI_Account>();
        account.NickNameInputField.text = _role == "host" ? "자동방장" : "자동손님";
        account.Login();
        Log("login requested");

        // 2) 로비 입장 대기
        yield return WaitFor(() => SceneManager.GetActiveScene().buildIndex == 1 && PhotonNetwork.InLobby, 60, "lobby");
        Log($"in lobby region={PhotonNetwork.CloudRegion}");

        if (_role.StartsWith("quick"))
        {
            // 빠른 대전: 양쪽 다 같은 버튼 → 먼저 누른 쪽이 방을 만들고, 나중 쪽이 자동으로 들어옴
            yield return new WaitForSeconds(_role == "quickB" ? 3f : 0f);
            FindAnyObjectByType<UI_Lobby>().QuickMatch();
            Log("quick match requested");
        }
        else if (_role == "host")
        {
            var lobby = FindAnyObjectByType<UI_Lobby>();
            lobby.RoomNameInputField.text = "autotest";
            lobby.CreateRoom();
            Log("create room requested");
        }
        else
        {
            yield return WaitFor(() => _rooms.Any(r => r.Name.StartsWith("autotest") && r.PlayerCount < r.MaxPlayers && r.IsOpen), 120, "autotest room in list");
            var room = _rooms.Where(r => r.Name.StartsWith("autotest") && r.IsOpen).OrderByDescending(r => r.Name).First();
            Log("joining " + room.Name);
            PhotonNetwork.JoinRoom(room.Name);
        }

        // 3) 게임 씬 + 필드 생성 대기
        yield return WaitFor(() => PhotonNetwork.InRoom && BattleField.Instance != null, 60, "battlefield");
        Log($"in game room={PhotonNetwork.CurrentRoom.Name} master={PhotonNetwork.IsMasterClient} actor={PhotonNetwork.LocalPlayer.ActorNumber}");

        // 4) 게임 진행
        while (_gamesDone < _gamesToPlay)
        {
            yield return PlayOneGame();
            _gamesDone++;
            if (_gamesDone < _gamesToPlay)
            {
                yield return new WaitForSeconds(2.5f);
                if (!ClickResultButton("다시 하기")) Log("ERROR rematch button not found");
                else Log("rematch requested");
                yield return WaitFor(() => GameManager.Instance != null && !GameManager.Instance.IsGameOver, 60, "rematch start");
            }
        }
        Log("DONE all games");
    }

    private IEnumerator PlayOneGame()
    {
        _moves = 0;
        _myTurnCount = 0;
        var gm = GameManager.Instance;
        float idle = 0;
        ETurn lastTurnSeen = (ETurn)(-1);
        while (!gm.IsGameOver)
        {
            var bf = BattleField.Instance;
            if (bf == null)
            {
                // 연결 끊김으로 게임판이 사라진 상태 → 재연결을 기다림
                if (ReconnectManager.ExpectingReconnect)
                {
                    yield return new WaitForSeconds(0.5f);
                    idle += 0.5f;
                    if (idle > 90f) { Log("ERROR reconnect timeout " + Status()); yield break; }
                    continue;
                }
                Log("ERROR battlefield destroyed (opponent left?)"); yield break;
            }

            if (bf.IsMyTurn && bf.HandCardManagers[0].HandCardSlots.Any(s => s.HasCard))
            {
                if (bf.CurrentTurn != lastTurnSeen) { _myTurnCount++; lastTurnSeen = bf.CurrentTurn; }
                bool doIdle = _gamesDone == 0 && _myTurnCount == _idleAtTurn;
                if (doIdle)
                {
                    // 턴 시간 초과 테스트: 아무것도 하지 않고 자동 진행을 기다림
                    Log($"IDLE turn #{_myTurnCount} — waiting for timeout auto-play ({BattleField.TurnSeconds}s)");
                    int before = bf.Rounds.Sum(r => r.CountCards(true));
                    float t0 = Time.time;
                    while (bf != null && bf.IsMyTurn && Time.time - t0 < BattleField.TurnSeconds + 10) { yield return new WaitForSeconds(0.25f); bf = BattleField.Instance; }
                    int after = bf != null ? bf.Rounds.Sum(r => r.CountCards(true)) : -1;
                    Log(after == before + 1 ? $"TIMEOUT-AUTOPLAY ok after {Time.time - t0:F1}s" : $"ERROR timeout autoplay did not happen (before={before} after={after})");
                    _idleAtTurn = -1;
                    lastTurnSeen = (ETurn)(-1);
                    continue;
                }

                yield return new WaitForSeconds(1.2f); // 연출이 끝날 시간
                if (gm.IsGameOver || !bf.IsMyTurn) continue;
                try { MakeMove(bf); }
                catch (System.Exception e) { Log("ERROR move exception: " + e); yield break; }
                lastTurnSeen = (ETurn)(-1);
                idle = 0;

                if (_gamesDone == 0 && _moves == _dropAtMove)
                {
                    yield return SimulateDrop();
                    gm = GameManager.Instance;
                }
            }
            else
            {
                if (!bf.IsMyTurn) lastTurnSeen = (ETurn)(-1);
                idle += 0.25f;
                if (idle > 120f) { Log("ERROR stuck: no turn change for 120s " + Status()); yield break; }
            }
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(1.5f);
        var owners = gm.RoundOwners;
        int me = owners.Count(o => o == 1), enemy = owners.Count(o => o == 2);
        Log($"RESULT game={_gamesDone + 1} moves={_moves} owners(local)={string.Join(",", owners)} me={me} enemy={enemy} board={Board(BattleField.Instance)}");
    }

    /// <summary>
    /// 네트워크 끊김 흉내: 들어오고 나가는 패킷을 100% 유실 → 클라이언트 타임아웃으로 실제처럼 끊김.
    /// 끊긴 뒤 유실을 해제하면 ReconnectManager가 자동으로 재연결/재입장하고, BattleField가 판 상태를 복원해야 함.
    /// </summary>
    private IEnumerator SimulateDrop()
    {
        string before = Board(BattleField.Instance);
        Log($"DROP simulating connection loss after move #{_moves} board={before}");
        PhotonNetwork.NetworkingClient.SimulateConnectionLoss(true);

        float t0 = Time.realtimeSinceStartup;
        // 끊김 감지 = 연결이 끊겼거나, 이미 재연결을 시작했음 (재연결이 너무 빨라 Disconnected 상태를 놓칠 수 있음)
        yield return WaitFor(() => !PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState == ClientState.Disconnected || ReconnectManager.IsReconnecting, 40, "disconnect");
        Log($"DROP disconnected after {Time.realtimeSinceStartup - t0:F1}s reconnecting={ReconnectManager.IsReconnecting} scene={SceneManager.GetActiveScene().name}");
        yield return new WaitForSecondsRealtime(2f);
        PhotonNetwork.NetworkingClient.SimulateConnectionLoss(false); // 네트워크 복구

        yield return WaitFor(() => PhotonNetwork.InRoom && BattleField.Instance != null && BattleField.Instance.MatchInProgress && !ReconnectManager.IsReconnecting, 70, "rejoin + restore");
        yield return new WaitForSeconds(2f); // 스냅샷 적용/로드
        Log($"REJOINED after {Time.realtimeSinceStartup - t0:F1}s master={PhotonNetwork.IsMasterClient} scene={SceneManager.GetActiveScene().name} board={Board(BattleField.Instance)}");
    }

    private void MakeMove(BattleField bf)
    {
        var gm = GameManager.Instance;
        bool Same(Card a, Card b) => a.CardNumber == b.CardNumber && a.Color == b.Color;
        var field = bf.Rounds.SelectMany(r => r.PlayerCardSlots.Concat(r.EnemyCardSlots)).Where(s => s.IsOccupied).Select(s => s.Card).ToList();
        var fieldUnused = gm.GetAllPossibleCards().Where(c => !field.Any(f => Same(f, c))).ToList();
        var handSlots = bf.HandCardManagers[0].HandCardSlots;
        var hand = handSlots.Select(s => s.HasCard ? s.Card : null).ToList();

        var view = new AIBrain.BoardView
        {
            AiCards = bf.Rounds.Select(r => r.PlayerCardSlots.Where(s => s.IsOccupied).Select(s => s.Card).ToList()).ToArray(),
            PlayerCards = bf.Rounds.Select(r => r.EnemyCardSlots.Where(s => s.IsOccupied).Select(s => s.Card).ToList()).ToArray(),
            Owners = gm.RoundOwners.Select(o => o == 0 ? 0 : (o == 1 ? 2 : 1)).ToArray(),
            Hand = hand,
            FieldUnused = fieldUnused,
            Unseen = fieldUnused.Where(c => !hand.Any(h => h != null && Same(h, c))).ToList(),
            SlotsPerSide = bf.Rounds[0].PlayerCardSlots.Length,
        };
        var mv = AIBrain.ChooseMove(view);
        if (!mv.HasValue) { Log("no move available"); return; }

        var handSlot = handSlots[mv.Value.HandIndex];
        var card = handSlot.Card;
        var target = bf.Rounds[mv.Value.RoundIndex].FirstEmptySlot(true);
        var dragger = handSlot.GetComponentInChildren<UI_CardDragger>(true);

        // 실제 드래그와 같은 경로
        var ev = new PointerEventData(EventSystem.current);
        dragger.OnPointerDown(ev);
        dragger.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, dragger.transform.position.z);
        dragger.OnPointerUp(ev);

        bool ok = target.IsOccupied && !handSlot.HasCard;
        if (!ok)
        {
            var hits = Physics2D.OverlapPointAll(target.transform.position);
            var col = target.GetComponent<Collider2D>();
            Log($"DIAG target={target.name} round={target.RoundIndex} tag={target.tag} colBounds={(col ? col.bounds.ToString() : "-")} layer={LayerMask.LayerToName(target.gameObject.layer)}");
            Log($"DIAG myTurn={bf.IsMyTurn} canPlace={bf.CanPlaceOn(target)} targetActive={target.gameObject.activeInHierarchy} " +
                $"collider={(target.GetComponent<Collider2D>() ? target.GetComponent<Collider2D>().enabled.ToString() : "none")} " +
                $"targetPos={target.transform.position} dropPos={dragger.transform.position} handHasCard={handSlot.HasCard} " +
                $"hits=[{string.Join(",", hits.Select(h => h.name + ":" + h.tag))}]");
        }
        _moves++;
        Log($"{(ok ? "MOVE" : "ERROR drag failed")} #{_moves} {card.Color}{card.CardNumber} -> stone {mv.Value.RoundIndex} owners={string.Join(",", gm.RoundOwners)}");
    }

    private static string Board(BattleField bf)
    {
        if (bf == null) return "-";
        var sb = new StringBuilder();
        foreach (var r in bf.Rounds)
        {
            string side(CardSlot[] s) => string.Join("", s.Where(x => x.IsOccupied).Select(x => $"{x.Card.Color.ToString()[0]}{x.Card.CardNumber}"));
            sb.Append($"[{side(r.PlayerCardSlots)}|{side(r.EnemyCardSlots)}]");
        }
        return sb.ToString();
    }

    private static bool ClickResultButton(string label)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var t = b.GetComponentInChildren<TMP_Text>();
            if (t != null && t.text == label && b.isActiveAndEnabled && b.interactable) { b.onClick.Invoke(); return true; }
        }
        return false;
    }

    private IEnumerator WaitFor(System.Func<bool> cond, float timeout, string what)
    {
        float t = 0;
        while (!cond())
        {
            t += 0.25f;
            if (t > timeout) { Log($"ERROR timeout waiting for {what}: {Status()}"); yield break; }
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ---- Photon 콜백 ----
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var info in roomList)
        {
            _rooms.RemoveAll(r => r.Name == info.Name);
            if (!info.RemovedFromList) _rooms.Add(info);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message) => Log($"ERROR join failed {returnCode} {message}");
    public override void OnCreateRoomFailed(short returnCode, string message) => Log($"ERROR create failed {returnCode} {message}");
    public override void OnPlayerEnteredRoom(Player newPlayer) => Log($"player entered: {newPlayer.NickName} ({newPlayer.ActorNumber})");
    public override void OnPlayerLeftRoom(Player otherPlayer) => Log($"player left: {otherPlayer.NickName}");
    public override void OnDisconnected(DisconnectCause cause) => Log($"disconnected: {cause}");
}
#endif
