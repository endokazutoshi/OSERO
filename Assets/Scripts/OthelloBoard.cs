// OthelloBoard.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;


public class OthelloBoard : MonoBehaviourPunCallbacks
{
    public int CurrentTurn = 0;
    public GameObject ScoreBoard;
    public UnityEngine.UI.Text ScoreBoardText;
    public GameObject Template;
    public int BoardSize = 8;
    public List<Color> PlayerChipColors;
    public List<Vector2> DirectionList;

    static OthelloBoard instance;
    public static OthelloBoard Instance { get { return instance; } }

    OthelloCell[,] OthelloCells;

    public int EnemyID { get { return (CurrentTurn + 1) % 2; } }

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            instance = this;
            OthelloBoardIsSquareSize();

            // Photonの初期化はOnConnectedToMaster内で行うように変更
            PhotonNetwork.ConnectUsingSettings();
        }
    }


    public override void OnConnectedToMaster()
    {
        // マスターサーバーに接続後、ロビーに参加
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedRoom()
    {
        // ルームに参加したらゲームを初期化
        OthelloCells = new OthelloCell[BoardSize, BoardSize];
        float cellAnchorSize = 1.0f / BoardSize;

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                CreateNewCell(x, y, cellAnchorSize);
            }
        }

        // デバッグログを挿入
        Debug.Log("OthelloCells initialized. Length: " + OthelloCells.Length);

        if (ScoreBoard != null)
        {
            RectTransform scoreBoardRect = ScoreBoard.GetComponent<RectTransform>();
            if (scoreBoardRect != null)
            {
                scoreBoardRect.SetSiblingIndex(BoardSize * BoardSize + 1);
            }
        }

        Destroy(Template);
        InitializeGame();  // 不要な呼び出しではなく、必要な初期化を実施
    }
    private void CreateNewCell(int x, int y, float cellAnchorSize)
    {
        // Photonが接続されているか確認
        if (PhotonNetwork.IsConnectedAndReady) // IsConnectedAndReadyを使用する
        {
            // PhotonNetwork.Instantiate の第一引数はPrefabの名前
            GameObject go = PhotonNetwork.Instantiate("OthelloCellPrefab", Vector3.zero, Quaternion.identity);
            OthelloCell oc = go.GetComponent<OthelloCell>();

            // 以下、既存のコードと同じ処理
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(x * cellAnchorSize, y * cellAnchorSize);
            r.anchorMax = new Vector2((x + 1) * cellAnchorSize, (y + 1) * cellAnchorSize);

            OthelloCells[x, y] = oc;
            oc.Location.x = x;
            oc.Location.y = y;
        }
        else
        {
            Debug.LogError("Photon is not connected or not ready. Cannot instantiate OthelloCellPrefab.");
        }
    }


    private void OthelloBoardIsSquareSize()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (Screen.width > Screen.height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.height);
        }
        else
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.width);
        }
    }

    public void InitializeGame()
    {
        UnityEngine.Debug.Log("Game Initialized");

        // ScoreBoardがnullでないことを確認する
        if (ScoreBoard != null)
        {
            ScoreBoard.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("ScoreBoard is null in InitializeGame method.");
            return; // もしくはエラー処理を追加してください
        }

        if (OthelloCells == null)
        {
            // OthelloCellsがnullの場合は初期化
            OthelloCells = new OthelloCell[BoardSize, BoardSize];
            float cellAnchorSize = 1.0f / BoardSize;

            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    CreateNewCell(x, y, cellAnchorSize);
                }
            }
        }

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].OwnerID = -1;
            }
        }

        OthelloCells[3, 3].OwnerID = 0;
        OthelloCells[4, 4].OwnerID = 0;
        OthelloCells[4, 3].OwnerID = 1;
        OthelloCells[3, 4].OwnerID = 1;
    }

    // 以下同様...




internal bool CanPlaceHere(Vector2 location)
    {
        if (OthelloCells[(int)location.x, (int)location.y].OwnerID != -1)
            return false;

        for (int direction = 0; direction < DirectionList.Count; direction++)
        {
            Vector2 directionVector = DirectionList[direction];
            if (FindAllyChipOnOtherSide(directionVector, location, false) != null)
            {
                return true;
            }
        }
        return false;
    }

    internal void PlaceHere(OthelloCell othelloCell)
    {
        for (int direction = 0; direction < DirectionList.Count; direction++)
        {
            Vector2 directionVector = DirectionList[direction];
            OthelloCell onOtherSide = FindAllyChipOnOtherSide(directionVector, othelloCell.Location, false);
            if (onOtherSide != null)
            {
                ChangeOwnerBetween(othelloCell, onOtherSide, directionVector);
            }
        }
        OthelloCells[(int)othelloCell.Location.x, (int)othelloCell.Location.y].OwnerID = CurrentTurn;
    }

    private OthelloCell FindAllyChipOnOtherSide(Vector2 directionVector, Vector2 from, bool EnemyFound)
    {
        Vector2 to = from + directionVector;
        if (IsInRangeOfBoard(to) && OthelloCells[(int)to.x, (int)to.y].OwnerID != -1)
        {
            if (OthelloCells[(int)to.x, (int)to.y].OwnerID == OthelloBoard.Instance.CurrentTurn)
            {
                if (EnemyFound)
                    return OthelloCells[(int)to.x, (int)to.y];
                return null;
            }
            else
                return FindAllyChipOnOtherSide(directionVector, to, true);
        }
        return null;
    }

    private bool IsInRangeOfBoard(Vector2 point)
    {
        return point.x >= 0 && point.x < BoardSize && point.y >= 0 && point.y < BoardSize;
    }

    private void ChangeOwnerBetween(OthelloCell from, OthelloCell to, Vector2 directionVector)
    {
        for (Vector2 location = from.Location + directionVector; location != to.Location; location += directionVector)
        {
            OthelloCells[(int)location.x, (int)location.y].OwnerID = CurrentTurn;
        }
    }

    internal void EndTurn(bool isAlreadyEnded)
    {
        CurrentTurn = EnemyID;
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (CanPlaceHere(new Vector2(x, y)))
                {
                    return;
                }
            }
        }
        if (isAlreadyEnded)
            GameOver();
        else
        {
            EndTurn(true);
        }
    }



    public void GameOver()
    {
        if (ScoreBoardText == null)
        {
            Debug.LogError("ScoreBoardText is null in GameOver method.");
            return;
        }

        if (OthelloCells == null)
        {
            Debug.LogError("OthelloCells is null in GameOver method.");
            return;
        }

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCell cell = OthelloCells[x, y];
                if (cell != null)
                {
                    Button cellButton = cell.GetComponent<Button>();
                    if (cellButton != null)
                    {
                        cellButton.interactable = false;
                    }
                    else
                    {
                        Debug.LogError("Button component is null for cell at (" + x + ", " + y + ")");
                    }
                }
                else
                {
                    Debug.LogError("OthelloCell is null at (" + x + ", " + y + ")");
                }
            }
        }

        int white = CountScoreFor(0);
        int black = CountScoreFor(1);

        if (ScoreBoardText != null)
        {
            if (white > black)
                ScoreBoardText.text = "White wins " + white + ":" + black;
            else if (black > white)
                ScoreBoardText.text = "Black wins " + black + ":" + white;
            else
                ScoreBoardText.text = "Draw! " + white + ":" + black;
        }

        if (ScoreBoard != null)
        {
            ScoreBoard.gameObject.SetActive(true);
        }
        // ... 以下略
    }




    private int CountScoreFor(int owner)
    {
        int count = 0;
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (OthelloCells[x, y].OwnerID == owner)
                {
                    count++;
                }
            }
        }
        return count;
    }


    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions(), TypedLobby.Default);
    }

    [PunRPC]
    public void RpcEndTurn(bool isAlreadyEnded)
    {
        EndTurn(isAlreadyEnded);
    }

    public void ServerPlaceHere(Vector2 location)
    {
        // photonViewがnullでないこと、OthelloBoard.Instanceがnullでないことを確認
        if (photonView != null && photonView.IsMine && OthelloBoard.Instance != null)
        {
            OthelloBoard.Instance.PlaceHere(OthelloCells[(int)location.x, (int)location.y]);
        }
    }
}
