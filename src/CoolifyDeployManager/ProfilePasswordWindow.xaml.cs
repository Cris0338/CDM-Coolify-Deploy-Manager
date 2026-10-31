// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Windows;

namespace CoolifyDeployManager;

public partial class ProfilePasswordWindow : Window
{
    private readonly bool creating;
    public string EnteredPassword { get; private set; } = "";

    public ProfilePasswordWindow(bool isCreating, bool isMigrating)
    {
        InitializeComponent();
        creating = isCreating;
        Heading.Text = Loc.T(creating ? "VaultCreateTitle" : "VaultUnlockTitle");
        Title = Heading.Text;
        Description.Text = Loc.T(creating
            ? (isMigrating ? "VaultMigrateIntro" : "VaultCreateIntro")
            : "VaultUnlockIntro");
        PasswordLabel.Text = Loc.T("VaultPassword");
        ConfirmationLabel.Text = Loc.T("VaultConfirmPassword");
        ConfirmationPanel.Visibility = creating ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Content = Loc.T("Cancel");
        SubmitButton.Content = Loc.T(creating ? "VaultCreateButton" : "VaultUnlockButton");
        Loaded += (_, _) => PasswordInput.Focus();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        var passphrase = PasswordInput.Password;
        if (creating && passphrase.Length < 12)
        {
            ShowError(Loc.T("VaultPasswordTooShort"));
            return;
        }
        if (creating && passphrase != ConfirmationInput.Password)
        {
            ShowError(Loc.T("VaultPasswordMismatch"));
            return;
        }
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            ShowError(Loc.T("VaultPasswordRequired"));
            return;
        }
        EnteredPassword = passphrase;
        PasswordInput.Clear();
        ConfirmationInput.Clear();
        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.Visibility = Visibility.Visible;
    }
}
