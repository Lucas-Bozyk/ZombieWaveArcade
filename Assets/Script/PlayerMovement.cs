using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 4f;
    [Min(0f)] public float velocidadeCorrida = 7f;
    public float velocidadeRotacao = 10f;
    public float multiplicadorLateral = 0.33f;
    public float gravidade = -20f;

    [Header("Animação")]
    public Animator animator;
    [SerializeField] private AnimationClip clipeCorrida;
    [SerializeField] private AudioSource fontePassos;
    [SerializeField] private AudioClip somPasso;

    [Header("Camera")]
    public Transform cameraTransform;

    private CharacterController controller;
    private TiroJogador tiroJogador;
    private Transform alvoCamera;
    private float velocidadeVertical;
    private bool suportaCorrida;
    private bool suportaRitmo;
    private AnimatorOverrideController controleAlternativo;
    private AnimationClip caminhadaOriginal;
    private AnimationClip caminhadaAlternativa;
    private float distanciaAteOPasso = 0.6f;

    public bool EstaCorrendo { get; private set; }

    private static readonly int Correndo = Animator.StringToHash("Correndo");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Ritmo = Animator.StringToHash("Ritmo");
    private static readonly int EstadoIdle = Animator.StringToHash("Base Layer.AC_Adult_Idle_Look_Around");
    private static readonly int EstadoCaminhada = Animator.StringToHash("Base Layer.Adult_Walk");
    private static readonly int EstadoCorrida = Animator.StringToHash("Base Layer.Adult_Run");

#if UNITY_EDITOR
    private bool shiftAnterior;
    private bool corridaAnterior;
    private bool estadoCorridaAnterior;
#endif

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        tiroJogador = GetComponent<TiroJogador>();

        CameraTargetMouse controleCamera = GetComponentInChildren<CameraTargetMouse>();
        if (controleCamera != null)
            alvoCamera = controleCamera.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter parametro in animator.parameters)
            {
                suportaCorrida |= parametro.nameHash == Correndo && parametro.type == AnimatorControllerParameterType.Bool;
                suportaRitmo |= parametro.nameHash == Ritmo && parametro.type == AnimatorControllerParameterType.Float;
            }
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (animator != null && animator.runtimeAnimatorController != null && !animator.HasState(0, EstadoCorrida))
            PrepararCorridaAlternativa();
    }

    private void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;
        bool shiftPressionado = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;

        if (teclado != null)
        {
            shiftPressionado = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;

            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed)
                horizontal -= 1f;

            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed)
                horizontal += 1f;

            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed)
                vertical -= 1f;

            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed)
                vertical += 1f;
        }
#else
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
        shiftPressionado |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

        Vector3 frente = transform.forward;
        Vector3 direita = transform.right;

        if (cameraTransform != null)
        {
            frente = cameraTransform.forward;
            direita = cameraTransform.right;
        }

        frente.y = 0f;
        direita.y = 0f;
        frente.Normalize();
        direita.Normalize();

        Vector3 direcao = direita * horizontal * multiplicadorLateral + frente * vertical;

        if (direcao.sqrMagnitude > 1f)
            direcao.Normalize();

        Vector3 direcaoRotacao = direcao;
        if (tiroJogador != null && tiroJogador.EstaMirando)
        {
            direcaoRotacao = alvoCamera != null ? alvoCamera.forward : frente;
            direcaoRotacao.y = 0f;
        }

        if (direcaoRotacao.sqrMagnitude > 0.01f)
        {
            Quaternion rotacaoAlvoCamera = alvoCamera != null ? alvoCamera.rotation : Quaternion.identity;
            Quaternion rotacaoDesejada = Quaternion.LookRotation(direcaoRotacao);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacaoDesejada,
                velocidadeRotacao * Time.deltaTime
            );

            if (alvoCamera != null)
                alvoCamera.rotation = rotacaoAlvoCamera;
        }

        if (controller.isGrounded && velocidadeVertical < 0f)
            velocidadeVertical = -2f;

        velocidadeVertical += gravidade * Time.deltaTime;

        bool querCorrer = shiftPressionado && direcao.sqrMagnitude > 0.01f;
        float velocidadeAlvo = querCorrer ? velocidadeCorrida : velocidade;
        Vector3 movimento = direcao * velocidadeAlvo;
        movimento.y = velocidadeVertical;

        controller.Move(movimento * Time.deltaTime);

        Vector3 velocidadeReal = controller.velocity;
        velocidadeReal.y = 0f;
        float velocidadeHorizontal = velocidadeReal.magnitude;
        EstaCorrendo = querCorrer && velocidadeHorizontal > 0.1f;
        AtualizarPassos(velocidadeHorizontal);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(Speed, velocidadeHorizontal);
            if (suportaCorrida)
                animator.SetBool(Correndo, EstaCorrendo);
            if (suportaRitmo)
                animator.SetFloat(Ritmo, Mathf.Clamp(velocidadeHorizontal / Mathf.Max(0.1f, velocidadeAlvo), 0.4f, 1.4f));

            AtualizarEstadoLocomocao(velocidadeHorizontal > 0.1f);
        }

#if UNITY_EDITOR
        if (shiftPressionado != shiftAnterior || EstaCorrendo != corridaAnterior)
        {
            Debug.Log(
                $"[Corrida] Shift {(shiftPressionado ? "pressionado" : "solto")}, Correndo={EstaCorrendo}, velocidade={velocidadeHorizontal:F2}.",
                this);
        }
        shiftAnterior = shiftPressionado;
        corridaAnterior = EstaCorrendo;
#endif
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        int estadoAtual = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        bool noEstadoCorrida = estadoAtual == EstadoCorrida ||
                               (controleAlternativo != null && estadoAtual == EstadoCaminhada && EstaCorrendo);
        if (noEstadoCorrida && !estadoCorridaAnterior)
            Debug.Log("[Corrida] O Animator está reproduzindo Adult_Run.", this);

        estadoCorridaAnterior = noEstadoCorrida;
    }
#endif

    private void AtualizarEstadoLocomocao(bool emMovimento)
    {
        if (controleAlternativo != null)
        {
            AnimationClip clipe = EstaCorrendo ? clipeCorrida : caminhadaAlternativa;
            if (controleAlternativo[caminhadaOriginal] != clipe)
                controleAlternativo[caminhadaOriginal] = clipe;
        }

        int estado = emMovimento
            ? (EstaCorrendo && controleAlternativo == null ? EstadoCorrida : EstadoCaminhada)
            : EstadoIdle;
        if (!animator.HasState(0, estado) || animator.GetCurrentAnimatorStateInfo(0).fullPathHash == estado)
            return;

        if (!animator.IsInTransition(0) || animator.GetNextAnimatorStateInfo(0).fullPathHash != estado)
            animator.CrossFadeInFixedTime(estado, 0.12f, 0);
    }

    private void PrepararCorridaAlternativa()
    {
        RuntimeAnimatorController controleAtual = animator.runtimeAnimatorController;
        AnimatorOverrideController substituicoes = controleAtual as AnimatorOverrideController;
        RuntimeAnimatorController controleBase = substituicoes != null
            ? substituicoes.runtimeAnimatorController : controleAtual;

        foreach (AnimationClip clipe in controleBase.animationClips)
        {
            if (clipe != null && clipe.name == "Adult_Walk")
            {
                caminhadaOriginal = clipe;
                break;
            }
        }

        if (clipeCorrida == null || caminhadaOriginal == null)
        {
            Debug.LogWarning("O Animator do jogador não disponibilizou Adult_Run nem os clipes para a corrida alternativa.", animator);
            return;
        }

        controleAlternativo = new AnimatorOverrideController(controleBase);
        if (substituicoes != null)
        {
            var clipes = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            substituicoes.GetOverrides(clipes);
            controleAlternativo.ApplyOverrides(clipes);
        }

        caminhadaAlternativa = controleAlternativo[caminhadaOriginal] ?? caminhadaOriginal;
        animator.runtimeAnimatorController = controleAlternativo;
    }

    private void OnDisable()
    {
        EstaCorrendo = false;
        distanciaAteOPasso = 0.6f;
    }

    private void AtualizarPassos(float velocidadeHorizontal)
    {
        if (fontePassos == null || somPasso == null || !controller.isGrounded || velocidadeHorizontal < 0.2f)
            return;

        distanciaAteOPasso -= velocidadeHorizontal * Time.deltaTime;
        if (distanciaAteOPasso > 0f)
            return;

        distanciaAteOPasso = EstaCorrendo ? 1.65f : 1.4f;
        fontePassos.PlayOneShot(somPasso, EstaCorrendo ? 0.49f : 0.35f);
    }

    private void OnDestroy()
    {
        if (controleAlternativo != null)
            Destroy(controleAlternativo);
    }
}
