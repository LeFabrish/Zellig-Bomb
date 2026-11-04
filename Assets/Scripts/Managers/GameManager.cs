using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para recargar niveles
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }

        if (Keyboard.current != null &&
           (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            TogglePause();
        }
    }

    private void RestartGame()
    {
        // Descongelamos y recargamos la escena
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}