using UnityEngine;
using System.Collections; // Necesario para las corrutinas

[RequireComponent(typeof(Collider2D))]
public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Velocidad, MasBombas, RangoFuego }

    [Header("Configuración")]
    public PowerUpType tipoDeMejora;

    // Candado de seguridad para evitar autodestrucción instantánea
    private bool isVulnerable = false;

    void Start()
    {
        // Al nacer, inicia el contador de invulnerabilidad
        StartCoroutine(SpawnGracePeriod());
    }

    private IEnumerator SpawnGracePeriod()
    {
        // Es invulnerable por 0.2 segundos (suficiente para ignorar la explosión inicial)
        yield return new WaitForSeconds(0.2f);
        isVulnerable = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. REGLA DEL GDD: Si la explosión (miel) lo toca, se destruye.
        if (collision.GetComponent<Explosion>() != null)
        {
            // Solo se destruye si ya pasó su periodo de gracia
            if (isVulnerable)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 2. Si el jugador lo recoge, aplica la mejora
        PlayerController jugador = collision.GetComponent<PlayerController>();
        if (jugador != null)
        {
            AplicarMejora(jugador);
            Destroy(gameObject);
        }
    }

    private void AplicarMejora(PlayerController jugador)
    {
        switch (tipoDeMejora)
        {
            case PowerUpType.Velocidad:
                jugador.speed += 1f;
                break;
            case PowerUpType.MasBombas:
                jugador.maxBombs++;
                break;
            case PowerUpType.RangoFuego:
                jugador.explosionRadius++;
                break;
        }
        jugador.ActualizarHUD();
        jugador.TriggerPowerUpFeedback();
    }
}