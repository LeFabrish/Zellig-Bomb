using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Bomb : MonoBehaviour
{
    [Header("Parámetros")]
    public float fuseTime = 2.5f;
    public int explosionRadius = 1;
    public LayerMask obstacleLayer;

    private Coroutine explodeCoroutine;
    private Collider2D bombCollider;
    private float defaultFuseTime;

    // CANDADO: Evita detonaciones infinitas en cadena (StackOverflow)
    private bool isExploding = false;

    void Awake()
    {
        bombCollider = GetComponent<Collider2D>();
        defaultFuseTime = fuseTime;
    }

    void OnEnable()
    {
        isExploding = false; // Reseteamos el candado al salir del Object Pooler
        bombCollider.isTrigger = true;
        explodeCoroutine = StartCoroutine(ExplodeRoutine());
    }

    void OnDisable()
    {
        fuseTime = defaultFuseTime;
    }

    public void ForceExplode()
    {
        // Solo explota si está activa Y no ha comenzado a explotar ya
        if (gameObject.activeInHierarchy && !isExploding)
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
        isExploding = true; // Activamos el candado de seguridad inmediatamente
        SpawnExplosion(transform.position);

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 dir in directions)
        {
            for (int i = 1; i <= explosionRadius; i++)
            {
                Vector2 targetPos = (Vector2)transform.position + (dir * i);

                // ESCANEO TOTAL: Detectamos TODO lo que haya en la casilla
                Collider2D[] hits = Physics2D.OverlapBoxAll(targetPos, new Vector2(0.5f, 0.5f), 0f, obstacleLayer);

                if (hits.Length == 0)
                {
                    // La casilla está libre de obstáculos
                    SpawnExplosion(targetPos);
                }
                else
                {
                    bool needsExplosionVisual = false;

                    // Revisamos todos los objetos golpeados en esta casilla específica
                    foreach (Collider2D hit in hits)
                    {
                        DestructibleBlock block = hit.GetComponent<DestructibleBlock>();
                        Bomb hitBomb = hit.GetComponent<Bomb>();

                        if (block != null)
                        {
                            block.BreakBlock();
                            needsExplosionVisual = true;
                        }
                        else if (hitBomb != null)
                        {
                            hitBomb.ForceExplode(); // Esto ya no causará bucle gracias a !isExploding
                            needsExplosionVisual = true;
                        }
                    }

                    // Solo instanciamos una llama visual por casilla si rompimos madera o detonamos bomba
                    if (needsExplosionVisual)
                    {
                        SpawnExplosion(targetPos);
                    }

                    // Frenamos la expansión de la llama en esta dirección porque chocamos con la capa obstáculo
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Explosion>())
        {
            float distance = Vector2.Distance(transform.position, collision.transform.position);
            if (distance < 0.5f)
            {
                ForceExplode();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController>())
        {
            bombCollider.isTrigger = false;
        }
    }
}