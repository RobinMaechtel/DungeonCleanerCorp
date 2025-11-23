using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseExit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Global.TAG_PLAYER))
            SceneManager.LoadScene("LevelOne");
    }
}
