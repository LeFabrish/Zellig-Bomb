using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))] // Garantiza la reacción en cadena
public class Bomb : MonoBehaviour
{
    [Header("Parámetros")]
    public float fuseTime = 2.5f;
    public int explosionRadius = 1;
    public LayerMask obstacleLayer;

    private Coroutine explodeCoroutine;
    private Collider2D bombCollider;
    private float defaultFuseTime;

    void Awake()
    {
        bombCollider = GetComponent<Collider2D>();
        defaultFuseTime = fuseTime;
    }

    void OnEnable()
    {
        // 1. Al plantarse, es transparente para que el jugador pueda huir
        bombCollider.isTrigger = true;
        explodeCoroutine = StartCoroutine(ExplodeRoutine());
    }
    void OnDisable()
    {
        fuseTime = defaultFuseTime;
    }

    public void ForceExplode()
    {
        if (gameObject.activeInHierarchy)
        {
            StopCoroutine(explodeCoroutine);
            Detonate();
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        float timer = 0f;
        Vector3 originalScale = Vector3.one;

        while (timer < fuseTime)
        {
            float scale = 1f + Mathf.PingPong(timer * 2f, 0.15f);
            transform.localScale = originalScale * scale;
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        Detonate();
    }
    private void Detonate()
    {
        // 1. Crear explosión en el centro de la bomba
        SpawnExplosion(transform.position);

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 dir in directions)
        {
            // BUCLE: Escaneamos casilla por casilla
            for (int i = 1; i <= explosionRadius; i++)
            {
                Vector2 targetPos = (Vector2)transform.position + (dir * i);

                // MEJORA: Usar OverlapBox (0.5x0.5) es mucho más preciso para detectar Tilemaps
                Collider2D hit = Physics2D.OverlapBox(targetPos, new Vector2(0.5f, 0.5f), 0f, obstacleLayer);

                if (hit == null)
                {
                    // La casilla está libre. Ponemos miel/fuego y seguimos el bucle.
                    SpawnExplosion(targetPos);
                }
                else
                {
                    // Chocamos con algo. Revisamos si tiene el código de madera destructible.
                    DestructibleBlock block = hit.GetComponent<DestructibleBlock>();

                    if (block != null)
                    {
                        // SÍ ES MADERA: La rompemos y la miel ocupa su lugar.
                        block.BreakBlock();
                        SpawnExplosion(targetPos);
                    }

                    // IMPORTANTE: Sea un Muro sólido o una Madera, frenamos el bucle aquí.
                    break;
                }
            }
        }

        gameObject.SetActive(false); // Apagamos la bomba
    }
    private void SpawnExplosion(Vector2 position)
    {
        GameObject exp = ObjectPooler.Instance.GetPooledExplosion();
        if (exp != null)
        {
            exp.transform.position = position;
            exp.SetActive(true);
        }
    }

    // 2. REACCIÓN EN CADENA: Si toca el fuego, explota
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Explosion>())
        {
            ForceExplode();
        }
    }

    // 3. TRAMPA BOMBERMAN: Si el jugador sale de la casilla, la bomba se vuelve un muro
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController>())
        {
            bombCollider.isTrigger = false;
        }
    }
}