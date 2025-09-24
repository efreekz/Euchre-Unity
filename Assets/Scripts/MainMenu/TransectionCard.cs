using System;
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

        public void Setup(Transaction txn)
        {
            reasonText.text = txn.type;
            descriptionText.text = txn.description;
            DateTime parsedDate = DateTime.ParseExact(
                txn.created_at,
                "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture
            );

            dateText.text = parsedDate.ToString("dd MMM yyyy, HH:mm"); 
            
            amountText.text = $"{txn.amount}";
            
            if (txn.amount > 0)
            {
                amountText.color = Color.green;
                background.color = creditColor;
            }
            else
            {
                amountText.color = Color.red;
                background.color = debitColor;
            }
        }
    }
}