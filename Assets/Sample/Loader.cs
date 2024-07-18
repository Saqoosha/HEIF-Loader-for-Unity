using UnityEngine;
using UnityEngine.UI;

public class Loader : MonoBehaviour
{
    [SerializeField] bool useAsync = true;
    [SerializeField] bool flipY = true;
    [SerializeField] bool mipChain = true;
    [SerializeField] bool linear = false;
    [SerializeField] bool isNormalMap = false;
    [SerializeField] RawImage rawImage;

    Texture2D texture;
    float loadTime;

    async void Load()
    {
        if (texture != null)
        {
            Destroy(texture);
        }

        loadTime = Time.realtimeSinceStartup;

        var filename = isNormalMap ? "heifloader-normal.heic" : "heifloader.heic";
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, filename);
        Debug.Log(path);
        if (useAsync)
        {
            texture = await HeifLoader.LoadFromFileAsync(path, flipY, mipChain, linear, isNormalMap);
        }
        else
        {
            texture = HeifLoader.LoadFromFile(path, flipY, mipChain, linear, isNormalMap);
        }

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
