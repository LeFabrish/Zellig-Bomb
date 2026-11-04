using NUnit.Framework.Constraints;
using System.Diagnostics;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

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
        DontDestroyOnLoad(gameObject);
    }
    public void SetSelectedLevel(int level)
    {
        selectedLevel = level;
    }
    // Obtiene al 
    public void ActivarLayout()
    {
        if (layouts != null && selectedLevel >= 0 && selectedLevel < layouts.Length)
        {
            for (int i = 0; i < layouts.Length; i++)
            {
                layouts[i].SetActive(i == selectedLevel);
            }
        }
    }
    public void ActivarSkill()
    {

    }
}