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

    private bool isExploding = false;

    void Awake()
    {
        bombCollider = GetComponent<Collider2D>();
        defaultFuseTime = fuseTime;
    }

    void OnEnable()
    {
        isExploding = false;
        bombCollider.isTrigger = true;
        explodeCoroutine = StartCoroutine(ExplodeRoutine());
    }

    void OnDisable()
    {
        fuseTime = defaultFuseTime;
    }

    public void ForceExplode()
    {
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
        isExploding = true;
        SpawnExplosion(transform.position);

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 dir in directions)
        {
            for (int i = 1; i <= explosionRadius; i++)
            {
                Vector2 targetPos = (Vector2)transform.position + (dir * i);

                Collider2D[] hits = Physics2D.OverlapBoxAll(targetPos, new Vector2(0.5f, 0.5f), 0f, obstacleLayer);

                if (hits.Length == 0)
                {
                    SpawnExplosion(targetPos);
                }
                else
                {
                    bool needsExplosionVisual = false;
                    bool hitSolidWall = false; // Bandera para saber si chocamos con algo que SÍ debe frenar el fuego

                    foreach (Collider2D hit in hits)
                    {
                        DestructibleBlock block = hit.GetComponent<DestructibleBlock>();
                        Bomb hitBomb = hit.GetComponent<Bomb>();
                        PlayerController player = hit.GetComponent<PlayerController>();

                        if (block != null)
                        {
                            block.BreakBlock();
                            needsExplosionVisual = true;
                            hitSolidWall = true; // La madera frena el fuego
                        }
                        else if (hitBomb != null)
                        {
                            hitBomb.ForceExplode();
                            needsExplosionVisual = true;
                            hitSolidWall = true; // Otra bomba frena el fuego actual
                        }
                        else if (player != null)
                        {
                            // Si golpea al jugador, generamos fuego y dejamos que lo atraviese (NO frenamos el fuego)
                            needsExplosionVisual = true;
                            // No asignamos hitSolidWall = true aquí.
                        }
                        else
                        {
                            // Es un muro indestructible
                            hitSolidWall = true;
                        }
                    }

                    if (needsExplosionVisual)
                    {
                        SpawnExplosion(targetPos);
                    }

                    if (hitSolidWall)
                    {
                        break;
                    }
                }
            }
        }

        gameObject.SetActive(false);
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