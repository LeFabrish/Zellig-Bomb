using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{

    [Header("Estado del jugador")]
    public bool isSkillActive = false;
    public bool isDead = false;
    public Action<Bomb> OnBombPlanted;

    [Header("HUD")]
    [SerializeField] public List<HudStats> hudStats;

    [Header("Estadísticas Base")]
    public int maxBombs = 1;
    public int explosionRadius = 1;
    public float speed = 4.2f;
    public float snapSpeed = 3.0f;

    [Header("Ajustes de Sensores (Raycasts)")]
    public float rayOffset = 0.38f;
    public float forwardOffset = 0.40f;
    public float checkDistance = 0.30f;
    public LayerMask solidLayers;

    public Vector2 lastDirection = Vector2.down;

    [HideInInspector] public Animator anim;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Vector2 moveInput;
    private InputAction moveAction;
    private InputAction plantBombAction;
    private Vector2 startPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        startPosition = transform.position;
        PlayerInput playerInput = GetComponent<PlayerInput>();

        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{name}: falta PlayerInput con un input Actions Asset asignado.", this);
            enabled = false;
            return;
        }

        moveAction = playerInput.actions.FindAction("Player/Move");
        if (moveAction == null)
        {
            Debug.LogError($"{name}: no existe la acción 'Player/Move' en el Input Actions Asset.", this);
            enabled = false;
            return;
        }

        plantBombAction = playerInput.actions.FindAction("Player/PlantBomb");
        if (plantBombAction == null)
        {
            Debug.LogError($"{name}: no existe la acción 'Player/PlantBomb' en el Input Actions Asset.", this);
        }

        Physics2D.queriesHitTriggers = false;
        ActualizarHUD();
    }

    void Update()
    {
        // Movimiento y bombas leen unicamente las acciones de este PLayerInput
        // Skill y SkillCoco leeran Player/Skill del mismo PlayerInput
        if (isDead || isSkillActive)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Cada PlayerInpyt lee su copia de acciones y su propio dispositivo emparejado.
        if (plantBombAction != null && plantBombAction.WasPressedThisFrame() &&
            ObjectPooler.Instance != null && GetActiveBombCount() < maxBombs)
        {
            PlantBomb();
        }

        Vector2 rawInput = moveAction.ReadValue<Vector2>();
        const float deadZone = 0.2f;

        // Evita movimiento involuntario por el stick analógico de PC Remote.
        if (rawInput.sqrMagnitude < deadZone * deadZone)
        {
            moveInput = Vector2.zero;
        }
        else if (Mathf.Abs(rawInput.x) >= Mathf.Abs(rawInput.y))
        {
            moveInput = new Vector2(Mathf.Sign(rawInput.x), 0f);
        }
        else
        {
            moveInput = new Vector2(0f, Mathf.Sign(rawInput.y));
        }

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput;
        }

        if (anim != null)
        {
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
            anim.SetFloat("Speed", moveInput.magnitude);
        }
    }
    void FixedUpdate()
    {
        if (isSkillActive || isDead) return;

        Vector2 currentVelocity = moveInput * speed;
        Vector2 pos = rb.position;

        if (moveInput.y > 0)
        {
            Vector2 originLeft = pos + new Vector2(-rayOffset, forwardOffset);
            Vector2 originRight = pos + new Vector2(rayOffset, forwardOffset);

            bool hitLeft = IsMovementBlocked(originLeft, Vector2.up);
            bool hitRight = IsMovementBlocked(originRight, Vector2.up);

            if (hitLeft && !hitRight) { currentVelocity.x = snapSpeed; currentVelocity.y = 0; }
            else if (hitRight && !hitLeft) { currentVelocity.x = -snapSpeed; currentVelocity.y = 0; }
        }
        else if (moveInput.y < 0)
        {
            Vector2 originLeft = pos + new Vector2(-rayOffset, -forwardOffset);
            Vector2 originRight = pos + new Vector2(rayOffset, -forwardOffset);

            bool hitLeft = IsMovementBlocked(originLeft, Vector2.down);
            bool hitRight = IsMovementBlocked(originRight, Vector2.down);

            if (hitLeft && !hitRight) { currentVelocity.x = snapSpeed; currentVelocity.y = 0; }
            else if (hitRight && !hitLeft) { currentVelocity.x = -snapSpeed; currentVelocity.y = 0; }
        }
        else if (moveInput.x > 0)
        {
            Vector2 originTop = pos + new Vector2(forwardOffset, rayOffset);
            Vector2 originBottom = pos + new Vector2(forwardOffset, -rayOffset);

            bool hitTop = IsMovementBlocked(originTop, Vector2.right);
            bool hitBottom = IsMovementBlocked(originBottom, Vector2.right);

            if (hitTop && !hitBottom) { currentVelocity.y = -snapSpeed; currentVelocity.x = 0; }
            else if (hitBottom && !hitTop) { currentVelocity.y = snapSpeed; currentVelocity.x = 0; }
        }
        else if (moveInput.x < 0)
        {
            Vector2 originTop = pos + new Vector2(-forwardOffset, rayOffset);
            Vector2 originBottom = pos + new Vector2(-forwardOffset, -rayOffset);

            bool hitTop = IsMovementBlocked(originTop, Vector2.left);
            bool hitBottom = IsMovementBlocked(originBottom, Vector2.left);

            if (hitTop && !hitBottom) { currentVelocity.y = -snapSpeed; currentVelocity.x = 0; }
            else if (hitBottom && !hitTop) { currentVelocity.y = snapSpeed; currentVelocity.x = 0; }
        }

        rb.linearVelocity = currentVelocity;
    }

    // El raycast también debe respetar la excepción temporal entre
    // este jugador y una bomba colocada bajo sus pies.
    private bool IsMovementBlocked(Vector2 origin, Vector2 direction)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, checkDistance, solidLayers);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            // Evitar hits propios aunque el personaje tenga varios colliders.
            if (hit.collider.GetComponentInParent<PlayerController>() == this) continue;

            // Un Raycast no siempre sigue la exclusion IgnoreCollision entre
            // colliders. Consultamos directamente el permiso por jugador.
            Bomb bombHit = hit.collider.GetComponentInParent<Bomb>();
            if (bombHit != null && bombHit.AllowsTemporaryPassage(this)) continue;

            if (bodyCollider != null && bodyCollider.enabled &&
                Physics2D.GetIgnoreCollision(bodyCollider, hit.collider))
                continue;
            return true;
        }
        return false;
    }

    public void Die()
    {
        if (!isDead)
        {
            StartCoroutine(DeathSequence());
        }
    }

    public void ActualizarHUD()
    {
        foreach (HudStats stat in hudStats)
        {
            if (stat != null)
            {
                stat.ActualizarDatos(0, maxBombs, speed - 3, explosionRadius);
            }
        }
    }

    public void SelectAnim(RuntimeAnimatorController newController)
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        if (anim != null)
        {
            anim.runtimeAnimatorController = newController;
        }
    }
    private IEnumerator DeathSequence()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetTrigger("Die");

        yield return new WaitForSeconds(1.0f);

        transform.position = startPosition;

        isDead = false;
        if (anim != null)
        {
            anim.ResetTrigger("Die");
        }
    }

    private void PlantBomb()
    {
        float snapX = Mathf.Round(rb.position.x);
        float snapY = Mathf.Round(rb.position.y);
        Vector2 gridPosition = new Vector2(snapX, snapY);

        // Evita dos bombas en una misma casilla, incluso entre jugadores distintos.
        if (ObjectPooler.Instance.HasActiveBombAt(gridPosition)) return;

        // Los jugadores pueden compartir la casilla de una bomba. Bomb se
        // encargara de permitir que TODOS los ocupantes actuales salgan.

        GameObject bombObj = ObjectPooler.Instance.GetPooledBomb();
        if (bombObj == null) return;

        Bomb bombScript = bombObj.GetComponent<Bomb>();
        if (bombScript == null)
        {
            Debug.LogError($"{name}: el objeto de bomba no tiene un componente Bomb.", this);
            return;
        }
        // La bomba se coloca en el centro de la casilla actual.
        bombObj.transform.position = gridPosition;
        bombScript.SetOwner(this);  // Registrar propietario antes de activar desde el pool.
        bombScript.explosionRadius = explosionRadius;
        bombObj.SetActive(true);
        // La bomba ya esta activa: registrar el paso temporal para TODOS
        // los jugadores cuyo cuerpo ocupa su casilla (incluido el dueno).
        // Debe hacerse antes del siguiente paso de fisica.
        bombScript.AllowPlayersInsideToExit();
        OnBombPlanted?.Invoke(bombScript); // Compatibilidad con futura habilidad de Mymy.
        // Activar bomba inmediatamente para que la excepción de colisión se registre al instante.

    }

    private int GetActiveBombCount()
    {
        return ObjectPooler.Instance.GetActiveBombCount(this);
    }

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
            for (int i = 0; i < 3; i++)
            {
                sr.color = new Color(0.2f, 0.8f, 1f, 1f);
                yield return new WaitForSeconds(0.08f);
                sr.color = originalColor;
                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ComprobarMuerte(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        ComprobarMuerte(collision);
    }

    private void ComprobarMuerte(Collider2D collision)
    {
        // Si tocamos la miel "Explosion" morimos
        if (collision.GetComponent<Explosion>() != null)
        {
            Die();
        }
    }

    private void OnDrawGizmos()
    {
        Vector2 pos = transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(pos + new Vector2(-rayOffset, forwardOffset), Vector2.up * checkDistance);
        Gizmos.DrawRay(pos + new Vector2(rayOffset, forwardOffset), Vector2.up * checkDistance);
        Gizmos.DrawRay(pos + new Vector2(-rayOffset, -forwardOffset), Vector2.down * checkDistance);
        Gizmos.DrawRay(pos + new Vector2(rayOffset, -forwardOffset), Vector2.down * checkDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(pos + new Vector2(forwardOffset, rayOffset), Vector2.right * checkDistance);
        Gizmos.DrawRay(pos + new Vector2(forwardOffset, -rayOffset), Vector2.right * checkDistance);
        Gizmos.DrawRay(pos + new Vector2(-forwardOffset, rayOffset), Vector2.left * checkDistance);
        Gizmos.DrawRay(pos + new Vector2(-forwardOffset, -rayOffset), Vector2.left * checkDistance);
    }
}