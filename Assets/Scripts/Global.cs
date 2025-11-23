using UnityEngine;

public class Global : MonoBehaviour
{
    public static Global Instance;

    //TAGS
    public static string TAG_PLAYER = "Player";

    //Character Variables
    private int gold;
    public int Gold
    {
        get
        {
            return gold;
        }
        set
        {
            gold = value;
        }
    }


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            GameObject.Destroy(gameObject);
    
        DontDestroyOnLoad(gameObject);
    }
}
