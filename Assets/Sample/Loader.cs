using UnityEngine;
using UnityEngine.UI;

public class Loader : MonoBehaviour
{
    [SerializeField] RawImage rawImage;

    void Start()
    {
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "heifloader.heic");
        Debug.Log(path);
        rawImage.texture = HeifLoader.Load(path, true);
    }
}
