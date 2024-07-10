using UnityEngine;
using UnityEngine.UI;

public class Loader : MonoBehaviour
{
    [SerializeField] RawImage rawImage;

    Texture2D texture;
    float loadTime;

    void Load()
    {
        if (texture != null)
        {
            Destroy(texture);
        }

        loadTime = Time.realtimeSinceStartup;

        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "heifloader.heic");
        Debug.Log(path);
        texture = HeifLoader.Load(path, true);

        loadTime = Time.realtimeSinceStartup - loadTime;
        Debug.Log("Load time: " + loadTime);

        rawImage.texture = texture;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Load"))
        {
            Load();
        }

        GUI.Label(new Rect(10, 40, 200, 30), $"Load time: {loadTime * 1000f:F2} ms");
    }
}
