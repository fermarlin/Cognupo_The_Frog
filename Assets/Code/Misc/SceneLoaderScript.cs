using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderScript
{
    public static void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
