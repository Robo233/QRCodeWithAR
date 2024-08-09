using System.Collections;
using System.Collections.Generic;
using System.Web;
using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using Siccity.GLTFUtility;

/// <summary>
/// Contains all the methods which are used to download files from the web
/// </summary>

public class FileLoader : MonoBehaviour
{
    public Dictionary<string, byte[]> loadedFiles = new Dictionary<string, byte[]>();

    [SerializeField] EncryptionHelper encryptionHelper;

    public void WriteLoadedFiles() // While the files are being downloaded, theey aren't saved on the disk, but in a dictionary. This method writes them on the disk after the downalod is finsihed. This is because, if the user closes the app during download the files cannot be deleted
    {
        foreach(KeyValuePair<string, byte[]> file in loadedFiles)
        {
            string directoryPath = Path.GetDirectoryName(file.Key);

            if(!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(file.Key, file.Value);
        }

        loadedFiles.Clear();
    }

    public IEnumerator DownloadTextureCoroutine(string url, string path, bool shouldFileBeSaved, Action<Texture2D> OnTextureLoaded)
    {
        Texture2D loadedTexture = new Texture2D(1, 1);
        if(File.Exists(path))
        {
            byte[] encryptedImageBytes = File.ReadAllBytes(path);
//            byte[] decryptedImageBytes = encryptionHelper.Decrypt(encryptedImageBytes);
            loadedTexture.LoadImage(encryptedImageBytes);
            OnTextureLoaded?.Invoke(loadedTexture);
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(webRequest.error);
                    loadedTexture = null;
                    OnTextureLoaded?.Invoke(loadedTexture);
                }
                else
                {
                    loadedTexture = DownloadHandlerTexture.GetContent(webRequest);
                    byte[] imageBytes;
                    if(Path.GetExtension(path) == ".jpg")
                    {
                        imageBytes = loadedTexture.EncodeToJPG();
                    }
                    else
                    {
                        imageBytes = loadedTexture.EncodeToPNG();
                    }
                    byte[] encryptedImageBytes = encryptionHelper.Encrypt(imageBytes);
                    if(shouldFileBeSaved)
                    {
                        File.WriteAllBytes(path, encryptedImageBytes);
                    }
                    else if(!loadedFiles.ContainsKey(path))
                    {
                        loadedFiles.Add(path, encryptedImageBytes);
                    }
                        
                    OnTextureLoaded?.Invoke(loadedTexture);
                }
            }
        }
    }

    public IEnumerator DownloadModelCoroutine(string url, string path, string animationType, bool shouldFileBeSaved, Action<GameObject> OnModelLoaded)
    {
        Debug.Log("Will Loading model from " + url);
        GameObject Model;
        if(File.Exists(path))
        {
            Debug.Log("Found model locally");
            byte[] encryptedModelData = File.ReadAllBytes(path);
            byte[] decryptedModelData = encryptionHelper.Decrypt(encryptedModelData);
            Model = LoadModel(path, decryptedModelData, animationType, shouldFileBeSaved);
            OnModelLoaded?.Invoke(Model);
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                yield return webRequest.SendWebRequest();
                if(webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(webRequest.error);
                    Model = null;
                    OnModelLoaded?.Invoke(Model);
                }
                else
                {
                    Debug.Log("Loading model from " + url);
                    Model = LoadModel(path, webRequest.downloadHandler.data, animationType, shouldFileBeSaved);
                    OnModelLoaded?.Invoke(Model);
                }
            }
        }
    }

    AnimationClip[] AnimationClips;

    GameObject LoadModel(string path, byte[] modelData, string animationType, bool shouldFileBeSaved = true)
    {
        GameObject Model;
        var I = new ImportSettings();
        I.useLegacyClips = true;

        if(Path.GetExtension(path) == ".gltf")
        {
            string temporaryPath = Application.persistentDataPath + Path.GetFileName(path);
            File.WriteAllBytes(temporaryPath, modelData);
            Model = Importer.LoadFromFile(temporaryPath, I, out AnimationClips); // If a gltf model is loaded directly from bytes it doesn't work
            File.Delete(temporaryPath);

        }
        else
        {
            Model = Importer.LoadFromBytes(modelData, I, out AnimationClips);
            
        }

        Model.tag = "Loaded";
        Animation Anim = Model.AddComponent<Animation>();

        if(AnimationClips.Length > 0)
        {
            if(animationType == "multiple")
            {
                foreach(AnimationClip Clip in AnimationClips)
                {
                    Clip.legacy = true;
                    Anim.AddClip(Clip, Clip.name);
                }
            }
            else
            {
                AnimationClip Clip = AnimationClips[0];
                Clip.legacy = true;
                Clip.wrapMode = WrapMode.Loop;
                Anim.AddClip(Clip,Clip.name);
                Anim.clip = Clip;
                Anim.Play(Clip.name);
            }
        }

        byte[] encryptedModelData = encryptionHelper.Encrypt(modelData);
        if(shouldFileBeSaved)
        {
            File.WriteAllBytes(path, encryptedModelData);
        }
        else if(!loadedFiles.ContainsKey(path))
        {
            loadedFiles.Add(path, encryptedModelData);
        }

        return Model;
    
    }

    public IEnumerator DownloadAudioCoroutine(string url, string path, bool shouldFileBeSaved, Action<AudioClip> OnAudioLoaded)
    {
        AudioClip audioClip;
        if(File.Exists(path))
        {
            using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                audioClip = DownloadHandlerAudioClip.GetContent(www);
                OnAudioLoaded?.Invoke(audioClip);
            }
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return webRequest.SendWebRequest();
                if(webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(webRequest.error);
                    audioClip = null;
                    OnAudioLoaded?.Invoke(audioClip);
                }
                else
                {
                    audioClip = DownloadHandlerAudioClip.GetContent(webRequest);
                    if(shouldFileBeSaved)
                    {
                        File.WriteAllBytes(path, webRequest.downloadHandler.data);
                    }
                    else if (!loadedFiles.ContainsKey(path))
                    {
                        Debug.Log("Adding audio file to: " + path);
                        loadedFiles.Add(path, webRequest.downloadHandler.data);
                    }
                    else
                    {
                        Debug.Log("Audio file already loaded: " + path);
                    }
                    OnAudioLoaded?.Invoke(audioClip);

                }
            }
        }
    }

    public IEnumerator DownloadTextCoroutine(string url, string path, bool shouldFileBeSaved, Action<string> OnTextLoaded, bool shouldCheckIfFileExists = true)
    {
        string text;
        if(shouldCheckIfFileExists && File.Exists(path))
        {
            byte[] encryptedTextBytes = File.ReadAllBytes(path);
            //byte[] decryptedTextBytes = encryptionHelper.Decrypt(encryptedTextBytes);
            text = Encoding.UTF8.GetString(encryptedTextBytes); 
            OnTextLoaded?.Invoke(text);
            //text = File.ReadAllText(path); reads the file without encrypting it
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                if(webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(webRequest.error);
                    text = null;
                    OnTextLoaded?.Invoke(text);
                }
                else
                {
                    text = webRequest.downloadHandler.text;
                    byte[] textBytes = Encoding.UTF8.GetBytes(text);
                    byte[] encryptedTextBytes = encryptionHelper.Encrypt(textBytes);
                    if(shouldFileBeSaved)
                    {
                        File.WriteAllBytes(path, encryptedTextBytes);
                        //File.WriteAllText(path, text); saves the file without encrypting it
                    }
                    else if(!loadedFiles.ContainsKey(path))
                    {
                        loadedFiles.Add(path, encryptedTextBytes);
                    }
                    OnTextLoaded?.Invoke(text);
                    
                }
            }
        }
    }
}