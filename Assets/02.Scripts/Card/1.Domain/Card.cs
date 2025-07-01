using System;
using UnityEngine;
public enum ECardColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Brown,
}
[Serializable]
public class Card
{
    public int CardNumber;
    private int _cardNumber;
    public ECardColor Color;
    public string CardImageAddress => $"{Color.ToString()}_{CardNumber}";

    // public string CardImageAddress => $"Card_{CardNumber}";

    public Card(int cardNumber, ECardColor color)
    {
        CardNumber = cardNumber;
        Color = color;
    }

    
}
