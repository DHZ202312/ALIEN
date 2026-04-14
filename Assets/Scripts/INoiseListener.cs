using UnityEngine;

public interface INoiseListener
{
    void OnHeardNoise(UnityEngine.Vector3 noisePosition, float loudness = 1f);
}
