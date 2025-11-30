using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public interface IInteractable
{
    string GetInteractText();
    void Interact(Character user);
}

