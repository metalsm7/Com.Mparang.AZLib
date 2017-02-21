using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Com.Mparang.AZLib {
    public class AZEncrypt {
        public enum ENCRYPT {
            AES256
        }
        public class AES256 {
            public String Enc(String Input, String key) {
                string rtnValue = "";
                if (key.EndsWith("|")) {
                    throw new Exception("key value can't ends with \"|\"!");
                }
                if (key.Length > 32) {
                    throw new Exception("key value length can't over 32!");
                }
                else if (key.Length < 32) {
                    key += new String('|', 32 - key.Length);
                }
                //RijndaelManaged aes = new RijndaelManaged();
                
                byte[] xBuff = null;
                using (Aes aes = Aes.Create()) {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream()) {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
                                swEncrypt.Write(Input);
                            }
                            xBuff = msEncrypt.ToArray();
                        }
                    }
                }
                rtnValue = Convert.ToBase64String(xBuff);
                return rtnValue;
            }

            public String Dec(String Input, String key) {
                string rtnValue = "";
                if (key.EndsWith("|")) {
                    throw new Exception("key value can't ends with \"|\"!");
                }
                if (key.Length > 32) {
                    throw new Exception("key value length can't over 32!");
                }
                else if (key.Length < 32) {
                    key += new String('|', 32 - key.Length);
                }
                using (Aes aes = Aes.Create()) {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    // Create the streams used for decryption.
                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(Input))) {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {
                                rtnValue = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                return rtnValue;
            }
        }
    }
}