using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class SkillCoco : MonoBehaviour
{
    [Header("Parámetros del Dash (Coco)")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.2f; // Tiempo de impulso
    public float cooldown = 3.0f; // Tiempo de enfriamiento

    private bool canDash = true;
    private Rigidbody2D rb;
    private PlayerController controller;
    private Vector2 lastDirection = Vector2.down; // Dirección por defecto

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        // 1. Rastrear siempre hacia dónde mira el jugador para saber hacia dónde hacer el dash
        if (!controller.isSkillActive)
        {
            float inputX = 0f; float inputY = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputY = 1f;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputY = -1f;

            if (inputX != 0 || inputY != 0)
            {
                if (inputX != 0) inputY = 0; // Mantener la restricción de cruz
                lastDirection = new Vector2(inputX, inputY);
            }
        }

        // 2. Activar la habilidad con L o C
        if (canDash && (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame))
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        // Bloquear movimiento normal y estado
        canDash = false;
        controller.isSkillActive = true;

        // Fase 4: Aquí desactivaremos las colisiones con los enemigos temporalmente.
        // Por ahora, solo aplicamos la velocidad pura.
        rb.linearVelocity = lastDirection.normalized * dashSpeed;

        // Esperar los 0.2 segundos que dura el impulso
        yield return new WaitForSeconds(dashDuration);

        // Devolver el control al PlayerController
        controller.isSkillActive = false;

        // Iniciar el cronómetro de enfriamiento de 3 segundos
        yield return new WaitForSeconds(cooldown);
        canDash = true;
    }
}