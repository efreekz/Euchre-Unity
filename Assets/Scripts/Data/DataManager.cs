using System;
using System.Collections.Generic;

namespace Data
{
    #region Data Classes

    #region Error Data

    [Serializable]
    public class ErrorResponse
    {
        public string code;
        public string message;
        public ErrorData data;
    }

    [Serializable]
    public class ErrorData
    {
        public int status;
    }
    
    #endregion
    
    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string token;
        public UserData user;
        public float balance;
        public string promo_code; // Nullable promo code, keep as string
    }

    [Serializable]
    public class UserData
    {
        public int id;
        public string username;
        public string email;
        public bool email_verified;
    }
    
    [Serializable]
    public class Transaction
    {
        public int id;
        public int amount;
        public string type;
        public string description;
        public string created_at;
        public string formatted_amount;
    }

    [Serializable]
    public class Pagination
    {
        public int total;
        public int limit;
        public int offset;
        public bool has_more;
    }

    [Serializable]
    public class Balance
    {
        public int current;
        public string formatted;
    }

    [Serializable]
    public class TransactionResponse
    {
        public bool success;
        public List<Transaction> transactions;
        public Pagination pagination;
        public Balance balance;
        public Filters filters;
    }

    [Serializable]
    public class Filters
    {
        public string type;
    }


    public class GameResult
    {
        public bool IsLocalPlayerWinner;
        public int Reward;
        public bool IsDraw;
    }
    #endregion
}
