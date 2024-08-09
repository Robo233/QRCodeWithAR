using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InformationProviderImageTargetGenerator : MonoBehaviour
{
// Start a coroutine from the scanner
// Check if the url is valid, if it's not, return
// Download the json file based on the url
// Once it's downloaded, create the image target based on an image from the image's URL

//https://kulth.io/ar/model1

[SerializeField] FileLoader fileLoader;

string baseUrlQR = "https://kulth.io/ar/";
string baseUrlServer = "amazon/";

[SerializeField] ImageTargetGenerator imageTargetGenerator;


public IEnumerator StartInformationProviderImageTargetGenerator(string qrCodeResult)
{
    if(qrCodeResult.StartsWith(baseUrlQR))
    {
        string modelName = qrCodeResult.Substring(baseUrlQR.Length);
        yield return StartCoroutine(fileLoader.DownloadTextCoroutine(baseUrlServer + modelName + ".json", Application.persistentDataPath + "/" + modelName + "/data.json", false, (text) =>
        {
            if(!string.IsNullOrEmpty(text))
            {
                StartCoroutine(imageTargetGenerator.DownloadTextureAndGenerateImageTarget(null, baseUrlServer + modelName + ".jpg", Application.persistentDataPath + "/" + modelName + "/image.jpg", 1, (GameObject ImageTarget, Texture2D texture) =>
                {
                    Debug.Log(ImageTarget);
                } ));
            }
            else
            {
                StartInformationProviderImageTargetGenerator(qrCodeResult);
            }

        } ));
    }
    yield return null;
}


}

