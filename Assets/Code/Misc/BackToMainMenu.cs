using UnityEngine;

public class BackToMainMenu : MonoBehaviour
{

    public void OnTriggerEnter(Collider other) {
        if(other.CompareTag("Player")){
            SceneLoaderScript.LoadScene(0);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

    }
}
