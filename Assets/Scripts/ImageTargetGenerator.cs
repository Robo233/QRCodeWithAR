using System.Collections;
using System;
using System.IO;
using System.Web;
using UnityEngine;
using Vuforia;

/// <summary>
/// Creates an imageTarget, from a texture from a url, and sets its width
/// </summary>

public class ImageTargetGenerator : MonoBehaviour
{
    public GameObject CurrentImageTarget;

    [SerializeField] FileLoader fileLoader;

    public IEnumerator DownloadTextureAndGenerateImageTarget(GameObject Parent, string url, string path, float imageTargetWidth, Action<GameObject, Texture2D> OnImageTargetGenerated, int waitingTimeBeforeDownloadIsStarted = 0)
    {
        Debug.Log("imageURL: " + url + "/image.jpg");
        yield return new WaitForSeconds(waitingTimeBeforeDownloadIsStarted);
        yield return StartCoroutine(fileLoader.DownloadTextureCoroutine(url, path, false, (texture) => 
        {
            if(texture)
            {
                var ImageTarget = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(texture, imageTargetWidth, "ImageTarget" + Path.GetFileName(Path.GetDirectoryName(path)) );
                CurrentImageTarget = ImageTarget.gameObject;
                CurrentImageTarget.transform.SetParent(Parent.transform);
                Debug.Log("ImageTarget is created: " + CurrentImageTarget.gameObject.name);
                OnImageTargetGenerated(CurrentImageTarget, texture);
            }
            else
            {
                StartCoroutine(DownloadTextureAndGenerateImageTarget(Parent, url, path, imageTargetWidth, OnImageTargetGenerated, 1));
            }

        }));
    }
    
}