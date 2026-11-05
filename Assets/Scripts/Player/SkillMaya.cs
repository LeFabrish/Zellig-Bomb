using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class SkillMaya : MonoBehaviour
{
    [Header("Bolsillo de Maya")]
    public bool hasPocketedBomb = false;

    [Header("Animator de Maya")]
    [Tooltip("Trigger del Animator para reproducir la animacion de recoger una bomba.")]
    [SerializeField] private string getAnimationTrigger = "Get";

    [Tooltip("Bool del Animator: false = caminar sin bomba; true = caminar con bomba guardada.")]
    [SerializeField] private string hasBombAnimationBool = "HasPocketBomb";

    private Rigidbody2D rb;
    private PlayerController controller;
    private Animator mayaAnimator;
    private InputAction skillAction;
    private int pocketedExplosionRadius = 1;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        mayaAnimator = GetComponent<Animator>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{name}: falta PlayerInput con un Input Actions Asset asignado.", this);
            enabled = false;
            return;
        }

        // Usar las acciones de ESTE jugador: nunca Keyboard.current global.
        skillAction = playerInput.actions.FindAction("Player/Skill");
        if (skillAction == null)
        {
            Debug.LogError($"{name}: no existe la accion 'Player/Skill' en Input Actions.", this);
            enabled = false;
            return;
        }

        UpdateCarryAnimation();
    }

    private void Update()
    {
        if (controller.isDead || controller.isSkillActive || skillAction == null)
            return;

        if (skillAction.WasPressedThisFrame())
            UseSkill();
    }

    private void UseSkill()
    {
        if (ObjectPooler.Instance == null)
            return;

        Vector2 currentGridPos = new Vector2(Mathf.Round(rb.position.x), Mathf.Round(rb.position.y));
        Vector2 targetGridPos = currentGridPos + controller.lastDirection;

        if (!hasPocketedBomb)
        {
            // Recoge primero una bomba de la casilla ocupada, o de la casilla enfrente.
            Bomb bombToPick = TryGetBombAt(currentGridPos);
            if (bombToPick == null)
                bombToPick = TryGetBombAt(targetGridPos);

            if (bombToPick == null)
                return;

            pocketedExplosionRadius = bombToPick.explosionRadius;
            bombToPick.gameObject.SetActive(false); // Vuelve al pool; ya no cuenta como activa.
            hasPocketedBomb = true;

            // Solo se reproduce Get si de verdad se ha recogido una bomba.
            UpdateCarryAnimation();
            PlayGetAnimation();
        }
        else
        {
            // IMPORTANTE: la bomba de bolsillo es adicional a maxBombs.
            // Solo se prohíbe colocar DOS bombas sobre la misma casilla.
            // NO consultar GetActiveBombCount ni GetActiveNormalBombCount aqui.
            if (ObjectPooler.Instance.HasActiveBombAt(currentGridPos))
                return;

            GameObject bombObj = ObjectPooler.Instance.GetPooledBomb();
            if (bombObj == null)
                return;

            Bomb bomb = bombObj.GetComponent<Bomb>();
            if (bomb == null)
            {
                Debug.LogError($"{name}: la bomba del pool no tiene componente Bomb.", this);
                return;
            }

            bombObj.transform.position = currentGridPos;
            bomb.SetOwner(controller);
            bomb.SetPocketBomb(true); // No consume el cupo normal al quedar activa.
            bomb.explosionRadius = pocketedExplosionRadius;
            bombObj.SetActive(true);

            // Mantiene las excepciones temporales de colision de la Etapa 2.
            bomb.AllowPlayersInsideToExit();

            hasPocketedBomb = false;
            UpdateCarryAnimation();
        }
    }

    // ================================================================
    // ANIMACIONES DE MAYA: RECOGER + DOS VARIANTES DE WALK
    // ================================================================
    // PlayerController ya alimenta MoveX, MoveY y Speed de este Animator.
    // Aqui solo definimos el estado del bolsillo:
    //   HasPocketBomb = false -> Walk sin bomba (WalkNormal).
    //   HasPocketBomb = true  -> Walk con bomba (WalkConBomba).
    // El cambio entre Blend Trees se configura en Maya.controller.
    // ================================================================

    private void PlayGetAnimation()
    {
        if (AnimatorHasParameter(getAnimationTrigger, AnimatorControllerParameterType.Trigger))
            mayaAnimator.SetTrigger(getAnimationTrigger);
    }

    private void UpdateCarryAnimation()
    {
        if (AnimatorHasParameter(hasBombAnimationBool, AnimatorControllerParameterType.Bool))
            mayaAnimator.SetBool(hasBombAnimationBool, hasPocketedBomb);
    }

    private bool AnimatorHasParameter(string parameterName, AnimatorControllerParameterType expectedType)
    {
        if (mayaAnimator == null || string.IsNullOrEmpty(parameterName))
            return false;

        // El proyecto actual aun no tiene HasPocketBomb. Mientras se configura
        // el Animator, la habilidad puede funcionar sin generar errores.
        foreach (AnimatorControllerParameter parameter in mayaAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == expectedType)
                return true;
        }
        return false;
    }

    private Bomb TryGetBombAt(Vector2 position)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, 0.2f);
        foreach (Collider2D hit in hitColliders)
        {
            Bomb bomb = hit.GetComponent<Bomb>();
            if (bomb != null && bomb.gameObject.activeInHierarchy)
                return bomb;
        }
        return null;
    }
}
