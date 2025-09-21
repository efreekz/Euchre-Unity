using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    public class TransectionCard : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI reasonText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI dateText;
        public Image background; // for tinting

        [Header("Colors")]
        public Color creditColor = new Color(0.8f, 1f, 0.8f); // light green
        public Color debitColor = new Color(1f, 0.8f, 0.8f);  // light red
        public Color neutralBg = Color.white;

        public void Setup(Transections txn)
        {
            // reasonText.text = txn.reason;
            // descriptionText.text = txn.desctiption;
            // dateText.text = txn.CreatedAt.ToDateTime().ToString("dd MMM yyyy, HH:mm");
            //
            // if (txn.transectionType == TransectionType.Credit)
            // {
            //     amountText.text = $"+{txn.amount}";
            //     amountText.color = Color.green;
            //     background.color = creditColor;
            // }
            // else
            // {
            //     amountText.text = $"-{txn.amount}";
            //     amountText.color = Color.red;
            //     background.color = debitColor;
            // }
        }
    }
}