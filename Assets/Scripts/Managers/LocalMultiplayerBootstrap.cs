using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Etapa 4: no crea jugadores en Start. El menu llama a SpawnSelectedPlayers
/// solo despues de que ambos usuarios confirman y pulsan Jugar.
/// Personaje (Maya/Coco) y dispositivo (teclado/gamepad) son independientes.
/// </summary>
public class LocalMultiplayerBootstrap : MonoBehaviour
{
    [Header("Prefabs originales (contienen PlayerInput)")]
    [SerializeField] private GameObject mayaPrefab;
    [SerializeField] private GameObject cocoPrefab;

    [Header("Spawns: se reutilizan los campos existentes")]
    [Tooltip("Punto del Jugador 1, independientemente del personaje elegido.")]
    [SerializeField] private Transform mayaSpawn;
    [Tooltip("Punto del Jugador 2, independientemente del personaje elegido.")]
    [SerializeField] private Transform cocoSpawn;

    [Header("Raiz de la partida: GameObject Juego")]
    [Tooltip("Los personajes instanciados se colocaran debajo de Juego, no del menu.")]
    [SerializeField] private Transform gameplayRoot;

    private bool playersSpawned;

    /// <param name="player1Character">0=Maya, 1=Coco.</param>
    /// <param name="player2Character">0=Maya, 1=Coco.</param>
    /// <returns>true cuando ambos personajes fueron creados correctamente.</returns>
    public bool SpawnSelectedPlayers(int player1Character, int player2Character)
    {
        if (playersSpawned)
        {
            Debug.LogWarning("LocalMultiplayerBootstrap: ya se inicio la partida.", this);
            return false;
        }

        if (mayaPrefab == null || cocoPrefab == null || mayaSpawn == null ||
            cocoSpawn == null || gameplayRoot == null)
        {
            Debug.LogError("LocalMultiplayerBootstrap: faltan prefabs, spawns o la raiz Juego en el Inspector.", this);
            return false;
        }

        if (!gameplayRoot.gameObject.activeInHierarchy)
        {
            Debug.LogError("LocalMultiplayerBootstrap: primero debe activarse Juego.", this);
            return false;
        }

        if ((player1Character != 0 && player1Character != 1) ||
            (player2Character != 0 && player2Character != 1))
        {
            Debug.LogError("LocalMultiplayerBootstrap: personaje invalido (solo Maya=0 o Coco=1).", this);
            return false;
        }

        // Se conservan los dos esquemas ya probados en la Etapa 3.
        if (Keyboard.current == null || Mouse.current == null)
        {
            Debug.LogError("No hay teclado/mouse para Player 1 (Keyboard&Mouse).", this);
            return false;
        }

        if (Gamepad.current == null)
        {
            Debug.LogError("No hay Gamepad para Player 2. Conecta PC Remote antes de pulsar Jugar.", this);
            return false;
        }

        GameObject firstPrefab = GetPrefab(player1Character);
        GameObject secondPrefab = GetPrefab(player2Character);

        PlayerInput first = PlayerInput.Instantiate(
            firstPrefab, 0, "Keyboard&Mouse", -1, Keyboard.current, Mouse.current);

        if (first == null)
        {
            Debug.LogError("No se pudo crear el Jugador 1.", this);
            return false;
        }

        first.transform.SetParent(gameplayRoot, true);
        first.transform.position = mayaSpawn.position;

        PlayerInput second = PlayerInput.Instantiate(
            secondPrefab, 1, "Gamepad", -1, Gamepad.current);

        if (second == null)
        {
            Destroy(first.gameObject);
            Debug.LogError("No se pudo crear el Jugador 2; se cancelo el inicio.", this);
            return false;
        }

        second.transform.SetParent(gameplayRoot, true);
        second.transform.position = cocoSpawn.position;
        playersSpawned = true;

        Debug.Log($"Zellig-Bomb: J1={firstPrefab.name} ({first.currentControlScheme}), " +
                  $"J2={secondPrefab.name} ({second.currentControlScheme}).", this);
        return true;
    }

    private GameObject GetPrefab(int characterIndex)
    {
        return characterIndex == 0 ? mayaPrefab : cocoPrefab;
    }
}
