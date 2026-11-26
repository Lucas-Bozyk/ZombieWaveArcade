using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuGameOver : MonoBehaviour
{
    [SerializeField] private string cenaSurvivor = "Prototype Map";
    [SerializeField] private string cenaMenu = "MenuInicial";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void JogarNovamente()
    {
        SceneManager.LoadScene(cenaSurvivor);
    }

    public void VoltarAoMenu()
    {
        SceneManager.LoadScene(cenaMenu);
    }
}
