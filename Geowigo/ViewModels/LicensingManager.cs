using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;

namespace Geowigo.ViewModels
{
    public sealed class LicensingManager
    {
        #region Constants

        /// <summary>
        /// Product ID of the Custom Support IAP.
        /// </summary>
        public const string CustomSupportProductId = "CustomSupport";

        /// <summary>
        /// Path of the Custom Support license in the isolated storage.
        /// </summary>
        public const string CustomSupportLicenseFilepath = "CustomSupportIAP.dat";

        #endregion

        #region Properties

        #region HasCustomSupportCertificate

        /// <summary>
        /// Gets if a certificate for custom support is installed on this device.
        /// </summary>
        public bool HasCustomSupportCertificate
        {
            get
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return isf.FileExists(CustomSupportLicenseFilepath);
                }
            }
        }

        #endregion

        #endregion

        #region Fields

        private bool _hasActiveCustomSupportLicense;

        #endregion

        #region Custom Support License

        /// <summary>
        /// Determines if the user has a valid custom support license, and ensures it
        /// is installed on the system if so.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateCustomSupportLicense()
        {
            try
            {
                // Is there a valid license for custom support?
                // Stores the result of HasActiveProductLicense() because it is an expansive call.
                bool hadActiveCustomSupportLicense = _hasActiveCustomSupportLicense;
                if (!_hasActiveCustomSupportLicense && !HasActiveProductLicense(CustomSupportProductId))
                {
                    return false;
                }
                _hasActiveCustomSupportLicense = true;

                // Determines if the certificate is installed, builds it if not, or if the license changed.
                if (!HasCustomSupportCertificate || !hadActiveCustomSupportLicense)
                {
                    await BuildCustomSupportCertificate();
                }

                // We're good!
                return true;
            }
            catch (Exception)
            {
                return _hasActiveCustomSupportLicense;
            }
        }

        private async Task BuildCustomSupportCertificate()
        {
            // Gets the receipt for the product.
            string receipt = await CurrentApp.GetProductReceiptAsync(CustomSupportProductId);

            // Because the receipt might contain user information (such as user ID), it is encrypted
            // using AES+RSA in order to store it safely on the user's hard drive.
            EncryptToFile(receipt, CustomSupportLicenseFilepath);
        }

        #endregion

        #region Windows Store
        private bool HasActiveProductLicense(string productId)
        {
            // Gets all product licenses.
            IReadOnlyDictionary<string, ProductLicense> licenses = CurrentApp.LicenseInformation.ProductLicenses;

            // Returns fals if the product is unknown.
            if (!licenses.ContainsKey(productId))
            {
                return false;
            }

            // Returns if the product is active.
            return licenses[productId].IsActive;
        } 
        #endregion

        #region Cryptography

        private void EncryptToFile(string data, string filepath)
        {
            // Gets the bytes from the string.
            byte[] binData = Encoding.UTF8.GetBytes(data);

            // AES encryption of the source file.
            byte[] aesKey, aesIv;
            byte[] aesEncData;
            using (AesManaged aes = new AesManaged())
            {
                // Generates a random AES key and IV.
                aes.GenerateKey();
                aes.GenerateIV();
                aesKey = aes.Key;
                aesIv = aes.IV;

                // Encrypts the data.
                ICryptoTransform ct = aes.CreateEncryptor();
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                    {
                        using (BinaryWriter bw = new BinaryWriter(cs))
                        {
                            bw.Write(binData);
                        }
                    }
                    aesEncData = ms.ToArray();
                }
            }

            // Loads the RSA public key.
            RSAParameters rsaPublicKey = LoadRSAPublicKey();

            // RSA encryption of the AES key+iv.
            byte[] rsaEncAesKey, rsaEncAesIv;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // Imports the key.
                rsa.ImportParameters(rsaPublicKey);

                // Performs encryption.
                rsaEncAesKey = rsa.Encrypt(aesKey, false);
                rsaEncAesIv = rsa.Encrypt(aesIv, false);
            }

            // Writes the encrypted data.
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream fs = isf.OpenFile(filepath, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        // Key length + data.
                        bw.Write(rsaEncAesKey.Length);
                        bw.Write(rsaEncAesKey);

                        // IV length + data.
                        bw.Write(rsaEncAesIv.Length);
                        bw.Write(rsaEncAesIv);

                        // AES encrypted length + data
                        bw.Write(aesEncData.Length);
                        bw.Write(aesEncData);
                    }
                }
            }
        }

        private RSAParameters LoadRSAPublicKey()
        {
            // Loads the modulus and exponent from the embedded file.
            byte[] modulus, exponent;
            using (BinaryReader br = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Geowigo.Resources.licensing_public_key.dat")))
            {
                int expLen = br.ReadInt32();
                exponent = br.ReadBytes(expLen);

                int modLen = br.ReadInt32();
                modulus = br.ReadBytes(modLen);
            }

            // Bakes a RSA public key.
            return new RSAParameters()
            {
                Exponent = exponent,
                Modulus = modulus
            };
        }

        #endregion
    }
}
