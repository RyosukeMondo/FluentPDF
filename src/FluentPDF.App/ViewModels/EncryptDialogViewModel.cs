using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Services;
using Microsoft.UI.Xaml;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for the PDF encryption dialog.
/// Manages password inputs, permission settings, and encryption strength.
/// </summary>
public partial class EncryptDialogViewModel : ObservableObject
{
    private const int MinPasswordLength = 4;

    [ObservableProperty]
    private string _userPassword = string.Empty;

    [ObservableProperty]
    private string _userPasswordConfirm = string.Empty;

    [ObservableProperty]
    private string _ownerPassword = string.Empty;

    [ObservableProperty]
    private string _ownerPasswordConfirm = string.Empty;

    [ObservableProperty]
    private int _encryptionStrengthIndex = 1; // Default to 256-bit AES

    [ObservableProperty]
    private bool _allowPrint = true;

    [ObservableProperty]
    private bool _allowCopy = true;

    [ObservableProperty]
    private bool _allowModify = true;

    [ObservableProperty]
    private bool _allowAnnotate = true;

    [ObservableProperty]
    private string _userPasswordError = string.Empty;

    [ObservableProperty]
    private string _ownerPasswordError = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the user password has an error.
    /// </summary>
    public bool HasUserPasswordError => !string.IsNullOrEmpty(UserPasswordError);

    /// <summary>
    /// Gets a value indicating whether the owner password has an error.
    /// </summary>
    public bool HasOwnerPasswordError => !string.IsNullOrEmpty(OwnerPasswordError);

    /// <summary>
    /// Gets a value indicating whether the current settings are valid.
    /// </summary>
    public bool IsValid => ValidateSettings();

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptDialogViewModel"/> class.
    /// </summary>
    public EncryptDialogViewModel()
    {
        // Subscribe to property changes for validation
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UserPassword) ||
                e.PropertyName == nameof(UserPasswordConfirm) ||
                e.PropertyName == nameof(OwnerPassword) ||
                e.PropertyName == nameof(OwnerPasswordConfirm))
            {
                ValidatePasswords();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(HasUserPasswordError));
                OnPropertyChanged(nameof(HasOwnerPasswordError));
            }
        };
    }

    /// <summary>
    /// Applies a permission preset to the settings.
    /// </summary>
    [RelayCommand]
    private void ApplyPreset(string preset)
    {
        switch (preset?.ToLowerInvariant())
        {
            case "all":
                AllowPrint = true;
                AllowCopy = true;
                AllowModify = true;
                AllowAnnotate = true;
                break;

            case "readonly":
                AllowPrint = true;
                AllowCopy = false;
                AllowModify = false;
                AllowAnnotate = false;
                break;

            case "none":
                AllowPrint = false;
                AllowCopy = false;
                AllowModify = false;
                AllowAnnotate = false;
                break;
        }
    }

    /// <summary>
    /// Gets the encryption settings from the current input.
    /// </summary>
    /// <returns>The encryption settings.</returns>
    public EncryptionSettings GetEncryptionSettings()
    {
        var permissions = PdfPermissions.None;

        if (AllowPrint)
        {
            permissions |= PdfPermissions.Print;
        }

        if (AllowCopy)
        {
            permissions |= PdfPermissions.Copy;
        }

        if (AllowModify)
        {
            permissions |= PdfPermissions.Modify;
        }

        if (AllowAnnotate)
        {
            permissions |= PdfPermissions.Annotate;
        }

        return new EncryptionSettings
        {
            UserPassword = string.IsNullOrWhiteSpace(UserPassword) ? null : UserPassword,
            OwnerPassword = OwnerPassword,
            Permissions = permissions,
            Strength = EncryptionStrengthIndex == 0 ? EncryptionStrength.Aes128 : EncryptionStrength.Aes256
        };
    }

    private bool ValidateSettings()
    {
        ValidatePasswords();
        return string.IsNullOrEmpty(UserPasswordError) && string.IsNullOrEmpty(OwnerPasswordError);
    }

    private void ValidatePasswords()
    {
        // Validate user password
        if (!string.IsNullOrWhiteSpace(UserPassword))
        {
            if (UserPassword.Length < MinPasswordLength)
            {
                UserPasswordError = $"User password must be at least {MinPasswordLength} characters";
            }
            else if (UserPassword != UserPasswordConfirm)
            {
                UserPasswordError = "Passwords do not match";
            }
            else if (UserPassword == OwnerPassword && !string.IsNullOrWhiteSpace(OwnerPassword))
            {
                UserPasswordError = "User password must be different from owner password";
            }
            else
            {
                UserPasswordError = string.Empty;
            }
        }
        else if (!string.IsNullOrWhiteSpace(UserPasswordConfirm))
        {
            UserPasswordError = "Passwords do not match";
        }
        else
        {
            UserPasswordError = string.Empty;
        }

        // Validate owner password
        if (string.IsNullOrWhiteSpace(OwnerPassword))
        {
            OwnerPasswordError = "Owner password is required";
        }
        else if (OwnerPassword.Length < MinPasswordLength)
        {
            OwnerPasswordError = $"Owner password must be at least {MinPasswordLength} characters";
        }
        else if (OwnerPassword != OwnerPasswordConfirm)
        {
            OwnerPasswordError = "Passwords do not match";
        }
        else if (OwnerPassword == UserPassword && !string.IsNullOrWhiteSpace(UserPassword))
        {
            OwnerPasswordError = "Owner password must be different from user password";
        }
        else
        {
            OwnerPasswordError = string.Empty;
        }
    }
}
