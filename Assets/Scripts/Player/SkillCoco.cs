using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class SkillCoco : MonoBehaviour
{
    [Header("Parámetros del Dash (Coco)")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.2f;
    public float cooldown = 3.0f;

    private bool canDash = true;
    private bool dashRunning;
    private Rigidbody2D rb;
    private PlayerController controller;
    private InputAction skillAction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        PlayerInput playerInput = GetComponent<PlayerInput>();

        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{name}: falta PlayerInput con un input Actions Asset asignado.", this);
            enabled = false;
            return;
        }

        skillAction = playerInput.actions.FindAction("Player/Skill");
        if (skillAction == null)
        {
            Debug.LogError($"{name}: no existe la acción 'Player/Skill' en el Input Actions Asset.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (skillAction != null && canDash && !controller.isSkillActive && 
            !controller.isDead && skillAction.WasPressedThisFrame())
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        dashRunning = true;
        controller.isSkillActive = true;

        if (controller.anim != null)
        {
            controller.anim.SetTrigger("Dash");
        }

        rb.linearVelocity = controller.lastDirection.normalized * dashSpeed;
        yield return new WaitForSeconds(dashDuration);

        controller.isSkillActive = false;
        rb.linearVelocity = Vector2.zero;
        dashRunning = false;

        yield return new WaitForSeconds(cooldown);
        canDash = true;
    }

    void OnDisable()
    {
        if (canDash) return;
        StopAllCoroutines();
        if(dashRunning && controller != null)
            controller.isSkillActive = false;
        if(dashRunning && rb != null)
            rb.linearVelocity = Vector2.zero;
        dashRunning = false;
        canDash = true;
    }
}