using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

/// <summary>
///  Prueba termporal de multijugador local: Maya = teclado; Coco = PC Remote.
///  No gestiona seleccion de personajes ni victoria/derrota.
/// </summary>

public class LocalMultiplayerBootstrap: MonoBehaviour
{
	[Header("Prefabs (Deben incluir playerInput)")]
	[SerializeField] private GameObject mayaPrefab;
    [SerializeField] private GameObject cocoPrefab;

	[Header("Spawn Points libres de obstáculos")]
	[SerializeField] private Transform mayaSpawn;
    [SerializeField] private Transform cocoSpawn;


    // Use this for initialization
    private void Start()
	{
		if(mayaPrefab == null || cocoPrefab == null || mayaSpawn == null || cocoSpawn == null)
		{
			Debug.LogError("LocalMultiplayerBootstrap: faltan prefabs o puntos de spawn.", this);
			return;
		}

		if(Keyboard.current == null || Mouse.current == null)
		{
			Debug.LogError("No se detecta teclado o mouse para el esquema Keyboard/Mouse.", this);
			return;
		}

		if(Gamepad.current == null)
        {
            Debug.LogError("No hay Gamepad. Conecta PC Remote antes de pulsar Start.", this);
            return;
        }

		PlayerInput maya = PlayerInput.Instantiate(
			mayaPrefab, 0, "Keyboard&Mouse", -1,
			Keyboard.current, Mouse.current
			);
		if(maya == null)
		{
            Debug.LogError("No se pudo instanciar Maya.", this);
            return;
        }
		maya.transform.position = mayaSpawn.position;

		PlayerInput coco = PlayerInput.Instantiate(
			cocoPrefab, 1, "Gamepad", -1,
			Gamepad.current
			);
		if(coco == null)
		{
            Debug.LogError("No se pudo instanciar Coco.", this);
            return;
        }
		coco.transform.position = cocoSpawn.position;
		Debug.Log($"Multijugador local listo: Maya= {maya.currentControlScheme}, Coco= {coco.currentControlScheme}");
    }

	// Update is called once per frame
	void Update()
	{

	}
}