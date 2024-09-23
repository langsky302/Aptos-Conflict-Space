using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

//Aptos
using Aptos.Unity.Rest;
using Aptos.Unity.Rest.Model;
using Aptos.HdWallet;
using Aptos.Accounts;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

    private void Awake()
    {
        instance = this;
    }

    public enum EventCodes : byte { 
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;

    private List<LeaderboardPlayer> lboardPlayers = new List<LeaderboardPlayer>();

    public enum GameState { 
        Waiting,
        PLaying,
        Ending
    }

    public int killsToWin = 3;
    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterEnding = 5f;

    public bool perpetual;

    public float matchLength = 180f;
    private float currentMatchTime;
    private float sendTimer;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else {

            NewPlayerSend(PhotonNetwork.NickName);

            state = GameState.PLaying;

            SetupTimer();

            if (!PhotonNetwork.IsMasterClient) {
                UIController.instance.timerText.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending) {
            if (UIController.instance.leaderboard.activeInHierarchy)
            {
                UIController.instance.leaderboard.SetActive(false);
            }
            else {
                ShowLeaderboard();
            }
        }

        if (PhotonNetwork.IsMasterClient) {
            if (currentMatchTime > 0f && state == GameState.PLaying)
            {
                currentMatchTime -= Time.deltaTime;

                if (currentMatchTime <= 0f)
                {
                    currentMatchTime = 0;
                    state = GameState.Ending;
                    ListPlayersSend();
                    StateCheck();
                }
                UpdateTimerDisplay();

                sendTimer -= Time.deltaTime;
                if (sendTimer <= 0) {
                    sendTimer += 1f;

                    TimerSend();
                }
            }
        }        
    }

    public void OnEvent(EventData photonEvent) {

        if (photonEvent.Code < 200) {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;
                case EventCodes.ListPlayers:
                    ListPlayersReceive(data);
                    break;
                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;
                case EventCodes.NextMatch:
                    NextMapReceive();
                    break;
                case EventCodes.TimerSync:
                    TimerReceive(data);
                    break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username) {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);

        allPlayers.Add(player);
        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        // tạo ra một mảng mới có kiểu object với kích thước bằng số lượng phần tử trong allPlayers.
        //object[] chỉ định rằng package là một mảng có thể chứa nhiều đối tượng khác nhau, vì kiểu object trong C# là kiểu cơ sở cho tất cả các loại.
        //new object[allPlayers.Count] khởi tạo mảng với số lượng phần tử bằng số lượng người chơi (allPlayers.Count).
        object[] package = new object[allPlayers.Count + 1];

        package[0] = state;

        for (int i = 0; i < allPlayers.Count; i++) {
            //tạo ra một mảng mới có kiểu object với kích thước cố định là 4.
            //Mảng piece có thể chứa tối đa 4 đối tượng khác nhau, vì nó được khai báo với kiểu object, cho phép chứa bất kỳ loại dữ liệu nào.
            //Các phần tử trong mảng này sẽ được khởi tạo với giá trị mặc định là null, cho đến khi bạn gán giá trị cụ thể cho chúng.
            object[] piece = new object[4];
            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].death;

            package[i + 1] = piece;
            //package = {   {name,actor,kills,death},     {name,actor,kills,death} ,     {name,actor,kills,death}     }
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void ListPlayersReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        state = (GameState)dataReceived[0];

        //Vòng lặp for: for (int i = 0; i < dataReceived.Length; i++) sử dụng biến i để duyệt qua từng chỉ số của mảng dataReceived,
        //từ 0 đến chiều dài của mảng trừ 1.
        for (int i = 1; i < dataReceived.Length; i++) {
            //Gán giá trị: Trong mỗi vòng lặp, object[] piece = (object[])dataReceived[i];
            //ép kiểu phần tử thứ i của dataReceived sang kiểu object[] và gán nó cho biến piece.
            //Điều này có nghĩa là mỗi phần tử trong dataReceived được giả định là một mảng các đối tượng (kiểu object[]).
            //Nếu phần tử không phải là kiểu object[], sẽ xảy ra lỗi tại thời điểm chạy.
            object[] piece = (object[])dataReceived[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
                );
            allPlayers.Add(player);

            if (player.kills == 3 && PhotonNetwork.LocalPlayer.ActorNumber == player.actor) {
                Debug.Log("The winner is: " + player.name);

                //Thực thi code cộng tiền ở đây

                StartCoroutine(WinnerGetAptos());


            }

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor) {
                index = i - 1;
            }
        }

        StateCheck();
    }

    string mnemo = "voyage chalk social search pair zone husband mix lonely gather cherry beach";

    public IEnumerator WinnerGetAptos()
    {
        float AccountBalance = UserDataManager.Instance.UserAptosBalance;
        if (AccountBalance < 0.1f)
        {
            Debug.Log("Not enough Aptos");
            yield break;
        }

        #region REST & Faucet Client Setup
        Debug.Log("<color=cyan>=== =========================== ===</color>");
        Debug.Log("<color=cyan>=== Set Up REST Client ===</color>");
        Debug.Log("<color=cyan>=== =========================== ===</color>");

        RestClient restClient = RestClient.Instance.SetEndPoint(Constants.TESTNET_BASE_URL);
        Coroutine restClientSetupCor = StartCoroutine(RestClient.Instance.SetUp());
        yield return restClientSetupCor;
        #endregion

        Wallet wallet1 = new Wallet(UserDataManager.Instance.User12Words);
        Wallet wallet = new Wallet(mnemo);

        #region Alice Account
        Debug.Log("<color=cyan>=== ========= ===</color>");
        Debug.Log("<color=cyan>=== Addresses ===</color>");
        Debug.Log("<color=cyan>=== ========= ===</color>");
        Account alice = wallet.GetAccount(0);
        string authKey = alice.AuthKey();
        //Debug.Log("Alice Auth Key: " + authKey);

        AccountAddress aliceAddress = alice.AccountAddress;
        Debug.Log("Alice's Account Address: " + aliceAddress.ToString());

        PrivateKey privateKey = alice.PrivateKey;
        //Debug.Log("Aice Private Key: " + privateKey);
        #endregion

        #region Bob Account
        Account bob = wallet1.GetAccount(0);
        AccountAddress bobAddress = bob.AccountAddress;
        Debug.Log("Bob's Account Address: " + bobAddress.ToString());

        Debug.Log("Wallet: Account 0: Alice: " + aliceAddress.ToString());
        Debug.Log("Wallet: Account 1: Bob: " + bobAddress.ToString());
        #endregion

        Debug.Log("<color=cyan>=== ================ ===</color>");
        Debug.Log("<color=cyan>=== Initial Balances ===</color>");
        Debug.Log("<color=cyan>=== ================ ===</color>");

        #region Get Alice Account Balance
        ResponseInfo responseInfo = new ResponseInfo();
        AccountResourceCoin.Coin coin = new AccountResourceCoin.Coin();
        responseInfo = new ResponseInfo();
        Coroutine getAliceBalanceCor1 = StartCoroutine(RestClient.Instance.GetAccountBalance((_coin, _responseInfo) =>
        {
            coin = _coin;
            responseInfo = _responseInfo;

        }, aliceAddress));
        yield return getAliceBalanceCor1;

        if (responseInfo.status == ResponseInfo.Status.Failed)
        {
            Debug.LogError(responseInfo.message);
            yield break;
        }

        Debug.Log("Alice's Balance Before Funding: " + coin.Value);
        #endregion

        #region Have Alice give Bob 0.2 APT coins - Submit Transfer Transaction
        Aptos.Unity.Rest.Model.Transaction transferTxn = new Aptos.Unity.Rest.Model.Transaction();
        Coroutine transferCor = StartCoroutine(RestClient.Instance.Transfer((_transaction, _responseInfo) =>
        {
            transferTxn = _transaction;
            responseInfo = _responseInfo;
        }, alice, bob.AccountAddress.ToString(), 20000000));

        yield return transferCor;

        if (responseInfo.status != ResponseInfo.Status.Success)
        {
            Debug.LogWarning("Transfer failed: " + responseInfo.message);
            yield break;
        }

        Debug.Log("Transfer Response: " + responseInfo.message);
        string transactionHash = transferTxn.Hash;
        Debug.Log("Transfer Response Hash: " + transferTxn.Hash);
        #endregion

        #region Wait For Transaction
        bool waitForTxnSuccess = false;
        Coroutine waitForTransactionCor = StartCoroutine(
            RestClient.Instance.WaitForTransaction((_pending, _responseInfo) =>
            {
                waitForTxnSuccess = _pending;
                responseInfo = _responseInfo;
            }, transactionHash)
        );
        yield return waitForTransactionCor;

        if (!waitForTxnSuccess)
        {
            Debug.LogWarning("Transaction was not found. Breaking out of example", gameObject);
            yield break;
        }

        #endregion

        Debug.Log("<color=cyan>=== ===================== ===</color>");
        Debug.Log("<color=cyan>=== Intermediate Balances ===</color>");
        Debug.Log("<color=cyan>=== ===================== ===</color>");

        #region Get Alice Account Balance After Transfer
        Coroutine getAliceAccountBalance3 = StartCoroutine(RestClient.Instance.GetAccountBalance((_coin, _responseInfo) =>
        {
            coin = _coin;
            responseInfo = _responseInfo;
        }, aliceAddress));
        yield return getAliceAccountBalance3;

        if (responseInfo.status == ResponseInfo.Status.Failed)
        {
            Debug.LogError(responseInfo.message);
            yield break;
        }

        Debug.Log("Alice Balance After Transfer: " + coin.Value);
        #endregion

        #region Get Bob Account Balance After Transfer
        Coroutine getBobAccountBalance2 = StartCoroutine(RestClient.Instance.GetAccountBalance((_coin, _responseInfo) =>
        {
            coin = _coin;
            responseInfo = _responseInfo;
        }, bobAddress));
        yield return getBobAccountBalance2;

        if (responseInfo.status == ResponseInfo.Status.Failed)
        {
            Debug.LogError(responseInfo.message);
            yield break;
        }

        Debug.Log("Bob Balance After Transfer: " + coin.Value);

        #endregion

        yield return null;

    }


    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        //Khởi tạo mảng: new object[] { ... } tạo ra một mảng mới với các phần tử được chỉ định trong dấu ngoặc nhọn.

        object[] package = new object[] { actorSending , statToUpdate , amountToChange };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < allPlayers.Count; i++) {
            if (allPlayers[i].actor == actor) {
                switch (statType) {
                    case 0: //kills
                        allPlayers[i].kills += amount;
                        Debug.Log("Player " + allPlayers[i].name + " : kills " + allPlayers[i].kills);
                        break;

                    case 1: //deaths
                        allPlayers[i].death += amount;
                        Debug.Log("Player " + allPlayers[i].name + " : deaths " + allPlayers[i].death);
                        break;
                }

                if (i == index) {
                    UpdateStatsDisplay();
                }

                if (UIController.instance.leaderboard.activeInHierarchy) {
                    ShowLeaderboard();
                }

                break;
            }
        }
        ScoreCheck();
    }

    public void UpdateStatsDisplay() {

        if (allPlayers.Count > index)
        {
            UIController.instance.killsText.text = "Kills: " + allPlayers[index].kills;
            UIController.instance.deathsText.text = "Deaths: " + allPlayers[index].death;
        }
        else {
            UIController.instance.killsText.text = "Kills: 0" ;
            UIController.instance.deathsText.text = "Deaths: 0" ;
        }      
    }

    void ShowLeaderboard() {
        UIController.instance.leaderboard.SetActive(true);

        foreach (LeaderboardPlayer lp in lboardPlayers) {
            Destroy(lp.gameObject);
        }
        lboardPlayers.Clear();
        UIController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers);

        foreach (PlayerInfo player in sorted) {
            LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderboardPlayerDisplay, UIController.instance.leaderboardPlayerDisplay.transform.parent);

            newPlayerDisplay.SetDetails(player.name, player.kills, player.death);

            newPlayerDisplay.gameObject.SetActive(true);

            lboardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players) {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < players.Count) {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo player in players) {
                if (!sorted.Contains(player)) {
                    if (player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }                
            }
            sorted.Add(selectedPlayer);
        }
        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in allPlayers) { 
            if (player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound) {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending) {
                state = GameState.Ending;
                ListPlayersSend();
            }
        }
    }

    void StateCheck() {
        if (state == GameState.Ending) {
            EndGame();
        }
    }
    void EndGame() {
        state = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.endScreen.SetActive(true);
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;

        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo() {
        yield return new WaitForSeconds(waitAfterEnding);
        if (!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else {
            if (PhotonNetwork.IsMasterClient) {

                if (!Launcher.instance.changeMapBetweenRounds)
                {
                    NextMapSend();
                }
                else {
                    int newLevel = Random.Range(0, Launcher.instance.allMaps.Length);

                    if (Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMapSend();
                    }
                    else {
                        PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
                    }
                }
            }
        }
    }

    public void NextMapSend() {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void NextMapReceive()
    {
        state = GameState.PLaying;

        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);

        foreach (PlayerInfo player in allPlayers) {
            player.kills = 0;
            player.death = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.instance.SpawnPlayer();

        SetupTimer();
    }

    public void SetupTimer() {
        if (matchLength > 0) {
            currentMatchTime = matchLength;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay() {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + " " + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend() {

        object[] package = new object[] { (int)currentMatchTime, state };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void TimerReceive(object[] dataReceived)
    {
        currentMatchTime = (int)dataReceived[0];
        state = (GameState)dataReceived[1];

        UpdateTimerDisplay();

        UIController.instance.timerText.gameObject.SetActive(true);
    }
}

[System.Serializable]
public class PlayerInfo {
    public string name;
    public int actor, kills, death;

    public PlayerInfo(string _name, int _actor, int _kills, int _deaths) {
        name = _name;
        actor = _actor;
        kills = _kills;
        death = _deaths;
    }
}
