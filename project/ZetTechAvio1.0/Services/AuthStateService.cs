using ZetTechAvio1._0.Models;
using System.Text.Json;
using Microsoft.JSInterop;

namespace ZetTechAvio1._0.Services
{
    public interface IAuthStateService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        event Action? OnStateChanged;
        
        Task SetUserAsync(User? user);
        Task ClearUserAsync();
        Task InitializeAsync();
    }

    public class AuthStateService : IAuthStateService
    {
        private User? _currentUser;
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "auth_user";

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public event Action? OnStateChanged;

        public AuthStateService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                if (!string.IsNullOrEmpty(json))
                {
                    _currentUser = JsonSerializer.Deserialize<User>(json);
                    OnStateChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user from localStorage: {ex.Message}");
            }
        }

        public async Task SetUserAsync(User? user)
        {
            _currentUser = user;
            
            if (user != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(user);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving user to localStorage: {ex.Message}");
                }
            }
            
            OnStateChanged?.Invoke();
        }

        public async Task ClearUserAsync()
        {
            _currentUser = null;
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage: {ex.Message}");
            }
            
            OnStateChanged?.Invoke();
        }
    }
}
