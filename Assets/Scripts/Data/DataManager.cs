using System;

namespace Data
{
    #region Data Classes

    [Serializable]
    public class UserData
    {
    public string id;
    public string username;
    public string email;
    public int balance;
    //     public Timestamp CreatedAt;
    //     public string invitationCode;
    //     public List<Transections> transections;
    }
    [Serializable]
    public class Transections
    {
    //     public string id;
    //     public int amount;
    //     public Timestamp CreatedAt;
    //     public string desctiption;
    //     public string reason;
    //     public TransectionType transectionType;
    }

    public enum TransectionType
    {
        Debit,
        Credit,
    }

    public class GameResult
    {
        public bool IsLocalPlayerWinner;
        public int Reward;
        public bool IsDraw;
    }
    #endregion
}
