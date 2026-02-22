using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Provides a minimal device-tied XOR cipher used to obfuscate saved data.
    /// Key is derived deterministically from <see cref="SystemInfo.deviceUniqueIdentifier"/>
    /// (hashed via SHA256) and never stored on disk.
    /// NOTE: This is obfuscation, not cryptographic encryption.
    /// </summary>
    public static class DeviceDataCipher
    {
        private static byte[] GetDeviceKeyBytes()
        {
            try
            {
                var obDir = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    "TurnrootBrain",
                    "structured"
                );
                var obPath = System.IO.Path.Combine(obDir, ".turnrootob");

                if (System.IO.File.Exists(obPath))
                {
                    var base64 = System.IO.File.ReadAllText(obPath);
                    if (!string.IsNullOrEmpty(base64))
                    {
                        try
                        {
                            var idBytes = Convert.FromBase64String(base64);
                            var idStr = Encoding.UTF8.GetString(idBytes);
                            using var sha2 = SHA256.Create();
                            return sha2.ComputeHash(Encoding.UTF8.GetBytes(idStr ?? string.Empty));
                        }
                        catch { }
                    }
                }
            }
            catch { }

            var fallbackId = SystemInfo.deviceUniqueIdentifier ?? string.Empty;
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(fallbackId));
        }

        private static byte[] Xor(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            if (key == null || key.Length == 0)
            {
                return data;
            }

            var outBytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                outBytes[i] = (byte)(data[i] ^ key[i % key.Length]);
            }

            return outBytes;
        }

        public static string EncryptToBase64(string plainUtf8)
        {
            if (string.IsNullOrEmpty(plainUtf8))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(plainUtf8);
            var cipher = Xor(bytes, GetDeviceKeyBytes());
            return Convert.ToBase64String(cipher);
        }

        public static string DecryptFromBase64(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return string.Empty;
            }

            try
            {
                var bytes = Convert.FromBase64String(encoded);
                var plain = Xor(bytes, GetDeviceKeyBytes());
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                // If base64 parse fails, just return empty (caller handles tampering/errors).
                return string.Empty;
            }
        }

        public static string EncryptBytesToBase64(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            var cipher = Xor(bytes, GetDeviceKeyBytes());
            return Convert.ToBase64String(cipher);
        }

        public static byte[] DecryptBytesFromBase64(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return Array.Empty<byte>();
            }

            try
            {
                var bytes = Convert.FromBase64String(encoded);
                return Xor(bytes, GetDeviceKeyBytes());
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}
