using System;
using System.Windows;
using AgrochemOperator.Models;
using AgrochemOperator.Services;

namespace AgrochemOperator.Views
{
    public partial class LoginView : Window
    {
        private readonly ApiService _apiService;

        public LoginView()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Password))
            {
                lblError.Text = "❌ Введите логин и пароль";
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
                    if (result.Role != "operator")
                    {
                        lblError.Text = "❌ Доступ запрещён. Только для операторов.";
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
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "ВОЙТИ";
            }
        }
    }
}