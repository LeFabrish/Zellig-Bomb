using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class SkillVera : MonoBehaviour
{
    [Header("Parámetros de Lanzamiento")]
    public int maxThrowDistance = 4; // Cuántas casillas máximo puede volar la bomba
    public float throwDuration = 0.4f; // Tiempo en el aire
    public LayerMask obstacleLayer; // Asignar "Muros" y "Destructibles" en el Inspector

    private Rigidbody2D rb;
    private Vector2 lastDirection = Vector2.down;
    private bool isThrowing = false; // Candado para no lanzar dos veces seguidas

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. Rastrear la dirección en la que mira Vera
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

        // 2. Activar habilidad con L o C
        if (!isThrowing && (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame))
        {
            TryThrowBomb();
        }
    }

    private void TryThrowBomb()
    {
        Vector2 currentGridPos = new Vector2(Mathf.Round(rb.position.x), Mathf.Round(rb.position.y));
        Vector2 targetGridPos = currentGridPos + lastDirection; // Casilla de enfrente

        // Intentar agarrar la bomba de enfrente, o la de los pies
        Bomb bombToThrow = TryGetBombAt(targetGridPos);
        if (bombToThrow == null) bombToThrow = TryGetBombAt(currentGridPos);

        if (bombToThrow != null)
        {
            StartCoroutine(ThrowRoutine(bombToThrow));
        }
    }

    private IEnumerator ThrowRoutine(Bomb bomb)
    {
        isThrowing = true;

        Vector2 bombStartPos = new Vector2(Mathf.Round(bomb.transform.position.x), Mathf.Round(bomb.transform.position.y));
        Vector2 landingPos = bombStartPos;

        // 1. Calcular el punto de aterrizaje (el primer espacio libre detrás de los obstáculos)
        for (int i = 1; i <= maxThrowDistance; i++)
        {
            Vector2 checkPos = bombStartPos + (lastDirection * i);

            Collider2D obstacle = Physics2D.OverlapCircle(checkPos, 0.2f, obstacleLayer);
            Bomb bombInCell = TryGetBombAt(checkPos);

            // Si no hay muro, madera, ni otra bomba, es un aterrizaje válido
            if (obstacle == null && bombInCell == null)
            {
                landingPos = checkPos;
                break; // Rompemos el bucle para aterrizar justo después del obstáculo
            }
        }

        // Si no se encontró un lugar válido (ej. está atrapado entre los límites del mapa), se cancela
        if (landingPos == bombStartPos)
        {
            isThrowing = false;
            yield break;
        }

        // 2. Preparar la bomba para el vuelo (Convertirla en fantasma temporalmente)
        Collider2D bombCol = bomb.GetComponent<Collider2D>();
        bombCol.enabled = false;

        Vector3 startPos = bomb.transform.position;
        Vector3 endPos = landingPos;
        float timer = 0f;

        // 3. Vuelo de la bomba (Hack visual en 2D)
        while (timer < throwDuration)
        {
            timer += Time.deltaTime;
            float percent = timer / throwDuration;

            // Interpolación lineal de la posición
            bomb.transform.position = Vector3.Lerp(startPos, endPos, percent);

            // Simular altura usando el tamaño (Crece y se encoge en forma de arco)
            float scaleHeight = 1f + (Mathf.Sin(percent * Mathf.PI) * 0.4f);
            bomb.transform.localScale = new Vector3(scaleHeight, scaleHeight, 1f);

            yield return null;
        }

        // 4. Impacto y Solidificación
        bomb.transform.position = endPos;
        bomb.transform.localScale = Vector3.one; // Restablecer escala

        bombCol.enabled = true; // Reactivar físicas
        bombCol.isTrigger = false; // ¡Se vuelve sólida instantáneamente para acorralar enemigos!

        isThrowing = false;
    }

    // Función auxiliar para leer bombas en casillas
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