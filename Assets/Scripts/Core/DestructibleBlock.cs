using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    [Header("Drop de Power-Ups")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f; // 30% de probabilidad
    public GameObject[] powerUpPrefabs; // Lista de posibles Power-Ups a soltar

    public void BreakBlock()
    {
        // 1. Lanzamos los dados matemáticos
        if (Random.value <= dropChance && powerUpPrefabs.Length > 0)
        {
            // 2. Elegimos un Power-Up al azar de la lista
            int randomIndex = Random.Range(0, powerUpPrefabs.Length);

            // 3. Generamos el objeto en la misma posición exacta de la madera
            Instantiate(powerUpPrefabs[randomIndex], transform.position, Quaternion.identity);
        }

        // 4. Destruimos (o apagamos) el bloque de madera
        gameObject.SetActive(false);
    }
}