using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public class UIManager : MonoBehaviour
{
    public void LoadLevel(string sceneName)
    {
        int levelPilihan = 1;

        if (sceneName == "Prototype Chess")
        {
            levelPilihan = 2;
        }

        PlayerPrefs.SetInt("LevelTerpilih", levelPilihan);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        Application.Quit();

        EditorApplication.isPlaying = false;
    }

    public void PauseGameplay()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGameplay()
    {
        Time.timeScale = 1f;
    }
}