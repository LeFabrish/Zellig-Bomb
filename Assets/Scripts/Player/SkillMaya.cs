using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class SkillMaya : MonoBehaviour
{
    [Header("Bolsillo de Maya")]
    public bool hasPocketedBomb = false;

    private Rigidbody2D rb;
    private Vector2 lastDirection = Vector2.down; // Guardamos hacia dónde mira

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. Rastrear siempre hacia dónde mira Maya
        float inputX = 0f; float inputY = 0f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;
        else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputY = 1f;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputY = -1f;

        if (inputX != 0 || inputY != 0)
        {
            if (inputX != 0) inputY = 0;
            lastDirection = new Vector2(inputX, inputY);
        }

        // 2. Activar habilidad
        if (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    private void UseSkill()
    {
        Vector2 currentGridPos = new Vector2(Mathf.Round(rb.position.x), Mathf.Round(rb.position.y));
        Vector2 targetGridPos = currentGridPos + lastDirection; // La casilla que tiene en frente

        if (!hasPocketedBomb)
        {
            // FASE DE RECOGIDA
            // Intenta recoger en la casilla actual; si no hay nada, intenta en la de enfrente
            Bomb bombToPick = TryGetBombAt(currentGridPos);
            if (bombToPick == null)
            {
                bombToPick = TryGetBombAt(targetGridPos);
            }

            if (bombToPick != null)
            {
                bombToPick.gameObject.SetActive(false);
                hasPocketedBomb = true;
            }
        }
        else
        {
            // FASE DE COLOCACIÓN
            // Maya suelta la bomba en la casilla donde está parada
            if (TryGetBombAt(currentGridPos) == null) // Verifica que su casilla esté libre
            {
                GameObject bombObj = ObjectPooler.Instance.GetPooledBomb();
                if (bombObj != null)
                {
                    bombObj.transform.position = currentGridPos;
                    bombObj.GetComponent<Collider2D>().isTrigger = true;
                    bombObj.SetActive(true);
                    hasPocketedBomb = false;
                }
            }
        }
    }

    // Función auxiliar para buscar bombas en una coordenada específica
    private Bomb TryGetBombAt(Vector2 position)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, 0.2f);
        foreach (Collider2D hit in hitColliders)
        {
            Bomb b = hit.GetComponent<Bomb>();
            if (b != null && b.gameObject.activeInHierarchy)
            {
                return b;
            }
        }
        return null;
    }
}