using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class SkillMaya : MonoBehaviour
{
    [Header("Bolsillo de Maya")]
    public bool hasPocketedBomb = false;

    private Rigidbody2D rb;
    private PlayerController controller;
    private InputAction skillAction;
    private int pocketedExplosionRadius = 1;

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
        if (controller.isDead || controller.isSkillActive || skillAction == null)
            return;
        if (skillAction.WasPressedThisFrame())
            UseSkill();
    }

    private void UseSkill()
    {
        Vector2 currentGridPos = new Vector2(Mathf.Round(rb.position.x), Mathf.Round(rb.position.y));
        Vector2 targetGridPos = currentGridPos + controller.lastDirection; // La casilla que tiene en frente

        if (!hasPocketedBomb)
        {
            Bomb bombToPick = TryGetBombAt(currentGridPos);
            if (bombToPick == null)
                bombToPick = TryGetBombAt(targetGridPos);

            if (bombToPick != null)
            {
                pocketedExplosionRadius = bombToPick.explosionRadius;
                bombToPick.gameObject.SetActive(false);
                hasPocketedBomb = true;
            }
        }
        else
        {
            if (ObjectPooler.Instance == null ||
                ObjectPooler.Instance.HasActiveBombAt(currentGridPos) ||
                ObjectPooler.Instance.GetActiveBombCount(controller) >= 0)
                return;

            GameObject bombObj = ObjectPooler.Instance.GetPooledBomb();
            if (bombObj == null) return;

            Bomb bomb = bombObj.GetComponent<Bomb>();
            if (bomb == null)
            {
                Debug.LogError($"{name}: el objeto de la bomba no tiene un componente Bomb.", this);
                return;
            }

            bombObj.transform.position = currentGridPos;
            bomb.SetOwner(controller);
            bomb.explosionRadius = pocketedExplosionRadius;
            bombObj.SetActive(true);
            hasPocketedBomb = false;
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