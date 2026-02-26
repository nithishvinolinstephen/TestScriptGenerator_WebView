using System;
using System.IO;
using System.Text;

namespace TestScriptGeneratorTool.Infrastructure
{
    /// <summary>
    /// Service for storing and retrieving credentials securely.
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Store a credential securely.
        /// </summary>
        /// <param name="key">Credential key (e.g., "OpenAI.ApiKey").</param>
        /// <param name="value">Credential value.</param>
        Task StoreCredentialAsync(string key, string value);

        /// <summary>
        /// Retrieve a stored credential.
        /// </summary>
        /// <param name="key">Credential key.</param>
        /// <returns>Credential value, or null if not found.</returns>
        Task<string?> GetCredentialAsync(string key);

        /// <summary>
        /// Delete a stored credential.
        /// </summary>
        /// <param name="key">Credential key.</param>
        Task DeleteCredentialAsync(string key);

        /// <summary>
        /// Check if a credential exists.
        /// </summary>
        /// <param name="key">Credential key.</param>
        /// <returns>True if credential exists.</returns>
        Task<bool> CredentialExistsAsync(string key);
    }

    /// <summary>
    /// Implementation of ICredentialService using Windows Credential Manager.
    /// </summary>
    public class WindowsCredentialService : ICredentialService
    {
        private const string ApplicationName = "TestScriptGeneratorTool";

        public Task StoreCredentialAsync(string key, string value)
        {
            try
            {
                var credentialKey = $"{ApplicationName}.{key}";
                
                // In production, use Windows Credential Manager via DPAPI or similar
                // For Phase 6, we'll use simple encryption as fallback
                var encrypted = EncryptValue(value);
                
                // Store in local app data
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationName);
                
                Directory.CreateDirectory(appDataPath);
                
                var credFile = Path.Combine(appDataPath, $"{credentialKey}.cred");
                File.WriteAllText(credFile, encrypted);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to store credential '{key}'", ex);
            }
        }

        public Task<string?> GetCredentialAsync(string key)
        {
            try
            {
                var credentialKey = $"{ApplicationName}.{key}";
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationName);
                
                var credFile = Path.Combine(appDataPath, $"{credentialKey}.cred");
                
                if (!File.Exists(credFile))
                    return Task.FromResult<string?>(null);
                
                var encrypted = File.ReadAllText(credFile);
                var decrypted = DecryptValue(encrypted);
                
                return Task.FromResult<string?>(decrypted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve credential '{key}'", ex);
            }
        }

        public Task DeleteCredentialAsync(string key)
        {
            try
            {
                var credentialKey = $"{ApplicationName}.{key}";
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationName);
                
                var credFile = Path.Combine(appDataPath, $"{credentialKey}.cred");
                
                if (File.Exists(credFile))
                    File.Delete(credFile);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete credential '{key}'", ex);
            }
        }

        public Task<bool> CredentialExistsAsync(string key)
        {
            try
            {
                var credentialKey = $"{ApplicationName}.{key}";
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationName);
                
                var credFile = Path.Combine(appDataPath, $"{credentialKey}.cred");
                return Task.FromResult(File.Exists(credFile));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private string EncryptValue(string value)
        {
            // Simple base64 encoding for Phase 6 (not production-grade encryption)
            // In production, use DPAPI: DataProtectionScope.CurrentUser
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        private string DecryptValue(string encrypted)
        {
            try
            {
                var bytes = Convert.FromBase64String(encrypted);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return encrypted;
            }
        }
    }
}
