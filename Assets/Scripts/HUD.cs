using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    //For Gold/ Currency
    [SerializeField] TextMeshProUGUI currencyText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCurrencyChanged()
    {
        currencyText.text = Global.Instance.Gold.ToString();
    }

    private void OnEnable()
    {
        //Subscribe to CurrencyChanged   
        Global.CurrencyChanged += OnCurrencyChanged;
    }

    private void OnDisable()
    {
        //Unsub to CurrencyChanged   
        Global.CurrencyChanged -= OnCurrencyChanged;
    }
}
