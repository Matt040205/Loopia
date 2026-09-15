using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public string cenaGameOver = "GameOver"; // Nome da cena de Game Over

    public void IrParaGameOver()
    {
        SceneManager.LoadScene(cenaGameOver);
    }
}