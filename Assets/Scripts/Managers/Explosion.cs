using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour
{
    [Header("Duración del fuego")]
    public float duration = 0.5f; // Según tu tabla de parámetros

    void OnEnable()
    {
        StartCoroutine(TurnOffRoutine());
    }

    private IEnumerator TurnOffRoutine()
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false); // Vuelve al almacén
    }
}