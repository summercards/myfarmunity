// Assets/Scripts/UI/PickupHUD.cs
using UnityEngine;
using TMPro;
using FarmGame.Core.Contracts;

public class PickupHUD : MonoBehaviour, IHintHUD
{
    public TextMeshProUGUI label;

    public void Show(string text)
    {
        if (label) label.text = text;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
