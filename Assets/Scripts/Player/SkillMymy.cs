using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerController))]
public class SkillMymy : MonoBehaviour
{
    [Header("Parámetros Mymy")]
    public float mymyFuseTime = 5.0f; // Los 5s de tu GDD

    private PlayerController controller;
    private List<Bomb> myActiveBombs = new List<Bomb>(); // Su detonador remoto

    void Start()
    {
        controller = GetComponent<PlayerController>();

        // Nos "suscribimos" al evento de poner bombas
        controller.OnBombPlanted += HandleBombPlanted;
    }

    void Update()
    {
        // 1. Limpiar la lista de las bombas que ya explotaron por los 5 segundos
        myActiveBombs.RemoveAll(b => !b.gameObject.activeInHierarchy);

        // 2. Detonación Remota con L o C
        if (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame)
        {
            DetonateAllMyBombs();
        }
    }

    // Esta función se dispara automáticamente cuando presionas K o X
    private void HandleBombPlanted(Bomb newBomb)
    {
        // Alteramos el temporizador a 5 segundos
        newBomb.fuseTime = mymyFuseTime;

        // Guardamos la bomba en nuestra lista para controlarla
        myActiveBombs.Add(newBomb);
    }

    private void DetonateAllMyBombs()
    {
        foreach (Bomb b in myActiveBombs)
        {
            if (b != null && b.gameObject.activeInHierarchy)
            {
                b.ForceExplode(); // Usamos la misma función de reacción en cadena
            }
        }
        myActiveBombs.Clear(); // Vaciamos el detonador
    }

    void OnDestroy()
    {
        // Por seguridad, nos desuscribimos si el personaje muere
        if (controller != null) controller.OnBombPlanted -= HandleBombPlanted;
    }
}