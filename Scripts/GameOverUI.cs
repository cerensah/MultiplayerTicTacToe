using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;
    [SerializeField] private Color tieColor;
    [SerializeField] private Button rematchButton;

    private void Awake(){
        rematchButton.onClick.AddListener( () => {
            GameManager.Instance.RematchRpc();
        });
    }
    private void Start(){
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
        GameManager.Instance.OnGameTied += GameManager_OnGameTied;
        Hide();
    }

    private void GameManager_OnGameTied(object sender, EventArgs e){
        resultText.text = "TIE!";
        resultText.color = tieColor;
        Show();
    }
    private void GameManager_OnRematch(object sender, EventArgs e){
        Hide();
    }
    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e){
        if(e.winPlayerType == GameManager.Instance.GetLocalPlayerType()){
            resultText.text = "YOU WON!";
            resultText.color = winColor;
        } else{
            resultText.text = "YOU LOST!";
            resultText.color = loseColor;
        }
        Show();
    }
    private void Show(){
        gameObject.SetActive(true);
    }

    private void Hide(){
        gameObject.SetActive(false);
    }
}
