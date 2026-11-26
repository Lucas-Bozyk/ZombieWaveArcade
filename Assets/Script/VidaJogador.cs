using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VidaJogador : MonoBehaviour
{
    [SerializeField, Min(1)] private int vida = 100;
    [SerializeField] private string cenaGameOver = "GameOver";
    [SerializeField, Min(0f)] private float delayGameOver = 2f;
    [SerializeField] private Behaviour[] controlesDoJogador;

    private static readonly int Morto = Animator.StringToHash("Morto");

    public int VidaAtual => vida;
    public int VidaMaxima { get; private set; }

    private void Awake()
    {
        VidaMaxima = Mathf.Max(1, vida);
    }

    public bool RestaurarVida()
    {
        if (vida <= 0)
            return false;

        vida = VidaMaxima;
        return true;
    }

    public void ReceberDano(int dano)
    {
        if (dano <= 0 || vida <= 0)
            return;

        vida = Mathf.Max(0, vida - dano);

        if (vida == 0)
            StartCoroutine(AbrirGameOver());
    }

    private IEnumerator AbrirGameOver()
    {
        if (TryGetComponent<PlayerMovement>(out var movimento))
            movimento.enabled = false;

        Animator animator = movimento != null ? movimento.animator : null;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (TryGetComponent<TiroJogador>(out var tiro))
            tiro.enabled = false;

        if (controlesDoJogador != null)
        {
            foreach (Behaviour controle in controlesDoJogador)
            {
                if (controle != null && controle != animator)
                    controle.enabled = false;
            }
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            int camadaTiro = animator.GetLayerIndex("Tiro");
            if (camadaTiro >= 0)
                animator.SetLayerWeight(camadaTiro, 0f);

            animator.SetBool(Morto, true);
        }

        yield return new WaitForSecondsRealtime(delayGameOver);
        SceneManager.LoadScene(cenaGameOver);
    }
}
