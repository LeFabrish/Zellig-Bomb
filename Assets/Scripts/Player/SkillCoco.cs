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
    private Rigidbody2D rb;
    private PlayerController controller;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (canDash && !controller.isSkillActive && !controller.isDead)
        {
            if (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame)
            {
                StartCoroutine(DashRoutine());
            }
        }
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        controller.isSkillActive = true;

        if (controller.anim != null)
        {
            controller.anim.SetTrigger("Dash");
        }

        rb.linearVelocity = controller.lastDirection.normalized * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        controller.isSkillActive = false;

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(cooldown);
        canDash = true;
    }
}