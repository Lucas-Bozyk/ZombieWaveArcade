using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(NavMeshAgent))]
[DefaultExecutionOrder(50)]
public class DobermanCompanheiro : MonoBehaviour
{
    [SerializeField] private PlayerMovement jogador;
    [SerializeField] private RuntimeAnimatorController controlador;
    [SerializeField] private Avatar avatar;
    [SerializeField] private AnimationClip[] clipesLocomocao;
    [SerializeField, Min(0.5f)] private float distanciaParaParar = 1.8f;
    [SerializeField, Min(0.5f)] private float distanciaParaSeguir = 2.7f;
    [SerializeField, Min(0.1f)] private float velocidadeCaminhada = 4.5f;
    [SerializeField, Min(0.1f)] private float velocidadeCorrida = 8f;

    private enum Locomocao { Idle, Turn, Walk, Run }

    private static readonly int EstadoLocomocao = Animator.StringToHash("Locomocao");
    private static readonly int Ritmo = Animator.StringToHash("Ritmo");
    private static readonly int[] Estados =
    {
        Animator.StringToHash("Base Layer.Doberman_Idle"),
        Animator.StringToHash("Base Layer.Doberman_Turn"),
        Animator.StringToHash("Base Layer.Doberman_Walk"),
        Animator.StringToHash("Base Layer.Doberman_Run")
    };
    private const float IntervaloTrajeto = 0.15f;
    private const float IntervaloBuscaNavMesh = 0.5f;

    private NavMeshAgent agente;
    private Animator animator;
    private bool suportaLocomocao;
    private bool suportaRitmo;
    private VidaJogador vidaJogador;
    private Locomocao locomocao;
    private bool seguindo;
    private float atualizarTrajetoEm;
    private float buscarNavMeshEm;
    private Vector3 posicaoAnterior;
    private PlayableGraph grafoAlternativo;
    private AnimationMixerPlayable animacoesAlternativas;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        agente.stoppingDistance = distanciaParaParar;
        agente.updatePosition = true;
        agente.updateRotation = false;
        posicaoAnterior = transform.position;

        Transform raizAnimacao = transform;
        foreach (Transform parte in GetComponentsInChildren<Transform>(true))
        {
            if (parte.name == "Doberman_Rig" && parte.parent != null)
            {
                raizAnimacao = parte.parent;
                break;
            }
        }

        if (!raizAnimacao.TryGetComponent<Animator>(out animator))
            animator = raizAnimacao.gameObject.AddComponent<Animator>();

        if (avatar != null)
            animator.avatar = avatar;
        if (controlador != null)
            animator.runtimeAnimatorController = controlador;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter parametro in animator.parameters)
            {
                suportaLocomocao |= parametro.nameHash == EstadoLocomocao && parametro.type == AnimatorControllerParameterType.Int;
                suportaRitmo |= parametro.nameHash == Ritmo && parametro.type == AnimatorControllerParameterType.Float;
            }

            if (!suportaLocomocao || !suportaRitmo)
                Debug.LogWarning("O Animator do Doberman está com uma versão sem Locomocao ou Ritmo.", animator);
        }

        if (jogador != null)
            vidaJogador = jogador.GetComponentInParent<VidaJogador>();
    }

    private void Start()
    {
        foreach (int estado in Estados)
        {
            if (animator.runtimeAnimatorController == null || !animator.HasState(0, estado))
            {
                PrepararAnimacoesAlternativas();
                break;
            }
        }
    }

    private void Update()
    {
        Vector3 deslocamento = transform.position - posicaoAnterior;
        posicaoAnterior = transform.position;
        deslocamento.y = 0f;
        float velocidadeHorizontal = Time.deltaTime > 0f ? deslocamento.magnitude / Time.deltaTime : 0f;

        if (jogador == null || !jogador.isActiveAndEnabled || vidaJogador is { VidaAtual: 0 } ||
            !TentarEntrarNaNavMesh())
        {
            Parar();
            return;
        }

        Vector3 ateJogador = jogador.transform.position - transform.position;
        ateJogador.y = 0f;
        float limite = seguindo ? distanciaParaParar + 0.1f : distanciaParaSeguir;
        if (ateJogador.sqrMagnitude <= limite * limite)
        {
            Parar();
            return;
        }

        seguindo = true;
        bool correr = jogador.EstaCorrendo;
        agente.speed = correr ? velocidadeCorrida : velocidadeCaminhada;
        if (Time.time >= atualizarTrajetoEm)
        {
            atualizarTrajetoEm = Time.time + IntervaloTrajeto;
            if (NavMesh.SamplePosition(jogador.transform.position, out NavMeshHit ponto, 2f, FiltroNavMesh))
                agente.SetDestination(ponto.position);
        }

        Vector3 direcao = agente.steeringTarget - transform.position;
        direcao.y = 0f;
        if (agente.hasPath && direcao.sqrMagnitude > 0.01f)
        {
            Quaternion destino = Quaternion.LookRotation(direcao);
            float angulo = Quaternion.Angle(transform.rotation, destino);
            bool virar = !correr && angulo > (locomocao == Locomocao.Turn ? 15f : 50f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, destino, 300f * Time.deltaTime);
            if (virar)
            {
                agente.isStopped = true;
                Animar(Locomocao.Turn);
                return;
            }
        }

        agente.isStopped = false;
        if (velocidadeHorizontal > 0.2f)
        {
            float ritmo = Mathf.Clamp(velocidadeHorizontal / agente.speed, 0.4f, 1.4f);
            Animar(correr ? Locomocao.Run : Locomocao.Walk, ritmo);
        }
        else
        {
            Animar(Locomocao.Idle);
        }
    }

    private NavMeshQueryFilter FiltroNavMesh => new NavMeshQueryFilter
    {
        agentTypeID = agente.agentTypeID,
        areaMask = agente.areaMask
    };

    private bool TentarEntrarNaNavMesh()
    {
        if (agente.isOnNavMesh)
            return true;
        if (Time.time < buscarNavMeshEm)
            return false;

        buscarNavMeshEm = Time.time + IntervaloBuscaNavMesh;
        NavMeshQueryFilter filtro = FiltroNavMesh;
        Vector3 aoLadoDoJogador = jogador.transform.position + jogador.transform.right * distanciaParaParar;
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit ponto, 2f, filtro) &&
            !NavMesh.SamplePosition(aoLadoDoJogador, out ponto, 2f, filtro))
            return false;

        agente.enabled = false;
        transform.position = ponto.position;
        posicaoAnterior = ponto.position;
        agente.enabled = true;
        return agente.isOnNavMesh;
    }

    private void Parar()
    {
        if (agente.isOnNavMesh)
        {
            if (seguindo)
                agente.ResetPath();
            agente.isStopped = true;
        }

        seguindo = false;
        atualizarTrajetoEm = 0f;

        Animar(Locomocao.Idle);
    }

    private void Animar(Locomocao estado, float ritmo = 1f)
    {
        bool mudou = locomocao != estado;
        locomocao = estado;
        if (grafoAlternativo.IsValid())
        {
            int indice = (int)estado;
            if (mudou)
            {
                for (int i = 0; i < Estados.Length; i++)
                    animacoesAlternativas.SetInputWeight(i, i == indice ? 1f : 0f);
                animacoesAlternativas.GetInput(indice).SetTime(0);
            }
            animacoesAlternativas.GetInput(indice).SetSpeed(ritmo);
            return;
        }

        if (animator.runtimeAnimatorController == null)
            return;

        if (suportaRitmo)
            animator.SetFloat(Ritmo, ritmo);
        if (mudou && suportaLocomocao)
            animator.SetInteger(EstadoLocomocao, (int)estado);

        int hash = Estados[(int)estado];
        if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != hash && animator.HasState(0, hash) &&
            (!animator.IsInTransition(0) || animator.GetNextAnimatorStateInfo(0).fullPathHash != hash))
            animator.CrossFadeInFixedTime(hash, 0.12f, 0);
    }

    private void PrepararAnimacoesAlternativas()
    {
        if (clipesLocomocao == null || clipesLocomocao.Length != Estados.Length ||
            System.Array.Exists(clipesLocomocao, clipe => clipe == null))
        {
            Debug.LogWarning("O Doberman não disponibilizou todos os estados ou clipes de locomoção.", this);
            return;
        }

        animator.runtimeAnimatorController = null;
        grafoAlternativo = PlayableGraph.Create("LocomocaoDoberman");
        grafoAlternativo.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        animacoesAlternativas = AnimationMixerPlayable.Create(grafoAlternativo, Estados.Length);
        for (int i = 0; i < clipesLocomocao.Length; i++)
        {
            var clipe = AnimationClipPlayable.Create(grafoAlternativo, clipesLocomocao[i]);
            grafoAlternativo.Connect(clipe, 0, animacoesAlternativas, i);
            animacoesAlternativas.SetInputWeight(i, i == (int)Locomocao.Idle ? 1f : 0f);
        }

        var saida = AnimationPlayableOutput.Create(grafoAlternativo, "Doberman", animator);
        saida.SetSourcePlayable(animacoesAlternativas);
        grafoAlternativo.Play();
        Debug.LogWarning("O Doberman está usando os clipes diretamente porque o Animator não disponibilizou todos os estados.", this);
    }

    private void OnDestroy()
    {
        if (grafoAlternativo.IsValid())
            grafoAlternativo.Destroy();
    }
}
