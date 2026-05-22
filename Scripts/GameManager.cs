using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance {get; private set;}

    public event EventHandler<OnClickedOnGridPositionEventArgs> onClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs: EventArgs {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public enum PlayerType{
        None,
        Cross,
        Circle
    }

    public enum Orientation{
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB
    }

    public struct Line {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;

    public class OnGameWinEventArgs : EventArgs {
        public Line line;
        public PlayerType winPlayerType;
    }
    public event EventHandler OnCurrentPlayablePlayerChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;


    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();

    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;

    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();

    private void Awake(){
        if(Instance != null){
            Debug.LogError("More than once GameManager Instance");
        }
        Instance = this;

        playerTypeArray = new PlayerType[3,3];

        lineList = new List<Line>{
            //Horizontal
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(1,0),new Vector2Int(2,0)},
                centerGridPosition = new Vector2Int(1,0),
                orientation = Orientation.Horizontal,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,1), new Vector2Int(1,1),new Vector2Int(2,1)},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Horizontal,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,2), new Vector2Int(1,2),new Vector2Int(2,2)},
                centerGridPosition = new Vector2Int(1,2),
                orientation = Orientation.Horizontal,
            },

            //Vertical                   
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(0,1),new Vector2Int(0,2)},
                centerGridPosition = new Vector2Int(0,1),
                orientation = Orientation.Vertical,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(1,0), new Vector2Int(1,1),new Vector2Int(1,2)},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Vertical,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(2,0), new Vector2Int(2,1),new Vector2Int(2,2)},
                centerGridPosition = new Vector2Int(2,1),
                orientation = Orientation.Vertical,
            },


            //Diagonals                   
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(1,1),new Vector2Int(2,2)},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalA,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,2), new Vector2Int(1,1),new Vector2Int(2,0)},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalB,
            },
        };
    }

    public override void OnNetworkSpawn(){
        if(NetworkManager.Singleton.LocalClientId == 0){
            localPlayerType = PlayerType.Cross;
        } else{
            localPlayerType = PlayerType.Circle;
        }

        OnCurrentPlayablePlayerChanged?.Invoke(this, EventArgs.Empty);

        if(IsServer){
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnected;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) => {
            OnCurrentPlayablePlayerChanged?.Invoke(this, EventArgs.Empty);
        };

        playerCrossScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
        playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    private void NetworkManager_OnClientConnected(ulong obj){
        if(NetworkManager.Singleton.ConnectedClientsList.Count == 2){
            //Start Game
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc(){
           OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType){
        Debug.Log("ClickedOnGridPositionRpc " + x + " , " + y + currentPlayablePlayerType);

        if(playerType != currentPlayablePlayerType.Value){
            return;
        }

        if(playerTypeArray[x,y]!= PlayerType.None){
            //already occupied
            return;
        }

        playerTypeArray[x,y] = playerType;

        TriggerOnPlacedObjectRpc();

        onClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs{
            x = x,
            y = y,
            playerType = playerType,
        });

        switch(currentPlayablePlayerType.Value){
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;
        }

        TestWinner();
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    private bool TestWinnerLine(Line line){
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
            );
    }

    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType){
        return aPlayerType != PlayerType.None &&
        aPlayerType == bPlayerType &&
        aPlayerType== cPlayerType;
    }
    private void TestWinner(){
        for( int i = 0; i<lineList.Count ; i++) {
                Line line = lineList[i];
                if(TestWinnerLine(line))
                {
                    Debug.Log( "Winner!");
                    currentPlayablePlayerType.Value = PlayerType.None;
                    PlayerType winPlayerType = playerTypeArray[line.centerGridPosition.x,line.centerGridPosition.y];
                    switch(winPlayerType){
                        case PlayerType.Cross:
                            playerCrossScore.Value ++;
                            break;
                        case PlayerType.Circle:
                            playerCircleScore.Value ++;
                            break;
                    }

                    TriggerOnGameWinRpc(i, winPlayerType);

                    break;
                }
        }

        bool hasTie = true;
        for(int x = 0 ; x < playerTypeArray.GetLength(0); x++){
            for(int y = 0 ; y < playerTypeArray.GetLength(0); y++){
                if(playerTypeArray[x,y] == PlayerType.None){
                    hasTie = false;
                    break;
                }

            }
        }

        if(hasTie){
           TriggerOnGameTiedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc(){
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIdx, PlayerType winPlayerType){
        Line line = lineList[lineIdx];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs{
                line = line,
                winPlayerType = winPlayerType,
        });
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc(){
        for( int x = 0; x < playerTypeArray.GetLength(0); x++){
            for( int y = 0; y < playerTypeArray.GetLength(0); y++){
                playerTypeArray[x,y] = PlayerType.None;
            }
        }
        currentPlayablePlayerType.Value = PlayerType.Cross;

        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc(){
        OnRematch?.Invoke(this, EventArgs.Empty);
    }
    public PlayerType GetLocalPlayerType(){
        return localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType(){
        return currentPlayablePlayerType.Value;
    }

    public void GetScores(out int playerCrossScore, out int playerCircleScore){
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }

}
