using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    public void StartGame()
    {
        SceneLoaderScript.LoadScene(1);
    }
}
