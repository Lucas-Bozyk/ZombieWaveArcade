using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{
    [SerializeField] private string cenaSurvivor = "Prototype Map";

    public void JogarSurvivor()
    {
        SceneManager.LoadScene(cenaSurvivor);
    }
}