using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AgrochemApp.Models;
using AgrochemApp.Services;

namespace AgrochemApp.Views
{
    public partial class LoginView : Window
    {
        private readonly ApiService _apiService;
        // private string _captchaText; // удалено, используется CaptchaHelper._captchaText

        public LoginView()
        {
            InitializeComponent();
            _apiService = new ApiService();
            GenerateCaptcha();
        }

        private void GenerateCaptcha()
        {
            txtCaptchaCode.Text = new Random().Next(1000, 9999).ToString();
            txtCaptcha.Text = "";
        }

        private void BtnRefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
            txtCaptcha.Text = "";
        }

        private void LnkSwitch_Click(object sender, RoutedEventArgs e)
        {
            // Переключение между входом и регистрацией
            if (TitleText.Text == "Вход в систему")
            {
                TitleText.Text = "Регистрация";
                btnAction.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
                linkText.Text = "Уже есть аккаунт? Войти";
                CaptchaPanel.Visibility = Visibility.Collapsed;
                RegisterPanel.Visibility = Visibility.Visible;
            }
            else
            {
                TitleText.Text = "Вход в систему";
                btnAction.Content = "ВОЙТИ";
                linkText.Text = "Нет аккаунта? Зарегистрироваться";
                CaptchaPanel.Visibility = Visibility.Visible;
                RegisterPanel.Visibility = Visibility.Collapsed;
                GenerateCaptcha();
            }

            lblError.Text = "";
            txtCaptcha.Text = "";
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TitleText.Text == "Вход в систему")
                {
                    // Режим входа
                    if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
                    {
                        lblError.Text = "❌ Введите логин и пароль";
                        return;
                    }

                    if (txtCaptcha.Text != txtCaptchaCode.Text)
                    {
                        lblError.Text = "❌ Неправильный код Captcha";
                        GenerateCaptcha();
                        txtCaptcha.Text = "";
                        return;
                    }

                    btnAction.IsEnabled = false;
                    btnAction.Content = "⏳ Вход...";

                    var result = await _apiService.Login(txtUsername.Text, txtPassword.Password);

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        var user = new UserModel
                        {
                            Id = result.UserId,
                            Username = result.Username,
                            FullName = result.FullName ?? txtUsername.Text,
                            Role = result.Role,
                            Token = result.Token
                        };

                        var mainWindow = new MainWindow(user);
                        mainWindow.Show();
                        Close();
                    }
                    else
                    {
                        lblError.Text = "❌ Неверный логин или пароль";
                        GenerateCaptcha();
                        txtCaptcha.Text = "";
                        btnAction.IsEnabled = true;
                        btnAction.Content = "ВОЙТИ";
                    }
                }
                else
                {
                    // Режим регистрации
                    if (string.IsNullOrEmpty(txtUsername.Text) ||
                        string.IsNullOrEmpty(txtPassword.Password) ||
                        string.IsNullOrEmpty(txtFullName.Text) ||
                        string.IsNullOrEmpty(txtEmail.Text))
                    {
                        lblError.Text = "❌ Заполните все обязательные поля";
                        return;
                    }

                    if (txtPassword.Password.Length < 3)
                    {
                        lblError.Text = "❌ Пароль должен быть не менее 3 символов";
                        return;
                    }

                    btnAction.IsEnabled = false;
                    btnAction.Content = "⏳ Регистрация...";

                    var role = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();

                    var registerData = new RegisterRequest
                    {
                        Username = txtUsername.Text,
                        FullName = txtFullName.Text,
                        Email = txtEmail.Text,
                        Password = txtPassword.Password,
                        Role = role,
                        Department = txtDepartment.Text
                    };

                    var result = await _apiService.Register(registerData);

                    if (result != null && result.token != null)
                    {
                        MessageBox.Show("Регистрация успешна! Теперь войдите в систему.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Переключаемся на режим входа
                        TitleText.Text = "Вход в систему";
                        btnAction.Content = "ВОЙТИ";
                        linkText.Text = "Нет аккаунта? Зарегистрироваться";
                        CaptchaPanel.Visibility = Visibility.Visible;
                        RegisterPanel.Visibility = Visibility.Collapsed;
                        txtPassword.Password = "";
                        GenerateCaptcha();
                    }
                    else
                    {
                        lblError.Text = result?.message ?? "❌ Ошибка регистрации";
                    }

                    btnAction.IsEnabled = true;
                    btnAction.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"❌ Ошибка: {ex.Message}";
                btnAction.IsEnabled = true;
            }
        }
    }
}