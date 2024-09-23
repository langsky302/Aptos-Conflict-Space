using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.Linq;

//Aptos
using Aptos.Unity.Rest;
using Aptos.Unity.Rest.Model;
using Aptos.HdWallet;
using Aptos.Accounts;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

    private void Awake()
    {
        instance = this;
    }

    public GameObject loadingScreen;
    public TMP_Text loadingText;
    public GameObject menuButtons;

    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;

    public GameObject roomScreen;
    public TMP_Text roomNameText, playerNameLabel;
    private List<TMP_Text> allPlayerNames = new List<TMP_Text>();

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    public GameObject nameInputScreen;
    public TMP_InputField nameInput;
    public static bool hasSetNick;

    public string levelToPlay;

    public GameObject startButton;

    public GameObject roomTestButton;

    public string[] allMaps;
    public bool changeMapBetweenRounds = true;

    public TMP_Text startGameButtonText;

    public GameObject roomLeaveButton;

    public GameObject betAptosText;


    // Start is called before the first frame update
    void Start()
    {
        CloseMenu();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network";

        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
        }       

#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenu() {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
    }

    public override void OnConnectedToMaster()
    { 
        PhotonNetwork.JoinLobby();

        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        CloseMenu();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if (!hasSetNick)
        {
            CloseMenu();
            nameInputScreen.SetActive(true);

            if (PlayerPrefs.HasKey("playerName"))
            {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
        }
        else {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    public void OpenRoomCreate() {
        CloseMenu();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom() {
        if (!string.IsNullOrEmpty(roomNameInput.text)) {

            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 2;

            PhotonNetwork.CreateRoom(roomNameInput.text, options);

            CloseMenu();
            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenu();
        roomScreen.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else {
            startButton.SetActive(false);
        }
    }

    private void ListAllPlayers() {
        foreach (TMP_Text player in allPlayerNames) {
            Destroy(player.gameObject);
        }
        allPlayerNames.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        for (int i =0; i < players.Length; i++) {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            newPlayerLabel.text = players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);
            allPlayerNames.Add(newPlayerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);
        allPlayerNames.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Failed To Create Room: " + message;
        CloseMenu();
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen() {
        CloseMenu();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom() {
        PhotonNetwork.LeaveRoom();
        CloseMenu();
        loadingText.text = "Leaving Room";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenu();
        roomBrowserScreen.SetActive(true);
    }
    public void CloseRoomBrowser()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomButton rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();

        theRoomButton.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && roomList[i].IsOpen && roomList[i].IsVisible && !roomList[i].RemovedFromList)
            {
                RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);
                allRoomButtons.Add(newButton);
            }
        }
    }

    public void JoinRoom(RoomInfo inputInfo) {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenu();
        loadingText.text = "Joining Room";
        loadingScreen.SetActive(true);
    }

    public void QuitGame() {
        Application.Quit();
    }

    public void SetNickname() {
        if (!string.IsNullOrEmpty(nameInput.text)) {
            PhotonNetwork.NickName = nameInput.text;

            PlayerPrefs.SetString("playerName", nameInput.text);

            CloseMenu();
            menuButtons.SetActive(true);
            hasSetNick = true;
        }
    }

    public void StartGame() {
        // Đảm bảo người chơi chỉ có thể bắt đầu trò chơi nếu họ là MasterClient
        if (PhotonNetwork.IsMasterClient)
        {
            // Kiểm tra nếu phòng có đủ 2 người chơi
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                // Đặt phòng thành không mở và không hiển thị khi bắt đầu trò chơi
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // Chuyển sang scene mới
                //PhotonNetwork.LoadLevel(levelToPlay);

                startGameButtonText.text = "Start Game";

                // Gọi RPC để log ra debug message trên tất cả client
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("LogUser12Words", RpcTarget.All, UserDataManager.Instance.User12Words);


                //PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
            }
            else
            {
                startGameButtonText.text = "Need 2 Players to Play";
                Debug.Log("Không đủ người chơi để bắt đầu trò chơi. Cần ít nhất 2 người chơi.");
            }
        }
    }

    [PunRPC]
    public void LogUser12Words(string user12Words)
    {
        // Lấy thông tin người chơi đang thực thi hàm này
        string playerName = PhotonNetwork.NickName; // Lấy nickname của người chơi
        int playerID = PhotonNetwork.LocalPlayer.ActorNumber; // Lấy ID của người chơi

        // In ra log kèm theo tên người chơi và ID của họ
        Debug.Log("User12Words: " + user12Words + " | Logged by Player: " + playerName + " (ID: " + playerID + ")");

        //Trừ tiền ở đây

        StartCoroutine(AptosBetting());

    }

    string mnemo = "voyage chalk social search pair zone husband mix lonely gather cherry beach";

    public IEnumerator AptosBetting()
    {
        float AccountBalance = UserDataManager.Instance.UserAptosBalance;
        if (AccountBalance < 0.1f)
        {
            Debug.Log("Not enough Aptos");
            yield break;
        }

        roomLeaveButton.gameObject.SetActive(false);
        betAptosText.SetActive(true);

        #region REST & Faucet Client Setup
        Debug.Log("<color=cyan>=== =========================== ===</color>");
        Debug.Log("<color=cyan>=== Set Up REST Client ===</color>");
        Debug.Log("<color=cyan>=== =========================== ===</color>");

        RestClient restClient = RestClient.Instance.SetEndPoint(Constants.TESTNET_BASE_URL);
        Coroutine restClientSetupCor = StartCoroutine(RestClient.Instance.SetUp());
        yield return restClientSetupCor;
        #endregion

        Wallet wallet = new Wallet(UserDataManager.Instance.User12Words);

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

        Wallet wallet1 = new Wallet(mnemo);

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

        #region Have Alice give Bob 0.1 APT coins - Submit Transfer Transaction
        Aptos.Unity.Rest.Model.Transaction transferTxn = new Aptos.Unity.Rest.Model.Transaction();
        Coroutine transferCor = StartCoroutine(RestClient.Instance.Transfer((_transaction, _responseInfo) =>
        {
            transferTxn = _transaction;
            responseInfo = _responseInfo;
        }, alice, bob.AccountAddress.ToString(), 10000000));

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

        roomLeaveButton.gameObject.SetActive(true);
        betAptosText.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
        }

        yield return null;

    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public void QuickJoin() {

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test", options);
        CloseMenu();
        loadingText.text = "Creating Room";
        loadingScreen.SetActive(true);
    }
}
