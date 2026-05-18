using System;
using System.Windows;
using System.Windows.Controls;
using AgrochemLaboratory.Models;
using AgrochemLaboratory.Services;

namespace AgrochemLaboratory.Views
{
    public partial class LoginView : Window
    {
        private readonly ApiService _apiService;
        private string _captchaText;

        public LoginView()
        {
            InitializeComponent();
            _apiService = new ApiService();
            GenerateCaptcha();
        }

        private void GenerateCaptcha()
        {
            Random rand = new Random();
            _captchaText = rand.Next(1000, 9999).ToString();
            txtCaptchaCode.Text = _captchaText;
            txtCaptcha.Text = "";
        }

        private void BtnRefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrEmpty(txtUsername.Text))
            {
                lblError.Text = "❌ Введите логин";
                return;
            }
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                lblError.Text = "❌ Введите пароль";
                return;
            }
            if (txtCaptcha.Text != _captchaText)
            {
                lblError.Text = "❌ Неправильный код Captcha";
                GenerateCaptcha();
                return;
            }

            btnLogin.IsEnabled = false;
            btnLogin.Content = "⏳ Вход...";
            lblError.Text = "";

            try
            {
                var result = await _apiService.Login(txtUsername.Text, txtPassword.Password);

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    // Проверяем роль - только лаборант может войти
                    if (result.Role != "laboratory")
                    {
                        lblError.Text = "❌ Доступ запрещён. Только для сотрудников лаборатории.";
                        btnLogin.IsEnabled = true;
                        btnLogin.Content = "ВОЙТИ";
                        return;
                    }

                    var user = new UserModel
                    {
                        Id = result.UserId,
                        Username = result.Username,
                        FullName = result.FullName,
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
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"❌ Ошибка: {ex.Message}";
                GenerateCaptcha();
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "ВОЙТИ";
            }
        }
    }
}