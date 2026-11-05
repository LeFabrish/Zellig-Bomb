using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Etapa 4.5: mantiene la navegacion de la etapa 4 y permite reiniciar
/// la partida (mismo nivel y personajes) o volver al menu principal.
/// Se recarga la escena para restaurar tambien bloques, bombas y explosiones.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject PanelPrincipal;
    [SerializeField] private GameObject nivelPanel;
    [SerializeField] private GameObject PanelPersonajes;

    [Header("Etapa 4 - inicio de partida")]
    [SerializeField] private GameObject juego;
    [SerializeField] private CharacterSelectionMenu seleccionPersonajes;
    [SerializeField] private LocalMultiplayerBootstrap localMultiplayer;

    private readonly Stack<GameObject> HistorialPaneles = new Stack<GameObject>();
    private GameObject PanelActual;
    private bool partidaIniciada;

    public bool PartidaIniciada => partidaIniciada;
    public GameObject JuegoRoot => juego;

    // Solo se guardan datos simples entre recargas, NUNCA referencias a objetos Unity.
    private struct DatosReinicio
    {
        public int nivel;
        public int personajeJugador1;
        public int personajeJugador2;
    }

    private static bool hayReinicioPendiente;
    private static DatosReinicio datosReinicio;

    // Evita datos residuales al entrar en Play si Domain Reload esta desactivado.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InicializarEstadoEstatico()
    {
        hayReinicioPendiente = false;
        datosReinicio = default(DatosReinicio);
    }

    private void Awake()
    {
        // El escenario nunca se muestra antes de Jugar.
        if (juego != null)
            juego.SetActive(false);
    }

    private void Start()
    {
        ShowInitialPanel(PanelPrincipal);

        if (!hayReinicioPendiente)
            return;

        // Consumir la solicitud ANTES de iniciar para impedir bucles de recarga.
        DatosReinicio partidaAnterior = datosReinicio;
        hayReinicioPendiente = false;

        if (GameSession.Instance == null)
        {
            Debug.LogError("MenuManager: no existe GameSession para recuperar el nivel del reinicio.", this);
            return;
        }

        GameSession.Instance.SetSelectedLevel(partidaAnterior.nivel);

        if (!IniciarPartidaInterna(partidaAnterior.personajeJugador1,
                                  partidaAnterior.personajeJugador2))
        {
            // Si falla (por ejemplo, falta PC Remote), quedamos en menu.
            ShowInitialPanel(PanelPrincipal);
            Debug.LogWarning("No se pudo reiniciar la partida; se muestra PanelPrincipal.", this);
        }
    }

    private void ShowInitialPanel(GameObject panel)
    {
        if (PanelPrincipal == null || nivelPanel == null || PanelPersonajes == null || panel == null)
        {
            Debug.LogError("MenuManager: falta un panel del menu en el Inspector.", this);
            return;
        }

        PanelPrincipal.SetActive(false);
        nivelPanel.SetActive(false);
        PanelPersonajes.SetActive(false);
        panel.SetActive(true);
        PanelActual = panel;
        HistorialPaneles.Clear();
    }

    public void OpenPanel(GameObject sigPanel)
    {
        if (partidaIniciada || sigPanel == null) return;
        if (sigPanel == juego)
        {
            Debug.LogWarning("Juego no es un panel. Usa MenuManager.IniciarPartida().", this);
            return;
        }

        if (PanelActual != null)
        {
            HistorialPaneles.Push(PanelActual);
            PanelActual.SetActive(false);
        }

        sigPanel.SetActive(true);
        PanelActual = sigPanel;
    }

    public void GoBack()
    {
        if (partidaIniciada || HistorialPaneles.Count == 0)
            return;

        if (PanelActual != null)
            PanelActual.SetActive(false);

        PanelActual = HistorialPaneles.Pop();
        if (PanelActual != null)
            PanelActual.SetActive(true);
    }

    /// <summary>Conectar a BtnJugar.OnClick; exige dos confirmaciones.</summary>
    public void IniciarPartida()
    {
        if (partidaIniciada) return;
        if (seleccionPersonajes == null)
        {
            Debug.LogError("MenuManager: falta SeleccionPersonajes en el Inspector.", this);
            return;
        }

        if (PanelActual != PanelPersonajes || !seleccionPersonajes.AmbosConfirmados)
        {
            Debug.LogWarning("Los dos jugadores deben confirmar antes de Jugar.", this);
            return;
        }

        IniciarPartidaInterna(seleccionPersonajes.PersonajeJugador1,
                             seleccionPersonajes.PersonajeJugador2);
    }

    // Se usa tanto al pulsar Jugar como despues de reiniciar la escena.
    private bool IniciarPartidaInterna(int personajeJ1, int personajeJ2)
    {
        if (partidaIniciada) return false;
        if (localMultiplayer == null || juego == null || GameSession.Instance == null)
        {
            Debug.LogError("MenuManager: faltan Juego, LocalMultiplayer o GameSession.", this);
            return false;
        }

        juego.SetActive(true);
        GameSession.Instance.ActivarLayout();

        bool jugadoresCreados = localMultiplayer.SpawnSelectedPlayers(personajeJ1, personajeJ2);
        if (!jugadoresCreados)
        {
            juego.SetActive(false);
            return false;
        }

        partidaIniciada = true;
        if (PanelActual != null)
            PanelActual.SetActive(false);
        PanelActual = null;
        HistorialPaneles.Clear();
        return true;
    }

    /// <summary>Reinicia conservando NIVEL y PERSONAJES, no el estado del mapa.</summary>
    public void ReiniciarPartida()
    {
        if (!partidaIniciada) return;
        if (seleccionPersonajes == null || GameSession.Instance == null)
        {
            Debug.LogError("MenuManager: faltan SeleccionPersonajes o GameSession para reiniciar.", this);
            return;
        }

        Scene escena = SceneManager.GetActiveScene();
        if (!SePuedeRecargar(escena)) return;

        datosReinicio = new DatosReinicio
        {
            nivel = GameSession.Instance.selectedLevel,
            personajeJugador1 = seleccionPersonajes.PersonajeJugador1,
            personajeJugador2 = seleccionPersonajes.PersonajeJugador2
        };
        hayReinicioPendiente = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena.buildIndex);
    }

    /// <summary>Recarga la escena y vuelve a PanelPrincipal con nueva seleccion.</summary>
    public void VolverAlMenu()
    {
        if (!partidaIniciada) return;

        Scene escena = SceneManager.GetActiveScene();
        if (!SePuedeRecargar(escena)) return;

        hayReinicioPendiente = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena.buildIndex);
    }

    private bool SePuedeRecargar(Scene escena)
    {
        if (escena.buildIndex >= 0) return true;

        Debug.LogError("Agrega '" + escena.name + "' a File > Build Profiles > Scene List " +
                       "para poder reiniciar y volver al menu con SceneManager.LoadScene.", this);
        return false;
    }
}
