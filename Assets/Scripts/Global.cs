using UnityEngine;
using UnityEngine.Events;

public class Global : MonoBehaviour
{
    public static event UnityAction CurrencyChanged;

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
            CurrencyChanged.Invoke();
        }
    }


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            GameObject.Destroy(gameObject);
    
        DontDestroyOnLoad(gameObject);

        //Default Values
        gold = 0;
    }
}
