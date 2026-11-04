using UnityEngine;
using UnityEngine.UI;

public class SelectImage : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto SelectLayout")]
    [SerializeField] private Image imagenPrevista;

    // Funcion cuando el mouse pase por el botn
    public void CambiarImagen(Sprite nuevoSprite)
    {
        if (imagenPrevista != null && nuevoSprite != null)
        {
            imagenPrevista.sprite = nuevoSprite;
        }
    }
}