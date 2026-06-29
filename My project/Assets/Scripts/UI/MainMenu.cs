using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
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

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}