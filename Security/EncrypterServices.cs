using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CAPA_DATOS;

namespace CAPA_DATOS.Security
{
    public class EncrypterServices
    {      
       
        private static Aes myAes = Aes.Create();
        public static void myAesConfig(){
            myAes.IV = Encoding.ASCII.GetBytes("HR$2pIjHR$2pIj12");
            myAes.Key = Encoding.ASCII.GetBytes("HR$2pIjHR$2pIj13");
        }
        static public String Encrypt(string chain)
        {
            myAesConfig();
            // Encrypt the string to an array of bytes.
            byte[] encrypted = EncryptStringToBytes_Aes(chain, myAes.Key, myAes.IV);
            var encript = Encoding.ASCII.GetString(encrypted);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(encript);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        static public String Decrypt(string chain)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(chain);
            var textConverter = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            myAesConfig();
            byte[] encrypted = Encoding.ASCII.GetBytes(textConverter);
            string decrypt = DecryptStringFromBytes_Aes(encrypted, myAes.Key, myAes.IV);
            return decrypt;
             
        }
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string? plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
    }
}
