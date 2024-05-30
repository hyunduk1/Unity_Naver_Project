using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSoundMng : MonoBehaviour
{
    private static CSoundMng _instance;
    public static CSoundMng Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
        // Start is called before the first frame update
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
