using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZumbiInimigo : MonoBehaviour
{
    [SerializeField] private Transform alvo;
    [SerializeField, Min(1)] private int vidaMaxima = 100;
    [SerializeField, Min(0.1f)] private float alcanceAtaque = 1.8f;
    [SerializeField, Min(0.1f)] private float intervaloAtaque = 1.2f;
    [SerializeField, Min(1)] private int danoAtaque = 10;
    [SerializeField] private ParticleSystem sangueZumbi;
    [SerializeField] private AudioClip somImpacto;
    [SerializeField] private AudioSource fonteGemido;

    private static readonly int Morto = Animator.StringToHash("Morto");
    private static readonly int MorteAlternativa = Animator.StringToHash("MorteAlternativa");

    private NavMeshAgent agente;
    private Animator animator;
    private int vidaAtual;
    private float proximoAtaque;
    private float proximoGemido;

    public bool EstaVivo => vidaAtual > 0;
    public event System.Action<ZumbiInimigo> Eliminado;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        vidaAtual = vidaMaxima;
        if (fonteGemido != null)
            proximoGemido = Time.time + Random.Range(0.35f, 2.25f);
    }

    private void OnDisable()
    {
        if (fonteGemido != null)
            fonteGemido.Stop();
    }

    public void DefinirAlvo(Transform novoAlvo)
    {
        alvo = novoAlvo;
    }

    private void Update()
    {
        if (!agente.isOnNavMesh)
        {
            AtualizarGemido(float.PositiveInfinity);
            return;
        }

        VidaJogador vidaJogador = alvo != null ? alvo.GetComponentInParent<VidaJogador>() : null;
        if (alvo == null || !alvo.gameObject.activeInHierarchy ||
            vidaJogador is { VidaAtual: 0 })
        {
            AtualizarGemido(float.PositiveInfinity);
            agente.isStopped = true;
            AtualizarAnimacao(0f);
            return;
        }

        float distancia = Vector3.Distance(transform.position, alvo.position);
        AtualizarGemido(distancia);
        bool dentroDoAlcance = distancia <= alcanceAtaque;

        agente.isStopped = dentroDoAlcance;

        if (!dentroDoAlcance)
        {
            agente.SetDestination(alvo.position);
            AtualizarAnimacao(agente.velocity.magnitude);
            return;
        }

        AtualizarAnimacao(0f);
        TentarAtacar(vidaJogador);
    }

    private void AtualizarGemido(float distancia)
    {
        if (fonteGemido == null || fonteGemido.clip == null)
            return;

        if (distancia >= fonteGemido.maxDistance)
        {
            if (fonteGemido.isPlaying)
                fonteGemido.Stop();
            return;
        }

        if (Time.time >= proximoGemido && !fonteGemido.isPlaying)
        {
            fonteGemido.Play();
            proximoGemido = Time.time + fonteGemido.clip.length + Random.Range(3.5f, 6.5f);
        }
    }

    private void TentarAtacar(VidaJogador vidaJogador)
    {
        if (Time.time < proximoAtaque || vidaJogador == null)
            return;

        proximoAtaque = Time.time + intervaloAtaque;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger("Atacar");

        vidaJogador.ReceberDano(danoAtaque);
    }

    private void AtualizarAnimacao(float velocidade)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat("Velocidade", velocidade);
    }

    public void ReceberDano(int dano)
    {
        ReceberDano(dano, transform.position + Vector3.up);
    }

    public void ReceberDano(int dano, Vector3 pontoDeImpacto)
    {
        if (dano <= 0 || !EstaVivo)
            return;

        vidaAtual = Mathf.Max(0, vidaAtual - dano);
        if (sangueZumbi != null)
            Instantiate(sangueZumbi, pontoDeImpacto, Quaternion.identity);

        if (somImpacto != null)
            AudioSource.PlayClipAtPoint(somImpacto, pontoDeImpacto, 0.8f);

        if (!EstaVivo)
        {
            Morrer();
            Eliminado?.Invoke(this);
        }
    }

    private void Morrer()
    {
        agente.enabled = false;
        enabled = false;

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger("Atacar");
            animator.SetBool(MorteAlternativa, Random.Range(0, 2) == 1);
            animator.SetBool(Morto, true);
        }
    }
}
