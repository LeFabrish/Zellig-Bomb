using UnityEngine;

/// <summary>
/// Etapa 4.5: sesion vinculada a la escena actual. Los layouts son objetos
/// de esa escena: no deben sobrevivir a una recarga con DontDestroyOnLoad.
/// La seleccion del reinicio se conserva por datos simples en MenuManager.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    // Se conservan nombres/campos publicos para no romper escenas ni OnClick.
    public int selectedLevel;
    public int selectedCharacter;
    public GameObject[] layouts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // No usar DontDestroyOnLoad: layouts son referencias de la escena.
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetSelectedLevel(int level)
    {
        selectedLevel = level;
    }

    public void ActivarLayout()
    {
        if (layouts == null || selectedLevel < 0 || selectedLevel >= layouts.Length)
        {
            Debug.LogWarning("GameSession: nivel seleccionado fuera del rango de layouts.", this);
            return;
        }

        for (int i = 0; i < layouts.Length; i++)
        {
            if (layouts[i] != null)
                layouts[i].SetActive(i == selectedLevel);
        }
    }

    // Se mantiene por compatibilidad con codigo anterior.
    public void ActivarSkill()
    {
    }
}
