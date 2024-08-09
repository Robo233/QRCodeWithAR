
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using TMPro;
using Vuforia;

/*
Sample of first 10 pixels:
Pixel QR 0: R=154, G=153, B=124, A=255
Pixel QR 1: R=159, G=158, B=129, A=255
Pixel QR 2: R=159, G=168, B=134, A=255
Pixel QR 3: R=158, G=167, B=133, A=255
Pixel QR 4: R=159, G=176, B=134, A=255
Pixel QR 5: R=160, G=177, B=135, A=255
Pixel QR 6: R=161, G=179, B=132, A=255
Pixel QR 7: R=162, G=180, B=133, A=255
Pixel QR 8: R=166, G=182, B=136, A=255
Pixel QR 9: R=167, G=183, B=137, A=255
Texture Details:
Format: RGBA32
Size: 640x480
MipMap Count: 1
Filter Mode: Bilinear
Wrap Mode: Clamp
Anisotropic Level: 0
NPOT Support: Full
Is Readable: True


*/


/*
Android version:
-For the pixels, only the R is changing, the other two are always 255
Texture Format: R8
Texture Size: 1920x1080
MipMap Count: 1
Filter Mode: Bilinear
Wrap Mode: Clamp
Anisotropic Level: 0
NPOT Support: Full
Is Readable: True




*/

public class QRCodeScanner : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textOut;

    [SerializeField] Texture videoBackgroundTexture;

    [SerializeField] InformationProviderImageTargetGenerator informationProviderImageTargetGenerator;

    bool isVuforiaInitialized;

    float scanInterval = 1.0f;
    float lastScanTime = 0.0f;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
    }

    void OnVuforiaStarted()
    {
        isVuforiaInitialized = true;
    }

    void Update()
    {
        if(isVuforiaInitialized && Time.time - lastScanTime >= scanInterval)
        {
            Scan();
            lastScanTime = Time.time;

        }
    }

    void Scan()
    {
        Debug.Log("Scan");
        // On Android, the videoBackgroundTexture is R8 and the barcode scanner doesn't accept it, so it sneeds to be converted into RGBA32
        Texture2D texture2D = ConvertR8ToRGBA32(DownscaleTexture(transform.GetChild(0).GetComponent<MeshRenderer>().material.mainTexture as Texture2D, 640, 480)); // The DownscaleTexture should optimize it, but I don't know if it does. On Android, the videoBackgroundTexture is 1920x1080
        try
        {
            IBarcodeReader barcodeReader = new BarcodeReader();
            Result result = barcodeReader.Decode(texture2D.GetPixels32(), texture2D.width, texture2D.height);
            if(result != null)
            {
                StartCoroutine(informationProviderImageTargetGenerator.StartInformationProviderImageTargetGenerator(result.Text));
                textOut.text = result.Text;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Scanning failed: " + ex.Message);
            textOut.text = "FAILED IN TRY";
        }

    }

    Texture2D DownscaleTexture(Texture2D original, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        RenderTexture.active = rt;
        Graphics.Blit(original, rt);
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }


    Texture2D ConvertR8ToRGBA32(Texture2D originalTexture)
    {
        Texture2D convertedTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);

        Color32[] originalPixels = originalTexture.GetPixels32();
        Color32[] convertedPixels = new Color32[originalPixels.Length];

        for (int i = 0; i < originalPixels.Length; i++)
        {
            byte grayValue = originalPixels[i].r; // In R8 format, 'r' holds the grayscale value
            convertedPixels[i] = new Color32(grayValue, grayValue, grayValue, 255);
        }

        convertedTexture.SetPixels32(convertedPixels);
        convertedTexture.Apply();

        return convertedTexture;
    }

}
