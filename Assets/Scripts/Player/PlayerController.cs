using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Estado del jugador")]
    public bool isSkillActive = false; 
    public Action<Bomb> OnBombPlanted; // Evento para notificar cuando se planta una bomba

    [Header("Estadísticas Base")]
    public int maxBombs = 1;      // Límite de bombas en pantalla
    public int explosionRadius = 1;
    public float speed = 4.2f; // Velocidad de movimiento del jugador
    public float snapSpeed = 3.0f; // Velocidad de ajuste al deslizarse por esquinas

    [Header("Ajustes de Sensores (Raycasts)")]
    public float rayOffset = 0.45f;
    public float forwardOffset = 0.45f;
    public float checkDistance = 0.05f;
    public LayerMask solidLayers; // Filtrar colisiones

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Laseres ignoran los Triggers
        Physics2D.queriesHitTriggers = false;
    }

    void Update()
    {
        if (isSkillActive) return;
        float inputX = 0f;
        float inputY = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputY = 1f;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputY = -1f;
        }

        if (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame) 
        {
            if (GetActiveBombCount() < maxBombs)
            {
                PlantBomb();
            }
        }

        if (inputX != 0) inputY = 0; 
        moveInput = new Vector2(inputX, inputY);
    }
   

    void FixedUpdate()
    {
        if (isSkillActive) return;
        Vector2 currentVelocity = moveInput * speed;
        Vector2 pos = rb.position;

        // 2. Lógica de Corner-Cutting con Sensores y RAYO CENTRAL
        if (moveInput.y > 0) // Moviéndose ARRIBA
        {
            Vector2 originLeft = pos + new Vector2(-rayOffset, forwardOffset);
            Vector2 originCenter = pos + new Vector2(0f, forwardOffset); // NUEVO RAYO
            Vector2 originRight = pos + new Vector2(rayOffset, forwardOffset);

            bool hitLeft = Physics2D.Raycast(originLeft, Vector2.up, checkDistance, solidLayers);
            bool hitCenter = Physics2D.Raycast(originCenter, Vector2.up, checkDistance, solidLayers); // DETECCIÓN
            bool hitRight = Physics2D.Raycast(originRight, Vector2.up, checkDistance, solidLayers);

            // SOLO desliza si el centro está libre (no estás chocando de cara)
            if (!hitCenter)
            {
                if (hitLeft && !hitRight) currentVelocity.x = snapSpeed;
                else if (hitRight && !hitLeft) currentVelocity.x = -snapSpeed;
            }
        }
        else if (moveInput.y < 0) // Moviéndose ABAJO
        {
            Vector2 originLeft = pos + new Vector2(-rayOffset, -forwardOffset);
            Vector2 originCenter = pos + new Vector2(0f, -forwardOffset);
            Vector2 originRight = pos + new Vector2(rayOffset, -forwardOffset);

            bool hitLeft = Physics2D.Raycast(originLeft, Vector2.down, checkDistance, solidLayers);
            bool hitCenter = Physics2D.Raycast(originCenter, Vector2.down, checkDistance, solidLayers);
            bool hitRight = Physics2D.Raycast(originRight, Vector2.down, checkDistance, solidLayers);

            if (!hitCenter)
            {
                if (hitLeft && !hitRight) currentVelocity.x = snapSpeed;
                else if (hitRight && !hitLeft) currentVelocity.x = -snapSpeed;
            }
        }
        else if (moveInput.x > 0) // Moviéndose DERECHA
        {
            Vector2 originTop = pos + new Vector2(forwardOffset, rayOffset);
            Vector2 originCenter = pos + new Vector2(forwardOffset, 0f);
            Vector2 originBottom = pos + new Vector2(forwardOffset, -rayOffset);

            bool hitTop = Physics2D.Raycast(originTop, Vector2.right, checkDistance, solidLayers);
            bool hitCenter = Physics2D.Raycast(originCenter, Vector2.right, checkDistance, solidLayers);
            bool hitBottom = Physics2D.Raycast(originBottom, Vector2.right, checkDistance, solidLayers);

            if (!hitCenter)
            {
                if (hitTop && !hitBottom) currentVelocity.y = -snapSpeed;
                else if (hitBottom && !hitTop) currentVelocity.y = snapSpeed;
            }
        }
        else if (moveInput.x < 0) // Moviéndose IZQUIERDA
        {
            Vector2 originTop = pos + new Vector2(-forwardOffset, rayOffset);
            Vector2 originCenter = pos + new Vector2(-forwardOffset, 0f);
            Vector2 originBottom = pos + new Vector2(-forwardOffset, -rayOffset);

            bool hitTop = Physics2D.Raycast(originTop, Vector2.left, checkDistance, solidLayers);
            bool hitCenter = Physics2D.Raycast(originCenter, Vector2.left, checkDistance, solidLayers);
            bool hitBottom = Physics2D.Raycast(originBottom, Vector2.left, checkDistance, solidLayers);

            if (!hitCenter)
            {
                if (hitTop && !hitBottom) currentVelocity.y = -snapSpeed;
                else if (hitBottom && !hitTop) currentVelocity.y = snapSpeed;
            }
        }

        // 3. Aplicamos la velocidad final
        rb.linearVelocity = currentVelocity;
    }

    private void PlantBomb()
    {
        float snapX = Mathf.Round(rb.position.x);
        float snapY = Mathf.Round(rb.position.y);
        Vector2 gridPosition = new Vector2(snapX, snapY);

        GameObject bombObj = ObjectPooler.Instance.GetPooledBomb();
        if (bombObj != null)
        {
            bombObj.transform.position = gridPosition;
            Bomb bombScript = bombObj.GetComponent<Bomb>();

            // 1. PASAR EL RANGO DE EXPLOSIÓN DEL JUGADOR A LA BOMBA
            bombScript.explosionRadius = this.explosionRadius;

            // EL GANCHO: Permite a las habilidades modificar la bomba ANTES de activarla
            OnBombPlanted?.Invoke(bombScript);

            bombObj.SetActive(true);
        }
    }
    private int GetActiveBombCount()
    {
        // Consultamos la lista directamente en el almacén. ¡Sin delay!
        return ObjectPooler.Instance.GetActiveBombCount();
    }
    // Función llamada desde PowerUp.cs para activar retroalimentación visual
    public void TriggerPowerUpFeedback()
    {
        StartCoroutine(PowerUpGlowRoutine());
    }

    private IEnumerator PowerUpGlowRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;

            // Animación temporal en código: parpadeo rápido en azul cyan
            for (int i = 0; i < 3; i++)
            {
                sr.color = new Color(0.2f, 0.8f, 1f, 1f); // Tono azul/cyan brillante
                yield return new WaitForSeconds(0.08f);
                sr.color = originalColor;
                yield return new WaitForSeconds(0.08f);
            }
        }
    }
}