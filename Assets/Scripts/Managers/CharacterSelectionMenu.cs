using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menu local de DOS selecciones independientes. No instancia prefabs.
/// J1 elige con teclado; J2 elige con PC Remote (Gamepad del Input System).
/// 0 = Maya y 1 = Coco. Se permiten Maya/Maya y Coco/Coco.
/// Colocar el componente en PanelPersonajes (que empieza desactivado).
/// </summary>
public class CharacterSelectionMenu : MonoBehaviour
{
    [Header("Interfaz - referencias en PanelPersonajes")]
    [SerializeField] private Button botonJugar;
    [SerializeField] private TMP_Text textoJugador1;
    [SerializeField] private TMP_Text textoJugador2;

    public int PersonajeJugador1 { get; private set; } = 0;
    public int PersonajeJugador2 { get; private set; } = 1;
    private bool confirmadoJugador1;
    private bool confirmadoJugador2;
    private bool ultimoGamepadPresente;

    public bool AmbosConfirmados => confirmadoJugador1 && confirmadoJugador2 && Gamepad.current != null;

    private InputAction j1Izquierda;
    private InputAction j1Derecha;
    private InputAction j1Confirmar;
    private InputAction j2Izquierda;
    private InputAction j2Derecha;
    private InputAction j2Confirmar;
    private InputAction[] acciones;

    private void Awake()
    {
        j1Izquierda = NuevaAccion("J1 Izquierda", "<Keyboard>/a", "<Keyboard>/leftArrow");
        j1Derecha   = NuevaAccion("J1 Derecha", "<Keyboard>/d", "<Keyboard>/rightArrow");
        // F evita que Enter, empleado por el EventSystem, confirme y pulse un boton accidentalmente.
        j1Confirmar = NuevaAccion("J1 Confirmar", "<Keyboard>/f");

        j2Izquierda = NuevaAccion("J2 Izquierda", "<Gamepad>/dpad/left", "<Gamepad>/leftStick/left");
        j2Derecha   = NuevaAccion("J2 Derecha", "<Gamepad>/dpad/right", "<Gamepad>/leftStick/right");
        // X (Xbox buttonWest) evita el boton A, usado por Submit en el EventSystem.
        j2Confirmar = NuevaAccion("J2 Confirmar", "<Gamepad>/buttonWest");

        acciones = new[] { j1Izquierda, j1Derecha, j1Confirmar, j2Izquierda, j2Derecha, j2Confirmar };
    }

    private static InputAction NuevaAccion(string nombre, params string[] rutas)
    {
        InputAction accion = new InputAction(nombre, InputActionType.Button);
        foreach (string ruta in rutas)
            accion.AddBinding(ruta);
        return accion;
    }

    private void OnEnable()
    {
        PersonajeJugador1 = 0;
        PersonajeJugador2 = 1;
        confirmadoJugador1 = false;
        confirmadoJugador2 = false;
        ultimoGamepadPresente = Gamepad.current != null;

        if (acciones != null)
            foreach (InputAction accion in acciones) accion.Enable();
        ActualizarUI();
    }

    private void OnDisable()
    {
        if (acciones != null)
            foreach (InputAction accion in acciones) accion.Disable();
    }

    private void OnDestroy()
    {
        if (acciones != null)
            foreach (InputAction accion in acciones) accion.Dispose();
    }

    private void Update()
    {
        if (j1Izquierda.WasPressedThisFrame()) ElegirJugador1(0);
        if (j1Derecha.WasPressedThisFrame()) ElegirJugador1(1);
        if (j1Confirmar.WasPressedThisFrame())
        {
            confirmadoJugador1 = true;
            ActualizarUI();
        }

        if (j2Izquierda.WasPressedThisFrame()) ElegirJugador2(0);
        if (j2Derecha.WasPressedThisFrame()) ElegirJugador2(1);
        if (j2Confirmar.WasPressedThisFrame() && Gamepad.current != null)
        {
            confirmadoJugador2 = true;
            ActualizarUI();
        }

        bool gamepadPresente = Gamepad.current != null;
        if (gamepadPresente != ultimoGamepadPresente)
        {
            ultimoGamepadPresente = gamepadPresente;
            if (!gamepadPresente) confirmadoJugador2 = false;
            ActualizarUI();
        }
    }

    /// <summary>Opcional: conectar BtnMaya.OnClick para elegir Maya con el mouse (J1).</summary>
    public void ElegirMayaConMouse() => ElegirJugador1(0);

    /// <summary>Opcional: conectar BtnCoco.OnClick para elegir Coco con el mouse (J1).</summary>
    public void ElegirCocoConMouse() => ElegirJugador1(1);

    private void ElegirJugador1(int eleccion)
    {
        if (PersonajeJugador1 == eleccion) return;
        PersonajeJugador1 = eleccion;
        confirmadoJugador1 = false;
        ActualizarUI();
    }

    private void ElegirJugador2(int eleccion)
    {
        if (PersonajeJugador2 == eleccion) return;
        PersonajeJugador2 = eleccion;
        confirmadoJugador2 = false;
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoJugador1 != null)
            textoJugador1.text = "JUGADOR 1 - TECLADO\n" + Nombre(PersonajeJugador1) + "\n" +
                (confirmadoJugador1 ? "LISTO" : "A / D: ELEGIR    F: CONFIRMAR");

        if (textoJugador2 != null)
            textoJugador2.text = "JUGADOR 2 - PC REMOTE\n" + Nombre(PersonajeJugador2) + "\n" +
                (Gamepad.current == null ? "CONECTA PC REMOTE" :
                 confirmadoJugador2 ? "LISTO" : "CRUCETA: ELEGIR    X: CONFIRMAR");

        if (botonJugar != null)
            botonJugar.interactable = AmbosConfirmados;
    }

    private static string Nombre(int index) => index == 0 ? "MAYA" : "COCO";
}
