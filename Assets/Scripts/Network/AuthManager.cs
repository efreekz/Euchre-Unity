using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Data;
using Managers;
using Newtonsoft.Json;
using UnityEngine.Networking;


namespace Network
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        private const string JwtTokenKey = "jwtToken";
        
        [SerializeField] private string baseUrl = $"https://euchrefreakz.com/wp-json/fflogin/v1";
        [SerializeField] private string endPointLogin = "/login";
        [SerializeField] private string endPointSignUp = "/register";
        [SerializeField] private string endPointUser = "/user";
        [SerializeField] private string endPointAddBalance = "/balance/add";
        [SerializeField] private string endPointSubtractBalance = "/balance/subtract";
        [SerializeField] private string endPointGetTransactions = "/balance/history";
        
        private string _jwtToken;
    
        // Store JWT token after login/registration
        private void SetToken(string token)
        {
            _jwtToken = token;
            PlayerPrefs.SetString(JwtTokenKey, token);
            PlayerPrefs.Save();
        }
    
        // Load stored token (call at app startup)
        private void LoadToken()
        {
            if (PlayerPrefs.HasKey(JwtTokenKey))
            {
                _jwtToken = PlayerPrefs.GetString(JwtTokenKey);
            }
        }
    
        // Check if user is logged in
        public bool IsLoggedIn()
        {
            LoadToken();
            return !string.IsNullOrEmpty(_jwtToken);
        }

        private void Awake()
        {
            // Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async UniTask FetchUserData(Action<LoginResponse> onSucess, Action<string> onError)
        {
            if (!IsLoggedIn())
            {
                GameLogger.LogNetwork("User not logged in");
                return;
            }
            
            using var request = UnityWebRequest.Get($"{baseUrl}{endPointUser}");
            request.SetRequestHeader("authorization", $"Bearer {_jwtToken}");
            
            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GameLogger.LogNetwork("User Data: " + request.downloadHandler.text);
                    var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
                    onSucess?.Invoke(response);
                }
                else
                {
                    GameLogger.LogNetwork("Error: " + request.error, GameLogger.LogType.Error);
                    onError?.Invoke(request.error);
                }
            }
            catch (Exception exception)
            {
                var message = exception.Message;
                var code =  exception.StackTrace;

                // Try to extract the JSON error message from the response body
                var jsonStart = message.IndexOf('{');
                if (jsonStart >= 0)
                {
                    var json = message[jsonStart..];

                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(json);
                        message = errorResponse.message;
                        code = errorResponse.code;
                    }
                    catch
                    {
                        // If JSON parsing fails, just keep original message
                    }
                }

                GameLogger.LogNetwork($"Code : {code} \n Message : {message}");
                onError?.Invoke(message);
            }
            
        }
        
        public async UniTask Login(string username, string password, Action<LoginResponse> onSuccess, Action<string> onError)
        {
            var loginData = new { username, password };
        
            var jsonBody = JsonConvert.SerializeObject(loginData);

            using var request = new UnityWebRequest($"{baseUrl}{endPointLogin}", "POST");
            
            var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GameLogger.LogNetwork($"Login Response: {request.downloadHandler.text}");
                    var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
                    SetToken(response.token);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    GameLogger.LogNetwork($"Login Error: {request.error}", GameLogger.LogType.Error);
                    GameLogger.LogNetwork($"Response: {request.downloadHandler.text}", GameLogger.LogType.Error);
                    onError?.Invoke(request.downloadHandler.text);
                }
            }
            catch (Exception exception)
            {
                var message = exception.Message;
                var code =  exception.StackTrace;

                // Try to extract the JSON error message from the response body
                var jsonStart = message.IndexOf('{');
                if (jsonStart >= 0)
                {
                    var json = message[jsonStart..];

                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(json);
                        message = errorResponse.message;
                        code = errorResponse.code;
                    }
                    catch
                    {
                        // If JSON parsing fails, just keep original message
                    }
                }

                GameLogger.LogNetwork($"Code : {code} \n Message : {message}");
                onError?.Invoke(message);
            }
        }

        public void LogOut()
        {
            _jwtToken = string.Empty;
            PlayerPrefs.DeleteKey(JwtTokenKey);
            PlayerPrefs.Save();
        }

        public async UniTask SignUp(string email, string password, string promoCode, bool ageIs18, Action<LoginResponse> onSuccess, Action<string> onError)
        {
            var signUpData = new
            {
                email = email,
                password = password,
                promo_code = promoCode,
                age_confirm = ageIs18 ? "1" : "0"
            };
            
            var jsonBody = JsonConvert.SerializeObject(signUpData);
            GameLogger.LogNetwork($"Login Response: {jsonBody}");

            using var request = new UnityWebRequest($"{baseUrl}{endPointSignUp}", "POST");
            
            var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GameLogger.LogNetwork($"SignUp Response: {request.downloadHandler.text}");
                    var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
                    SetToken(response.token);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    GameLogger.LogNetwork($"Sign Up Error: {request.error}", GameLogger.LogType.Error);
                    GameLogger.LogNetwork($"Response: {request.downloadHandler.text}", GameLogger.LogType.Error);
                    onError?.Invoke(request.downloadHandler.text);
                }
            }
            catch (Exception exception)
            {
                var message = exception.Message;
                var code =  exception.StackTrace;

                var jsonStart = message.IndexOf('{');
                
                if (jsonStart >= 0)
                {
                    var json = message[jsonStart..];

                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(json);
                        message = errorResponse.message;
                        code = errorResponse.code;
                    }
                    catch
                    {
                        // If JSON parsing fails, just keep original message
                    }
                }

                GameLogger.LogNetwork($"Code : {code} \n Message : {message}");
                onError?.Invoke(message);
            }


        }
        
        
        #region Balance
    
        public async UniTask<bool> AddBalance(int amount, string type, string description)
        {
            if (!IsLoggedIn())
            {
                GameLogger.LogNetwork("User not logged in");
                return false;
            }

            var body = new
            {
                user_id = GameManager.UserData.id,
                amount = amount,
                type = type,
                description = description
            };
            
           
            // Convert to JSON
            string jsonData = JsonConvert.SerializeObject(body);

            using var request = new UnityWebRequest($"{baseUrl}{endPointAddBalance}", "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("authorization", $"Bearer {_jwtToken}");
            
            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GameLogger.LogNetwork("Add Balance Response: " + request.downloadHandler.text);
                    return true;
                }
                else
                {
                    GameLogger.LogNetwork("Error: " + request.error, GameLogger.LogType.Error);
                    return false;
                }
            }
            catch (Exception exception)
            {
                var message = exception.Message;
                var code = exception.StackTrace;

                // Try to extract JSON error if available
                var jsonStart = message.IndexOf('{');
                if (jsonStart >= 0)
                {
                    var json = message[jsonStart..];

                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(json);
                        message = errorResponse.message;
                        code = errorResponse.code;
                    }
                    catch
                    {
                        // fallback: leave message as-is
                    }
                }

                GameLogger.LogNetwork($"Code : {code} \n Message : {message}", GameLogger.LogType.Error);
                return false;
            }

        }
        
        
        public async UniTask<bool> SubtractBalance(int amount, string type, string description)
        {
            if (!IsLoggedIn())
            {
                GameLogger.LogNetwork("User not logged in");
                return false;
            }

            var body = new
            {
                user_id = GameManager.UserData.id,
                amount = amount,
                type = type,
                description = description
            };
            
           
            // Convert to JSON
            string jsonData = JsonConvert.SerializeObject(body);

            using var request = new UnityWebRequest($"{baseUrl}{endPointSubtractBalance}", "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("authorization", $"Bearer {_jwtToken}");
            
            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    GameLogger.LogNetwork("Add Balance Response: " + request.downloadHandler.text);
                    return true;
                }
                else
                {
                    GameLogger.LogNetwork("Error: " + request.error, GameLogger.LogType.Error);
                    return false;
                }
            }
            catch (Exception exception)
            {
                var message = exception.Message;
                var code = exception.StackTrace;

                // Try to extract JSON error if available
                var jsonStart = message.IndexOf('{');
                if (jsonStart >= 0)
                {
                    var json = message[jsonStart..];

                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(json);
                        message = errorResponse.message;
                        code = errorResponse.code;
                    }
                    catch
                    {
                        // fallback: leave message as-is
                    }
                }

                GameLogger.LogNetwork($"Code : {code} \n Message : {message}", GameLogger.LogType.Error);
                return false;
            }

        }

        
        public async UniTask<TransactionResponse> GetAllTransactions()
        {
            if (!IsLoggedIn())
            {
                GameLogger.LogNetwork("User not logged in");
                return null;
            }

            using var request = UnityWebRequest.Get($"{baseUrl}{endPointGetTransactions}");

            // Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("authorization", $"Bearer {_jwtToken}");

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    GameLogger.LogNetwork("Transactions Response: " + json);

                    var response = JsonConvert.DeserializeObject<TransactionResponse>(json);
                    return response;
                }
                else
                {
                    GameLogger.LogNetwork("Error: " + request.error, GameLogger.LogType.Error);
                    return null;
                }
            }
            catch (Exception exception)
            {
                GameLogger.LogNetwork($"Exception while fetching transactions: {exception.Message}", GameLogger.LogType.Error);
                return null;
            }
        }


        #endregion
    }
}