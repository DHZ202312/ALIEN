using System.Collections;
using UnityEngine;

public class AnalyzerTrigger : MonoBehaviour
{
    public SuperAnalyzer analyzer;
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RightSample"))
        {
            StartCoroutine(waitthenRetract());
        }
        else
        {
            Debug.Log("Wrong Sample");
        }
    }
    IEnumerator waitthenRetract()
    {
        yield return new WaitForSeconds(1f);
        analyzer.DrawerRetract();
    }
}
