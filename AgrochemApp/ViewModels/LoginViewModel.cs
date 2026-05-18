using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AgrochemApp.Models;
using AgrochemApp.Services;

namespace AgrochemApp.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;

        private string _username = "tech.ivanov";
        private string _password = "123";
        private bool _isLoading;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public event EventHandler<UserModel> LoginSuccess;
        public event PropertyChangedEventHandler PropertyChanged;

        public LoginViewModel()
        {
            _apiService = new ApiService();
            LoginCommand = new RelayCommand(async _ => await Login(), _ => !IsLoading);
        }

        private async Task Login()
        {
            IsLoading = true;
            ErrorMessage = "";

            try
            {
                var result = await _apiService.Login(Username, Password);

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    var user = new UserModel
                    {
                        Id = result.UserId,
                        Username = result.Username,
                        FullName = result.FullName ?? Username,
                        Role = result.Role,
                        Token = result.Token
                    };
                    _apiService.SetToken(result.Token);
                    LoginSuccess?.Invoke(this, user);
                }
                else
                {
                    ErrorMessage = "❌ Неверный логин или пароль";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}