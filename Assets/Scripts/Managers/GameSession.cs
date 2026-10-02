using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    public int selectedLevel;
    public int selectedCharacter;

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
}