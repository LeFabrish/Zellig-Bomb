using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para recargar niveles
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private bool isPaused = false;

    void Update()
    {
        // Reinicio rápido con "R"
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }

        // Pausar con "P" o "Escape"
        if (Keyboard.current != null &&
           (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            TogglePause();
        }
    }

    private void RestartGame()
    {
        // 1. Descongelamos el tiempo (por si el jugador presiona R mientras el juego estaba en pausa)
        Time.timeScale = 1f;

        // 2. Recargamos la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // Congela todas las físicas, movimientos y timers
            Time.timeScale = 0f;
        }
        else
        {
            // Devuelve el juego a su velocidad normal
            Time.timeScale = 1f;
        }
    }
}