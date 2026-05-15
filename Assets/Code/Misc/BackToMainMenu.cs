using UnityEngine;

public class BackToMainMenu : MonoBehaviour
{

    public void OnTriggerEnter(){
        SceneLoaderScript.LoadScene(0);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
