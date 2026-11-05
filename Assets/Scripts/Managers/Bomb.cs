using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private Rigidbody2D bombRigidbody;
    private float defaultFuseTime;

    private bool isExploding = false;

    // Se mantiene antes de activar la bomba desde el pool.
    public PlayerController Owner { get; private set; }

    // Indica que esta bomba fue colocada desde el bolsillo de Maya.
    // Se contabiliza en el pool, pero no consume el limite normal maxBombs.
    public bool IsPocketBomb { get; private set; }

    public void SetPocketBomb(bool fromPocket)
    {
        IsPocketBomb = fromPocket;
    }

    [Header("Salida de la bomba")]
    [Tooltip("Separación mínima entre todos los colliders del jugador y la bomba para restaurar su colisión.")]
    [Min(0f)] public float exitClearance = 0.03f;

    // La excepcion debe ser INDIVIDUAL: Maya puede salir mientras Coco
    // sigue sobre la bomba, y viceversa. Soporta varios colliders por jugador.
    private readonly Dictionary<PlayerController, List<Collider2D>>
        ignoredByPlayer = new Dictionary<PlayerController, List<Collider2D>>();
    private readonly List<PlayerController> finishedPlayers = new List<PlayerController>();

    public void SetOwner(PlayerController owner)
    {
        Owner = owner;
    }

    void Awake()
    {
        bombCollider = GetComponent<Collider2D>();
        bombRigidbody = GetComponent<Rigidbody2D>();
        bombRigidbody.bodyType = RigidbodyType2D.Static;
        bombRigidbody.linearVelocity = Vector2.zero;
        bombRigidbody.angularVelocity = 0f;
        defaultFuseTime = fuseTime;
    }

    void OnEnable()
    {
        isExploding = false;
        ignoredByPlayer.Clear();
        bombRigidbody.bodyType = RigidbodyType2D.Static;
        bombRigidbody.linearVelocity = Vector2.zero;
        bombRigidbody.angularVelocity = 0f;
        bombCollider.isTrigger = false;
        explodeCoroutine = StartCoroutine(ExplodeRoutine());
    }

    // Llamar inmediatamente DESPUES de activar la bomba desde el pool.
    // La bomba sigue siendo solida para quienes se acerquen desde fuera.
    // Solo quienes YA estaban encima al crearla reciben permiso de salida.
    public void AllowPlayersInsideToExit()
    {
        if (!gameObject.activeInHierarchy || bombCollider == null) return;

        // Al modificar transform.position en Update, Physics2D puede no
        // haber actualizado las posiciones de los colliders todavia.
        Physics2D.SyncTransforms();

        // Incluimos a cualquier jugadora sobre la misma bomba, no solo Owner.
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player == null || !player.isActiveAndEnabled) continue;

            Collider2D[] colliders = player.GetComponentsInChildren<Collider2D>();
            List<Collider2D> solids = new List<Collider2D>();
            bool currentlyInside = false;

            foreach (Collider2D col in colliders)
            {
                if (col == null || !col.enabled || col.isTrigger ||
                    !col.gameObject.activeInHierarchy) continue;

                solids.Add(col);
                ColliderDistance2D distance = bombCollider.Distance(col);
                if (distance.isValid && distance.distance <= 0f)
                    currentlyInside = true;
            }

            // El propietario se incluye siempre, incluso si esta en el
            // borde de la casilla al pulsar el boton de colocar bomba.
            if (!currentlyInside && player != Owner) continue;
            if (solids.Count == 0) continue;

            foreach (Collider2D col in solids)
                Physics2D.IgnoreCollision(bombCollider, col, true);

            ignoredByPlayer[player] = solids;
        }
    }

    // Alias de compatibilidad para codigo antiguo. No utilizar para nuevas
    // mecánicas: AllowPlayersInsideToExit describe mejor el comportamiento.
    public void AllowOwnerToExit()
    {
        AllowPlayersInsideToExit();
    }

    // El PlayerController lo consulta para que sus Raycasts de alineamiento
    // no bloqueen a alguien que aun puede salir de esta bomba.
    public bool AllowsTemporaryPassage(PlayerController player)
    {
        return player != null && ignoredByPlayer.ContainsKey(player);
    }

    void FixedUpdate()
    {
        if (ignoredByPlayer.Count == 0) return;
        finishedPlayers.Clear();

        // El permiso se retira DE FORMA INDIVIDUAL cuando todo el cuerpo
        // del jugador se ha separado completamente de la bomba.
        foreach (KeyValuePair<PlayerController, List<Collider2D>> entry in ignoredByPlayer)
        {
            bool fullyOutside = true;
            foreach (Collider2D col in entry.Value)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                ColliderDistance2D distance = bombCollider.Distance(col);
                if (!distance.isValid || distance.distance <= exitClearance)
                {
                    fullyOutside = false;
                    break;
                }
            }

            if (fullyOutside) finishedPlayers.Add(entry.Key);
        }

        foreach (PlayerController player in finishedPlayers)
        {
            List<Collider2D> colliders = ignoredByPlayer[player];
            foreach (Collider2D col in colliders)
            {
                if (col != null && col.enabled && col.gameObject.activeInHierarchy)
                    Physics2D.IgnoreCollision(bombCollider, col, false);
            }
            ignoredByPlayer.Remove(player);
        }
    }

    void OnDisable()
    {
        fuseTime = defaultFuseTime;
        Owner = null;
        IsPocketBomb = false; // Reiniciar al devolver esta instancia al pool.
        ignoredByPlayer.Clear(); // Unity elimina los ignores al desactivar.
        finishedPlayers.Clear();
        explodeCoroutine = null;
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
            float scale = 1f + Mathf.PingPong(timer * 2f, 0.15f); // Animación de pulsación de la bomba
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
        if (collision.GetComponent<Explosion>() != null &&
            Vector2.Distance(transform.position, collision.transform.position) < 0.5f)
        {
            ForceExplode();
        }
    }
}