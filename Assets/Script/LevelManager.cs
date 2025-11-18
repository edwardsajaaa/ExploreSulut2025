using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public void UlangiLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void KeMainMenu(string namaSceneMenu)
    {
        SceneManager.LoadScene(namaSceneMenu);
    }
}