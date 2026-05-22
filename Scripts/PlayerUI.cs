using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject crossArrowGameObject;
    [SerializeField] private GameObject circleArrowGameObject;
    [SerializeField] private GameObject crossTextGameObject;
    [SerializeField] private GameObject circleTextGameObject;
    [SerializeField] private TextMeshProUGUI circleScoreText;
    [SerializeField] private TextMeshProUGUI crossScoreText;

    private void Awake(){
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);
        crossTextGameObject.SetActive(false);
        circleTextGameObject.SetActive(false);

        circleScoreText.text = "";
        crossScoreText.text = "";
    }

    private void Start(){
        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
        GameManager.Instance.OnCurrentPlayablePlayerChanged += GameManager_OnCurrentPlayablePlayerChanged;
        GameManager.Instance.OnScoreChanged += GameManager_OnScoreChanged; 
    }
    
    private void GameManager_OnScoreChanged(object sender, System.EventArgs e)
    {
        GameManager.Instance.GetScores(out int playerCrossScore, out int playerCircleScore);
        circleScoreText.text = playerCircleScore.ToString();
        crossScoreText.text = playerCrossScore.ToString();
    }

    private void GameManager_OnCurrentPlayablePlayerChanged(object sender, System.EventArgs e){
        UpdateCurrentArrow();
    }
    private void GameManager_OnGameStarted(object sender, System.EventArgs e){
        if(GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross){
            crossTextGameObject.SetActive(true);
        } else{
             circleTextGameObject.SetActive(true);
        }

        circleScoreText.text = "0";
        crossScoreText.text = "0";

        UpdateCurrentArrow();
    }

    private void UpdateCurrentArrow(){
        if(GameManager.Instance.GetCurrentPlayablePlayerType()== GameManager.PlayerType.Cross){
            crossArrowGameObject.SetActive(true);
            circleArrowGameObject.SetActive(false);
        } else{
            crossArrowGameObject.SetActive(false);
            circleArrowGameObject.SetActive(true);
        }
    }
}
