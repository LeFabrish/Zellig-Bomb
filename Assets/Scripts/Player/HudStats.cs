using UnityEngine;
using TMPro;

public class HudStats : MonoBehaviour
{
    [Header("Referencias de TextMeshPro")]
    [SerializeField] private TMP_Text textRondas;
    [SerializeField] private TMP_Text textBombas;
    [SerializeField] private TMP_Text textVelocidad;
    [SerializeField] private TMP_Text textMiel;

    public void ActualizarDatos(int rondas, int bombas,
            float velocidad, int miel
        )
    {
        textRondas.text = rondas.ToString();
        textBombas.text = bombas.ToString();
        textMiel.text = miel.ToString();
        textVelocidad.text = velocidad.ToString("0");
    }
}
