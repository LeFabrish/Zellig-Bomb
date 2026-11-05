using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Etapa 4.5: las teclas de pausa/reinicio/menu solo funcionan durante Juego.
/// Al pausar se bloquea PlayerInput, no solamente Time.timeScale.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Referencias de la escena")]
    [SerializeField] private MenuManager menuManager;
    [Tooltip("Opcional: panel UI ubicado dentro de Juego/Canvas, desactivado por defecto.")]
    [SerializeField] private GameObject panelPausa;

    private bool isPaused;
    private readonly List<PlayerInput> entradasPausadas = new List<PlayerInput>();

    public bool EstaPausado => isPaused;

    private void Awake()
    {
        if (menuManager == null)
            menuManager = FindFirstObjectByType<MenuManager>();

        // Evita que una sesion de Play previa deje el editor congelado.
        Time.timeScale = 1f;
        if (panelPausa != null) panelPausa.SetActive(false);
    }

    private void Update()
    {
        // En PanelPrincipal, PanelNivel o PanelPersonajes no hacer NADA.
        if (menuManager == null || !menuManager.PartidaIniciada) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.rKey.wasPressedThisFrame)
        {
            ReiniciarPartida();
            return;
        }

        if (teclado != null && teclado.backspaceKey.wasPressedThisFrame)
        {
            VolverAlMenu();
            return;
        }

        bool pausaTeclado = teclado != null &&
            (teclado.pKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame);
        bool pausaGamepad = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (pausaTeclado || pausaGamepad)
            TogglePause();
    }

    public void TogglePause()
    {
        if (menuManager == null || !menuManager.PartidaIniciada) return;

        if (!isPaused)
        {
            isPaused = true;
            PausarEntradaDeJugadores();
            Time.timeScale = 0f;
            if (panelPausa != null) panelPausa.SetActive(true);
        }
        else
        {
            isPaused = false;
            Time.timeScale = 1f;
            ReanudarEntradaDeJugadores();
            if (panelPausa != null) panelPausa.SetActive(false);
        }
    }

    /// <summary>Opcional: conectar a un boton Continuar de PanelPausa.</summary>
    public void ContinuarPartida()
    {
        if (isPaused) TogglePause();
    }

    /// <summary>Tecla R o boton de UI: conserva nivel y personajes.</summary>
    public void ReiniciarPartida()
    {
        if (menuManager == null || !menuManager.PartidaIniciada) return;
        menuManager.ReiniciarPartida();
    }

    /// <summary>Backspace o boton de UI: nueva seleccion desde PanelPrincipal.</summary>
    public void VolverAlMenu()
    {
        if (menuManager == null || !menuManager.PartidaIniciada) return;
        menuManager.VolverAlMenu();
    }

    private void PausarEntradaDeJugadores()
    {
        entradasPausadas.Clear();

        if (menuManager == null || menuManager.JuegoRoot == null) return;

        // Solo los PlayerInput instanciados dentro de Juego. No desactivamos
        // la interfaz (EventSystem), para que los botones de pausa respondan.
        PlayerInput[] jugadores = menuManager.JuegoRoot.GetComponentsInChildren<PlayerInput>(true);
        foreach (PlayerInput entrada in jugadores)
        {
            if (entrada == null || !entrada.isActiveAndEnabled) continue;
            entradasPausadas.Add(entrada);
            entrada.DeactivateInput();
        }
    }

    private void ReanudarEntradaDeJugadores()
    {
        foreach (PlayerInput entrada in entradasPausadas)
        {
            if (entrada != null && entrada.isActiveAndEnabled)
                entrada.ActivateInput();
        }
        entradasPausadas.Clear();
    }

    private void OnDestroy()
    {
        // Seguridad al parar Play o recargar la escena mientras esta pausada.
        if (isPaused)
            Time.timeScale = 1f;
    }
}
