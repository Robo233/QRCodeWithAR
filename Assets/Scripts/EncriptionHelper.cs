using System.Security.Cryptography;
using System.Text;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles the encryption and decryption of files, like models, images texts etc. These methods are used to make the files corrupt, so that they cannot be opened by the user from the file explorer
/// </summary>

public class EncryptionHelper : MonoBehaviour
{
    byte[] key;
    byte[] iv;

    [SerializeField] string keyText;
    [SerializeField] string ivText;

    void Awake()
    {
        key = Encoding.UTF8.GetBytes(keyText); // 32-byte key for encryption
        iv = Encoding.UTF8.GetBytes(ivText); // 16-byte Initialization Vector (IV)
    }

    public byte[] GenerateRandomBytes(int length)
    {
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);
            return randomBytes;
        }
    }

    public byte[] Encrypt(byte[] data)
    {
        using (RijndaelManaged rijndael = new RijndaelManaged())
        {
            rijndael.Key = key;
            rijndael.IV = iv;
            rijndael.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
    }

    public byte[] Decrypt(byte[] data)
    {
        using (RijndaelManaged rijndael = new RijndaelManaged())
        {
            rijndael.Key = key;
            rijndael.IV = iv;
            rijndael.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (MemoryStream decryptedData = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            decryptedData.Write(buffer, 0, bytesRead);
                        }
                        return decryptedData.ToArray();
                    }
                }
            }
        }
    }
}