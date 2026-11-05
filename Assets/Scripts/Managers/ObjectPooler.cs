using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [Header("Pool de Bombas")]
    public GameObject bombPrefab;
    public int bombPoolSize = 13; // Honor a la cancelacion Update #130
    private List<GameObject> pooledBombs;

    [Header("Pool de Explosiones (Miel)")]
    public GameObject explosionPrefab;
    public int explosionPoolSize = 60; // Necesitamos más porque una cruz usa 5 cubos
    private List<GameObject> pooledExplosions;

    void Awake()
    {
        Instance = this;
        pooledBombs = new List<GameObject>();
        pooledExplosions = new List<GameObject>();

        // Crear bombas
        for (int i = 0; i < bombPoolSize; i++)
        {
            GameObject obj = Instantiate(bombPrefab);
            obj.SetActive(false);
            pooledBombs.Add(obj);
        }

        // Crear explosiones
        for (int i = 0; i < explosionPoolSize; i++)
        {
            GameObject obj = Instantiate(explosionPrefab);
            obj.SetActive(false);
            pooledExplosions.Add(obj);
        }
    }

    public GameObject GetPooledBomb()
    {
        for (int i = 0; i < pooledBombs.Count; i++)
        {
            if (!pooledBombs[i].activeInHierarchy) return pooledBombs[i];
        }
        return null;
    }

    public GameObject GetPooledExplosion()
    {
        for (int i = 0; i < pooledExplosions.Count; i++)
        {
            if (!pooledExplosions[i].activeInHierarchy) return pooledExplosions[i];
        }
        return null;
    }
    public int GetActiveBombCount()
    {
        int count = 0;
        for (int i = 0; i < pooledBombs.Count; i++)
        {
            if (pooledBombs[i].activeInHierarchy) count++;
        }
        return count;
    }

    // Cuenta solo las bombas de una jugadora. El pool siguei siendo compartido.
    public int GetActiveBombCount(PlayerController owner)
    {
        if (owner == null) return 0;
        int count = 0;

        foreach (GameObject obj in pooledBombs)
        {
            if(obj != null && obj.activeInHierarchy)
            {
                Bomb bomb = obj.GetComponent<Bomb>();
                if(bomb != null && bomb.Owner == owner)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public bool HasActiveBombAt(Vector2 gridPosition)
    {
        foreach(GameObject obj in pooledBombs)
        {
            if (obj != null && obj.activeInHierarchy &&
                Vector2.Distance((Vector2)obj.transform.position, gridPosition) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }
}