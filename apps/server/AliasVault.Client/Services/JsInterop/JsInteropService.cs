//-----------------------------------------------------------------------
// <copyright file="JsInteropService.cs" company="lanedirt">
// Copyright (c) lanedirt. All rights reserved.
// Licensed under the AGPLv3 license. See LICENSE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

namespace AliasVault.Client.Services.JsInterop;

using System.Security.Cryptography;
using System.Text.Json;
using AliasVault.Client.Services.JsInterop.Models;
using Microsoft.JSInterop;

/// <summary>
/// JavaScript interop service for calling JavaScript functions from C#.
/// </summary>
/// <param name="jsRuntime">IJSRuntime.</param>
public sealed class JsInteropService(IJSRuntime jsRuntime)
{
    private const string _DEFAULT_VERSION = "0.0.0";
    private const string _VAULT_SQL_GENERATOR_FACTORY_FUNCTION = "CreateVaultSqlGenerator";

    private IJSObjectReference? _identityGeneratorModule;
    private IJSObjectReference? _passwordGeneratorModule;
    private IJSObjectReference? _vaultSqlInteropModule;

    /// <summary>
    /// Initialize the identity generator module.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _identityGeneratorModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/dist/shared/identity-generator/index.mjs");
        if (_identityGeneratorModule == null)
        {
            throw new InvalidOperationException("Failed to initialize identity generator module");
        }

        _passwordGeneratorModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/dist/shared/password-generator/index.mjs");
        if (_passwordGeneratorModule == null)
        {
            throw new InvalidOperationException("Failed to initialize password generator module");
        }

        _vaultSqlInteropModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/dist/shared/vault-sql/index.mjs");
        if (_vaultSqlInteropModule == null)
        {
            throw new InvalidOperationException("Failed to initialize vault SQL generator module");
        }
    }

    /// <summary>
    /// Symmetrically encrypts a string using the provided encryption key.
    /// </summary>
    /// <param name="plaintext">Plain text to encrypt.</param>
    /// <param name="encryptionKey">Encryption key to use.</param>
    /// <returns>Encrypted ciphertext.</returns>
    public async Task<string> SymmetricEncrypt(string plaintext, string encryptionKey)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        return await jsRuntime.InvokeAsync<string>("cryptoInterop.encrypt", plaintext, encryptionKey);
    }

    /// <summary>
    /// Symmetrically decrypts a string using the provided encryption key.
    /// </summary>
    /// <param name="ciphertext">Cipher text to decrypt.</param>
    /// <param name="encryptionKey">Encryption key to use.</param>
    /// <returns>Encrypted ciphertext.</returns>
    public async Task<string> SymmetricDecrypt(string ciphertext, string encryptionKey)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }

        return await jsRuntime.InvokeAsync<string>("cryptoInterop.decrypt", ciphertext, encryptionKey);
    }

    /// <summary>
    /// Downloads a file from a stream.
    /// </summary>
    /// <param name="filename">Filename of the download.</param>
    /// <param name="blob">Blob byte array to download.</param>
    /// <returns>Task.</returns>
    public async Task DownloadFileFromStream(string filename, byte[] blob) =>
        await jsRuntime.InvokeVoidAsync("downloadFileFromStream", filename, blob);

    /// <summary>
    /// Focus an element by its ID.
    /// </summary>
    /// <param name="elementId">The element ID to focus.</param>
    /// <returns>Task.</returns>
    public async Task FocusElementById(string elementId) =>
        await jsRuntime.InvokeVoidAsync("focusElement", elementId);

    /// <summary>
    /// Blur (defocus) an element by its ID.
    /// </summary>
    /// <param name="elementId">The element ID to focus.</param>
    /// <returns>Task.</returns>
    public async Task BlurElementById(string elementId) =>
        await jsRuntime.InvokeVoidAsync("blurElement", elementId);

    /// <summary>
    /// Copy a string to the browser's clipboard.
    /// </summary>
    /// <param name="value">Value to copy to clipboard.</param>
    /// <returns>Task.</returns>
    public async Task CopyToClipboard(string value) =>
        await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", value);

    /// <summary>
    /// Copy a string to the clipboard and schedule automatic clearing after specified seconds.
    /// </summary>
    /// <param name="value">Value to copy to clipboard.</param>
    /// <param name="clearAfterSeconds">Number of seconds after which to clear the clipboard.</param>
    /// <returns>True if copy was successful, false otherwise.</returns>
    public async Task<bool> CopyToClipboardWithClear(string value, int clearAfterSeconds) =>
        await jsRuntime.InvokeAsync<bool>("copyToClipboardWithClear", value, clearAfterSeconds);

    /// <summary>
    /// Clear the clipboard safely, handling document focus issues.
    /// </summary>
    /// <returns>True if clipboard was cleared successfully, false otherwise.</returns>
    public async Task<bool> SafeClearClipboard() =>
        await jsRuntime.InvokeAsync<bool>("safeClearClipboard");

    /// <summary>
    /// Clear the clipboard by setting it to an empty string.
    /// </summary>
    /// <returns>Task.</returns>
    public async Task ClearClipboard() =>
        await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", string.Empty);

    /// <summary>
    /// Register a callback for clipboard status changes.
    /// </summary>
    /// <typeparam name="TComponent">Component type.</typeparam>
    /// <param name="objRef">DotNetObjectReference.</param>
    /// <returns>Task.</returns>
    public async Task RegisterClipboardStatusCallback<TComponent>(DotNetObjectReference<TComponent> objRef)
        where TComponent : class =>
        await jsRuntime.InvokeVoidAsync("registerClipboardStatusCallback", objRef);

    /// <summary>
    /// Unregister the clipboard status callback.
    /// </summary>
    /// <returns>Task.</returns>
    public async Task UnregisterClipboardStatusCallback() =>
        await jsRuntime.InvokeVoidAsync("unregisterClipboardStatusCallback");

    /// <summary>
    /// Initializes the top menu.
    /// </summary>
    /// <returns>Task.</returns>
    public async Task InitTopMenu() =>
        await jsRuntime.InvokeVoidAsync("window.initTopMenu");

    /// <summary>
    /// Registers a click outside handler for the top menu.
    /// </summary>
    /// <typeparam name="TComponent">Component type.</typeparam>
    /// <param name="objRef">DotNetObjectReference.</param>
    /// <returns>Task.</returns>
    public async Task TopMenuClickOutsideHandler<TComponent>(DotNetObjectReference<TComponent> objRef)
        where TComponent : class =>
        await jsRuntime.InvokeVoidAsync("window.topMenuClickOutsideHandler", objRef);

    /// <summary>
    /// Generates a new RSA key pair.
    /// </summary>
    /// <returns>Tuple with public and private key.</returns>
    public async Task<(string PublicKey, string PrivateKey)> GenerateRsaKeyPair()
    {
        var result = await jsRuntime.InvokeAsync<JsonElement>("rsaInterop.generateRsaKeyPair");
        return (result.GetProperty("publicKey").GetString()!, result.GetProperty("privateKey").GetString()!);
    }

    /// <summary>
    /// Encrypts a plaintext with a public key.
    /// </summary>
    /// <param name="plaintext">Plain text to encrypt.</param>
    /// <param name="publicKey">Public key to use for encryption.</param>
    /// <returns>Encrypted ciphertext.</returns>
    public async Task<string> EncryptWithPublicKey(string plaintext, string publicKey) =>
        await jsRuntime.InvokeAsync<string>("rsaInterop.encryptWithPublicKey", plaintext, publicKey);

    /// <summary>
    /// Decrypts a ciphertext with a private key.
    /// </summary>
    /// <param name="base64Ciphertext">Ciphertext to decrypt.</param>
    /// <param name="privateKey">Private key to use for decryption.</param>
    /// <returns>Decrypted string.</returns>
    public async Task<byte[]> DecryptWithPrivateKey(string base64Ciphertext, string privateKey)
    {
        try
        {
            // Invoke the JavaScript function and get the result as a byte array
            byte[] result = await jsRuntime.InvokeAsync<byte[]>("rsaInterop.decryptWithPrivateKey", base64Ciphertext, privateKey);
            return result;
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript decryption error: {ex.Message}");
            throw new CryptographicException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Generates a QR code.
    /// </summary>
    /// <param name="elementId">Element ID that contains data-url attribute which to generate QR code for.</param>
    /// <returns>Task.</returns>
    public async Task GenerateQrCode(string elementId)
    {
        try
        {
            // Invoke the JavaScript function and get the result as a byte array
            await jsRuntime.InvokeVoidAsync("generateQrCode", "authenticator-uri");
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the WebAuthn credential derived key used to encrypt or decrypt the persisted vault encryption key.
    /// </summary>
    /// <param name="credentialId">The credential ID to use.</param>
    /// <param name="salt">The salt to use.</param>
    /// <returns>Derived key as base64 string.</returns>
    /// <exception cref="NotSupportedException">Thrown when the authenticator does not support the PRF extension.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the PRF key derivation fails.</exception>
    /// <exception cref="CryptographicException">Thrown when there's an error getting the WebAuthn credential or when decryption fails.</exception>
    public async Task<string> GetWebAuthnCredentialDerivedKey(string credentialId, string salt)
    {
        try
        {
            var result = await jsRuntime.InvokeAsync<WebAuthnGetCredentialResult>("getWebAuthnCredentialAndDeriveKey", credentialId, salt);

            if (result.Error is null)
            {
                return result.DerivedKey ?? throw new CryptographicException("Derived key is null");
            }

            throw result.Error switch
            {
                "PRF_NOT_SUPPORTED" => new NotSupportedException("Authenticator does not support the PRF extension."),
                "PRF_DERIVATION_FAILED" => new InvalidOperationException("Failed to derive key using PRF extension."),
                "WEBAUTHN_CREATE_ERROR" => new CryptographicException($"Failed to create WebAuthn credential: {result.Message}"),
                _ => new CryptographicException($"Unknown error occurred: {result.Error ?? "No error specified"}"),
            };
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error: {ex.Message}");
            throw new CryptographicException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Gets or creates the WebAuthn credential derived key used to encrypt or decrypt the persisted vault encryption key.
    /// </summary>
    /// <param name="username">The username for the credential.</param>
    /// <returns>A tuple containing the credential ID, salt and the derived key.</returns>
    /// <exception cref="CryptographicException">Thrown when decryption fails due to a JavaScript error.</exception>
    /// <exception cref="NotSupportedException">Thrown when the authenticator does not support the PRF extension.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the PRF key derivation fails.</exception>
    public async Task<(string CredentialId, string Salt, string DerivedKey)> CreateWebAuthnCredentialDerivedKey(string username)
    {
        try
        {
            var result = await jsRuntime.InvokeAsync<WebAuthnCreateCredentialResult>("createWebAuthnCredentialAndDeriveKey", "AliasVault | " + username);

            if (result.CredentialId is not null &&
                result.Salt is not null &&
                result.DerivedKey is not null)
            {
                return (result.CredentialId, result.Salt, result.DerivedKey);
            }

            throw result.Error switch
            {
                "PRF_NOT_SUPPORTED" => new NotSupportedException("Authenticator does not support the PRF extension."),
                "PRF_DERIVATION_FAILED" => new InvalidOperationException("Failed to derive key using PRF extension."),
                "WEBAUTHN_CREATE_ERROR" => new CryptographicException($"Failed to create WebAuthn credential: {result.Message}"),
                _ => new CryptographicException($"Unknown error occurred: {result.Error ?? "No error specified"}"),
            };
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error: {ex.Message}");
            throw new CryptographicException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Scrolls to the top of the page.
    /// </summary>
    /// <returns>Task.</returns>
    public async Task ScrollToTop()
    {
        await jsRuntime.InvokeVoidAsync("window.scrollTo", 0, 0);
    }

    /// <summary>
    /// Registers a visibility callback which is invoked when the visibility of component changes in client.
    /// </summary>
    /// <typeparam name="TComponent">Component type.</typeparam>
    /// <param name="objRef">DotNetObjectReference.</param>
    /// <returns>Task.</returns>
    public async Task RegisterVisibilityCallback<TComponent>(DotNetObjectReference<TComponent> objRef)
        where TComponent : class =>
        await jsRuntime.InvokeVoidAsync("window.registerVisibilityCallback", objRef);

    /// <summary>
    /// Unregisters the visibility callback to prevent memory leaks.
    /// </summary>
    /// <typeparam name="TComponent">Component type.</typeparam>
    /// <param name="objRef">DotNetObjectReference.</param>
    /// <returns>Task.</returns>
    public async Task UnregisterVisibilityCallback<TComponent>(DotNetObjectReference<TComponent> objRef)
        where TComponent : class =>
        await jsRuntime.InvokeVoidAsync("window.unregisterVisibilityCallback", objRef);

    /// <summary>
    /// Symmetrically decrypts a byte array using the provided encryption key.
    /// </summary>
    /// <param name="cipherBytes">Cipher bytes to decrypt.</param>
    /// <param name="encryptionKey">Encryption key to use.</param>
    /// <returns>Decrypted bytes.</returns>
    public async Task<byte[]> SymmetricDecryptBytes(byte[] cipherBytes, string encryptionKey)
    {
        if (cipherBytes == null || cipherBytes.Length == 0)
        {
            return [];
        }

        var base64Ciphertext = Convert.ToBase64String(cipherBytes);
        return await jsRuntime.InvokeAsync<byte[]>("cryptoInterop.decryptBytes", base64Ciphertext, encryptionKey);
    }

    /// <summary>
    /// Generates a random identity using the specified language.
    /// </summary>
    /// <param name="language">The language to use for generating the identity (e.g. "en", "nl").</param>
    /// <param name="gender">The gender preference for generating the identity (optional, defaults to random).</param>
    /// <returns>An AliasVaultIdentity containing the generated identity information.</returns>
    public async Task<AliasVaultIdentity> GenerateRandomIdentityAsync(string language, string? gender = null)
    {
        try
        {
            if (_identityGeneratorModule == null)
            {
                await InitializeAsync();
            }

            var generatorInstance = await _identityGeneratorModule!.InvokeAsync<IJSObjectReference>("CreateIdentityGenerator", language);
            var result = string.IsNullOrEmpty(gender) || gender == "random"
                ? await generatorInstance.InvokeAsync<AliasVaultIdentity>("generateRandomIdentity")
                : await generatorInstance.InvokeAsync<AliasVaultIdentity>("generateRandomIdentity", gender);

            return result;
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error generating identity: {ex.Message}");
            throw new InvalidOperationException("Failed to generate random identity", ex);
        }
    }

    /// <summary>
    /// Generates a random username.
    /// </summary>
    /// <param name="identity">The identity to use for generating the username.</param>
    /// <returns>The generated username.</returns>
    public async Task<string> GenerateRandomUsernameAsync(AliasVaultIdentity identity)
    {
        try
        {
            if (_identityGeneratorModule == null)
            {
                await InitializeAsync();
            }

            var generatorInstance = await _identityGeneratorModule!.InvokeAsync<IJSObjectReference>("CreateUsernameEmailGenerator");
            var result = await generatorInstance.InvokeAsync<string>("generateUsername", identity);
            return result;
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error generating username: {ex.Message}");
            throw new InvalidOperationException("Failed to generate random username", ex);
        }
    }

    /// <summary>
    /// Generates a random email prefix.
    /// </summary>
    /// <param name="identity">The identity to use for generating the email prefix.</param>
    /// <returns>The generated email prefix.</returns>
    public async Task<string> GenerateRandomEmailPrefixAsync(AliasVaultIdentity identity)
    {
        try
        {
            if (_identityGeneratorModule == null)
            {
                await InitializeAsync();
            }

            var generatorInstance = await _identityGeneratorModule!.InvokeAsync<IJSObjectReference>("CreateUsernameEmailGenerator");
            var result = await generatorInstance.InvokeAsync<string>("generateEmailPrefix", identity);
            return result;
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error generating email prefix: {ex.Message}");
            throw new InvalidOperationException("Failed to generate random email prefix", ex);
        }
    }

    /// <summary>
    /// Generates a random password using the specified settings.
    /// </summary>
    /// <param name="settings">The password settings to use.</param>
    /// <returns>The generated password.</returns>
    public async Task<string> GenerateRandomPasswordAsync(PasswordSettings settings)
    {
        try
        {
            if (_passwordGeneratorModule == null)
            {
                await InitializeAsync();
            }

            var generatorInstance = await _passwordGeneratorModule!.InvokeAsync<IJSObjectReference>("CreatePasswordGenerator", settings);

            var result = await generatorInstance.InvokeAsync<string>("generateRandomPassword");

            return result;
        }
        catch (JSException ex)
        {
            await Console.Error.WriteLineAsync($"JavaScript error generating password: {ex.Message}");
            throw new InvalidOperationException("Failed to generate random password", ex);
        }
    }

    /// <summary>
    /// Gets SQL commands to create a new vault with the latest schema.
    /// </summary>
    /// <returns>SQL generation result with commands to execute.</returns>
    public async Task<SqlGenerationResult> GetCreateVaultSqlAsync()
    {
        try
        {
            if (_vaultSqlInteropModule == null)
            {
                await InitializeAsync();
            }

            var vaultGenerator = await _vaultSqlInteropModule!.InvokeAsync<IJSObjectReference>(_VAULT_SQL_GENERATOR_FACTORY_FUNCTION);
            var result = await vaultGenerator.InvokeAsync<JsonElement>("getCreateVaultSql");

            return new SqlGenerationResult
            {
                Success = result.GetProperty("success").GetBoolean(),
                SqlCommands = [.. result.GetProperty("sqlCommands").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))],
                Version = result.GetProperty("version").GetString() ?? _DEFAULT_VERSION,
                MigrationNumber = result.GetProperty("migrationNumber").GetInt32(),
                Error = result.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null,
            };
        }
        catch (JSException ex)
        {
            return new SqlGenerationResult
            {
                Success = false,
                SqlCommands = [],
                Version = _DEFAULT_VERSION,
                MigrationNumber = 0,
                Error = $"JavaScript error: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Gets all available vault versions.
    /// </summary>
    /// <returns>List of vault versions.</returns>
    public async Task<List<SqlVaultVersion>> GetAllVaultVersionsAsync()
    {
        if (_vaultSqlInteropModule == null)
        {
            await InitializeAsync();
        }

        var vaultGenerator = await _vaultSqlInteropModule!.InvokeAsync<IJSObjectReference>(_VAULT_SQL_GENERATOR_FACTORY_FUNCTION);
        var result = await vaultGenerator.InvokeAsync<JsonElement>("getAllVersions");
        return result.EnumerateArray().Select(x => new SqlVaultVersion
        {
            Revision = x.GetProperty("revision").GetInt32(),
            Version = x.GetProperty("version").GetString() ?? string.Empty,
            Description = x.GetProperty("description").GetString() ?? string.Empty,
            ReleaseVersion = x.GetProperty("releaseVersion").GetString() ?? string.Empty,
        }).ToList();
    }

    /// <summary>
    /// Gets SQL commands to check current vault version.
    /// </summary>
    /// <returns>Array of SQL commands to execute.</returns>
    public async Task<SqlVaultVersion> GetLatestVaultVersionAsync()
    {
        if (_vaultSqlInteropModule == null)
        {
            await InitializeAsync();
        }

        var vaultGenerator = await _vaultSqlInteropModule!.InvokeAsync<IJSObjectReference>(_VAULT_SQL_GENERATOR_FACTORY_FUNCTION);
        var result = await vaultGenerator.InvokeAsync<JsonElement>("getLatestVersion");
        return new SqlVaultVersion
        {
            Revision = result.GetProperty("revision").GetInt32(),
            Version = result.GetProperty("version").GetString() ?? string.Empty,
            Description = result.GetProperty("description").GetString() ?? string.Empty,
            ReleaseVersion = result.GetProperty("releaseVersion").GetString() ?? string.Empty,
        };
    }

    /// <summary>
    /// Gets SQL commands to upgrade vault from current to target migration.
    /// </summary>
    /// <param name="currentMigrationNumber">Current migration number.</param>
    /// <param name="targetMigrationNumber">Target migration number (optional, defaults to latest).</param>
    /// <returns>SQL generation result with commands to execute.</returns>
    public async Task<SqlGenerationResult> GetUpgradeVaultSqlAsync(int currentMigrationNumber, int? targetMigrationNumber = null)
    {
        try
        {
            if (_vaultSqlInteropModule == null)
            {
                await InitializeAsync();
            }

            var vaultGenerator = await _vaultSqlInteropModule!.InvokeAsync<IJSObjectReference>(_VAULT_SQL_GENERATOR_FACTORY_FUNCTION);
            var result = targetMigrationNumber.HasValue
                ? await vaultGenerator.InvokeAsync<JsonElement>("getUpgradeVaultSql", currentMigrationNumber, targetMigrationNumber.Value)
                : await vaultGenerator.InvokeAsync<JsonElement>("getUpgradeToLatestSql", currentMigrationNumber);

            return new SqlGenerationResult
            {
                Success = result.GetProperty("success").GetBoolean(),
                SqlCommands = [.. result.GetProperty("sqlCommands").EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(x => !string.IsNullOrEmpty(x))],
                Version = result.GetProperty("version").GetString() ?? _DEFAULT_VERSION,
                MigrationNumber = result.GetProperty("migrationNumber").GetInt32(),
                Error = result.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null,
            };
        }
        catch (JSException ex)
        {
            return new SqlGenerationResult
            {
                Success = false,
                SqlCommands = [],
                Version = _DEFAULT_VERSION,
                MigrationNumber = 0,
                Error = $"JavaScript error: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Validates vault structure from table names.
    /// </summary>
    /// <param name="tableNames">List of table names found in database.</param>
    /// <returns>True if vault structure is valid.</returns>
    public async Task<bool> ValidateVaultStructureAsync(string[] tableNames)
    {
        try
        {
            if (_vaultSqlInteropModule == null)
            {
                await InitializeAsync();
            }

            var vaultGenerator = await _vaultSqlInteropModule!.InvokeAsync<IJSObjectReference>(_VAULT_SQL_GENERATOR_FACTORY_FUNCTION);
            return await vaultGenerator.InvokeAsync<bool>("validateVaultStructure", tableNames);
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>
    /// Represents the result of a WebAuthn get credential operation.
    /// </summary>
    private sealed class WebAuthnGetCredentialResult
    {
        /// <summary>
        /// Gets the derived key.
        /// </summary>
        public string? DerivedKey { get; init; }

        /// <summary>
        /// Gets the optional error message.
        /// </summary>
        public string? Error { get; init; }

        /// <summary>
        /// Gets the optional additional error details.
        /// </summary>
        public string? Message { get; init; }
    }

    /// <summary>
    /// Represents the result of a WebAuthn credential operation.
    /// </summary>
    private sealed class WebAuthnCreateCredentialResult
    {
        /// <summary>
        /// Gets the credential ID as a base64 string.
        /// </summary>
        public string? CredentialId { get; init; }

        /// <summary>
        /// Gets the salt as a base64 string.
        /// </summary>
        public string? Salt { get; init; }

        /// <summary>
        /// Gets the derived key as a base64 string.
        /// </summary>
        public string? DerivedKey { get; init; }

        /// <summary>
        /// Gets the optional error message.
        /// </summary>
        public string? Error { get; init; }

        /// <summary>
        /// Gets the optional additional error details.
        /// </summary>
        public string? Message { get; init; }
    }
}
