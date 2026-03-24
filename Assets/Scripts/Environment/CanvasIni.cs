using UnityEngine;

public class CanvasIni : MonoBehaviour
{
    Canvas canvas;
    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = GameObject.FindGameObjectWithTag("PlayerCam").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
