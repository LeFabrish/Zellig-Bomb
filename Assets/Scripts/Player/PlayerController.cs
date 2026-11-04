using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
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
    private Vector2 moveInput;
    private Vector2 startPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        startPosition = transform.position;
        Physics2D.queriesHitTriggers = false;
        ActualizarHUD();
    }

    void Update()
    {
        if (isSkillActive || isDead) return;

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

            bool hitLeft = Physics2D.Raycast(originLeft, Vector2.up, checkDistance, solidLayers);
            bool hitRight = Physics2D.Raycast(originRight, Vector2.up, checkDistance, solidLayers);

            if (hitLeft && !hitRight) { currentVelocity.x = snapSpeed; currentVelocity.y = 0; }
            else if (hitRight && !hitLeft) { currentVelocity.x = -snapSpeed; currentVelocity.y = 0; }
        }
        else if (moveInput.y < 0)
        {
            Vector2 originLeft = pos + new Vector2(-rayOffset, -forwardOffset);
            Vector2 originRight = pos + new Vector2(rayOffset, -forwardOffset);

            bool hitLeft = Physics2D.Raycast(originLeft, Vector2.down, checkDistance, solidLayers);
            bool hitRight = Physics2D.Raycast(originRight, Vector2.down, checkDistance, solidLayers);

            if (hitLeft && !hitRight) { currentVelocity.x = snapSpeed; currentVelocity.y = 0; }
            else if (hitRight && !hitLeft) { currentVelocity.x = -snapSpeed; currentVelocity.y = 0; }
        }
        else if (moveInput.x > 0)
        {
            Vector2 originTop = pos + new Vector2(forwardOffset, rayOffset);
            Vector2 originBottom = pos + new Vector2(forwardOffset, -rayOffset);

            bool hitTop = Physics2D.Raycast(originTop, Vector2.right, checkDistance, solidLayers);
            bool hitBottom = Physics2D.Raycast(originBottom, Vector2.right, checkDistance, solidLayers);

            if (hitTop && !hitBottom) { currentVelocity.y = -snapSpeed; currentVelocity.x = 0; }
            else if (hitBottom && !hitTop) { currentVelocity.y = snapSpeed; currentVelocity.x = 0; }
        }
        else if (moveInput.x < 0)
        {
            Vector2 originTop = pos + new Vector2(-forwardOffset, rayOffset);
            Vector2 originBottom = pos + new Vector2(-forwardOffset, -rayOffset);

            bool hitTop = Physics2D.Raycast(originTop, Vector2.left, checkDistance, solidLayers);
            bool hitBottom = Physics2D.Raycast(originBottom, Vector2.left, checkDistance, solidLayers);

            if (hitTop && !hitBottom) { currentVelocity.y = -snapSpeed; currentVelocity.x = 0; }
            else if (hitBottom && !hitTop) { currentVelocity.y = snapSpeed; currentVelocity.x = 0; }
        }

        rb.linearVelocity = currentVelocity;
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
        foreach(HudStats stat in hudStats)
        {
            if (stat != null)
            {
                stat.ActualizarDatos(0, maxBombs, speed-3, explosionRadius);
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

        GameObject bombObj = ObjectPooler.Instance.GetPooledBomb();
        if (bombObj != null)
        {
            bombObj.transform.position = gridPosition;
            Bomb bombScript = bombObj.GetComponent<Bomb>();
            bombScript.explosionRadius = this.explosionRadius;
            OnBombPlanted?.Invoke(bombScript);
            bombObj.SetActive(true);
        }
    }

    private int GetActiveBombCount()
    {
        return ObjectPooler.Instance.GetActiveBombCount();
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