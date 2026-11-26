using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(200)]
public class TiroJogador : MonoBehaviour
{
    [SerializeField] private Camera cameraDoJogo;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform pistola;
    [SerializeField] private ParticleSystem claraoTiro;
    [SerializeField] private AudioSource fonteTiro;
    [SerializeField] private GameObject prefabRifle;
    [SerializeField] private Material materialRifle;
    [SerializeField] private AudioClip somRifle;
    [SerializeField] private AudioClip somRajadaRifle;
    [SerializeField] private Vector3 empunhaduraRifle = new Vector3(0f, -0.065f, 0.13f);
    [SerializeField] private Vector3 apoioRifle = new Vector3(0f, -0.035f, -0.31f);
    [SerializeField, Min(0.05f)] private float intervaloRifle = 0.1f;
    [SerializeField] private LayerMask camadasDoTiro = Physics.DefaultRaycastLayers;
    [SerializeField, Min(1)] private int dano = 25;
    [SerializeField, Min(1f)] private float alcance = 50f;
    [SerializeField, Min(0.1f)] private float intervalo = 0.5f;
    [SerializeField, Min(0.1f)] private float tempoParaGuardar = 1.5f;

    // O disparo do clipe Shooting começa perto do frame 9 a 30 FPS.
    private const float AtrasoPrimeiroDisparo = 0.3f;
    private const float DuracaoSacar = 0.12f;
    private const float DuracaoGuardar = 0.3f;

    private static readonly int PistolaEquipada = Animator.StringToHash("PistolaEquipada");
    private static readonly int Atirar = Animator.StringToHash("Atirar");
    private static readonly int RifleIdle = Animator.StringToHash("Rifle.Rifle Idle");
    private static readonly int RifleShooting = Animator.StringToHash("Rifle.Rifle Shooting");

    private int camadaTiro = -1;
    private int camadaRifle = -1;
    private Transform tronco;
    private Transform malhaArma;
    private Transform rifle;
    private Transform bracoEsquerdo;
    private Transform antebracoEsquerdo;
    private Transform maoEsquerda;
    private Quaternion rotacaoBaseRifle;
    private ParticleSystem claraoRifle;
    private AudioSource fonteRajadaRifle;
    private bool usandoRifle;
    private bool emRajada;
    private double proximoTiroRifle;
    private int tirosRifleNesteQuadro;
    private Vector3 bocaLocal;
    private Vector3 direcaoCanoLocal;
    private bool estaArmado;
    private bool disparoNesteQuadro;
    private float? tiroAoSacarEm;
    private float guardarEm;
    private float proximoTiro;
    private float pesoTiro;

    public bool EstaMirando => isActiveAndEnabled && estaArmado;
    public string EquipamentoAtual => usandoRifle ? "AK74" : "PISTOLA";

    private void Awake()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            camadaTiro = animator.GetLayerIndex("Tiro");
            camadaRifle = animator.GetLayerIndex("Rifle");

            if (animator.isHuman && pistola != null)
            {
                bracoEsquerdo = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                antebracoEsquerdo = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                maoEsquerda = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                tronco = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (tronco == null || !pistola.IsChildOf(tronco))
                    tronco = animator.GetBoneTransform(HumanBodyBones.Spine);

                if (tronco != null && !pistola.IsChildOf(tronco))
                    tronco = null;
            }
        }

        ConfigurarCano(pistola, claraoTiro, false);
        GuardarPistola();
    }

    private void ConfigurarCano(Transform arma, ParticleSystem clarao, bool isRifle)
    {
        malhaArma = null;
        if (arma != null && arma.GetComponentInChildren<MeshFilter>(true) is { } filtro &&
            filtro.sharedMesh != null)
        {
            malhaArma = filtro.transform;
            Bounds limites = filtro.sharedMesh.bounds;
            Vector3 empunhadura = malhaArma.InverseTransformPoint(arma.position);
            bool canoNoXPositivo = limites.max.x - empunhadura.x >= empunhadura.x - limites.min.x;
            direcaoCanoLocal = isRifle ? Vector3.back : (canoNoXPositivo ? Vector3.right : Vector3.left);
            bocaLocal = isRifle
                ? new Vector3(limites.center.x, Mathf.Lerp(limites.min.y, limites.max.y, 0.70f), limites.min.z)
                : new Vector3(canoNoXPositivo ? limites.max.x : limites.min.x,
                    Mathf.Lerp(limites.min.y, limites.max.y, 0.86f), limites.center.z);

            if (clarao != null)
            {
                if (isRifle && arma.Find("PontoDisparoAK") is { } bocaDoRifle)
                {
                    clarao.transform.SetParent(bocaDoRifle, false);
                    clarao.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    var forma = clarao.shape;
                    forma.enabled = false;
                    return;
                }

                Transform pontoDisparo = isRifle ? clarao.transform : clarao.transform.parent ?? clarao.transform;
                pontoDisparo.SetPositionAndRotation(
                    malhaArma.TransformPoint(bocaLocal),
                    Quaternion.LookRotation(malhaArma.TransformDirection(direcaoCanoLocal), arma.up));
            }
        }
    }

    public bool EquiparRifle()
    {
        if (!isActiveAndEnabled || usandoRifle || prefabRifle == null || pistola == null ||
            animator == null || camadaRifle < 0 || !animator.HasState(camadaRifle, RifleIdle) ||
            !animator.HasState(camadaRifle, RifleShooting))
            return false;

        if (rifle == null)
        {
            Vector3 frente = malhaArma != null ? malhaArma.TransformDirection(direcaoCanoLocal) : pistola.forward;
            rifle = Instantiate(prefabRifle, pistola.parent, false).transform;
            rifle.rotation = Quaternion.LookRotation(-frente, pistola.up);
            rotacaoBaseRifle = rifle.localRotation;
            if (somRajadaRifle != null)
            {
                fonteRajadaRifle = rifle.gameObject.AddComponent<AudioSource>();
                fonteRajadaRifle.playOnAwake = false;
                fonteRajadaRifle.loop = true;
                fonteRajadaRifle.clip = somRajadaRifle;
                fonteRajadaRifle.priority = 64;
                fonteRajadaRifle.volume = fonteTiro != null ? fonteTiro.volume * 0.8f : 0.6f;
                fonteRajadaRifle.pitch = somRajadaRifle.length / (10f * intervaloRifle);
            }
            if (materialRifle != null)
            {
                foreach (MeshRenderer parte in rifle.GetComponentsInChildren<MeshRenderer>(true))
                    parte.sharedMaterial = materialRifle;
            }
            if (claraoTiro != null)
                claraoRifle = Instantiate(claraoTiro, rifle, false);
        }

        tiroAoSacarEm = null;
        disparoNesteQuadro = false;
        GuardarPistola();
        if (camadaTiro >= 0)
            animator.ResetTrigger(Atirar);
        usandoRifle = true;
        estaArmado = true;
        pesoTiro = 1f;
        rifle.gameObject.SetActive(true);
        PosicionarRifle();
        proximoTiroRifle = Time.timeAsDouble;
        animator.SetLayerWeight(camadaRifle, 1f);
        animator.Play(RifleIdle, camadaRifle, 0f);
        ConfigurarCano(rifle, claraoRifle, true);
        return true;
    }

    public void DesequiparRifle()
    {
        if (!usandoRifle)
            return;

        PararRajada();
        usandoRifle = false;
        disparoNesteQuadro = false;
        tirosRifleNesteQuadro = 0;
        if (rifle != null)
            rifle.gameObject.SetActive(false);
        if (animator != null)
            animator.SetLayerWeight(camadaRifle, 0f);
        GuardarPistola();
        ConfigurarCano(pistola, claraoTiro, false);
    }

    private void OnDisable()
    {
        DesequiparRifle();
        tiroAoSacarEm = null;
        disparoNesteQuadro = false;
        if (camadaTiro >= 0 && animator != null)
            animator.ResetTrigger(Atirar);

        GuardarPistola();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool clicou = Mouse.current != null &&
                      Mouse.current.leftButton.wasPressedThisFrame;
        bool pressionado = Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        bool clicou = Input.GetMouseButtonDown(0);
        bool pressionado = Input.GetMouseButton(0);
#endif

        if (usandoRifle)
        {
            AtualizarRajada(clicou || pressionado);
            return;
        }

        float agora = Time.time;
        if (tiroAoSacarEm is float primeiroTiro && agora >= primeiroTiro)
        {
            tiroAoSacarEm = null;
            if (cameraDoJogo != null)
                Disparar();
        }

        if (clicou && cameraDoJogo != null && agora >= proximoTiro &&
            !tiroAoSacarEm.HasValue)
        {
            if (estaArmado)
            {
                if (camadaTiro >= 0 && animator != null)
                    animator.SetTrigger(Atirar);

                Disparar();
            }
            else
                SacarPistola(agora);
        }

        if (estaArmado)
            AtualizarPesoTiro(tiroAoSacarEm.HasValue || agora < guardarEm);
    }

    private void AtualizarRajada(bool disparar)
    {
        if (!disparar || cameraDoJogo == null || Time.timeScale <= 0f)
        {
            PararRajada();
            return;
        }

        double agora = Time.timeAsDouble;
        if (agora < proximoTiroRifle)
            return;

        if (!emRajada)
        {
            emRajada = true;
            proximoTiroRifle = agora;
            animator.Play(RifleShooting, camadaRifle, 0f);
        }

        while (proximoTiroRifle <= agora && tirosRifleNesteQuadro < 3)
        {
            tirosRifleNesteQuadro++;
            proximoTiroRifle += intervaloRifle;
        }

        if (proximoTiroRifle <= agora)
            proximoTiroRifle = agora + intervaloRifle;
    }

    private void PararRajada()
    {
        if (!emRajada)
            return;

        emRajada = false;
        if (fonteRajadaRifle != null)
            fonteRajadaRifle.Stop();
        if (animator != null)
            animator.CrossFadeInFixedTime(RifleIdle, 0.06f, camadaRifle);
    }

    private void SacarPistola(float agora)
    {
        estaArmado = true;
        tiroAoSacarEm = agora + AtrasoPrimeiroDisparo;

        if (pistola != null)
            pistola.gameObject.SetActive(true);

        if (camadaTiro >= 0 && animator != null)
            animator.SetBool(PistolaEquipada, true);
    }

    private void AtualizarPesoTiro(bool manterPoseArmada)
    {
        float alvo = manterPoseArmada ? 1f : 0f;
        float duracao = manterPoseArmada ? DuracaoSacar : DuracaoGuardar;
        pesoTiro = Mathf.MoveTowards(pesoTiro, alvo, Time.deltaTime / duracao);

        if (camadaTiro >= 0 && animator != null)
            animator.SetLayerWeight(camadaTiro, pesoTiro);

        if (!manterPoseArmada && pesoTiro == 0f)
            GuardarPistola();
    }

    private void GuardarPistola()
    {
        estaArmado = false;
        pesoTiro = 0f;
        if (pistola != null)
            pistola.gameObject.SetActive(false);

        if (camadaTiro >= 0 && animator != null)
        {
            animator.SetBool(PistolaEquipada, false);
            animator.SetLayerWeight(camadaTiro, 0f);
        }
    }

    private void Disparar()
    {
        proximoTiro = Time.time + intervalo;
        guardarEm = Time.time + tempoParaGuardar;
        disparoNesteQuadro = true;
    }

    private void LateUpdate()
    {
        int tiros = usandoRifle ? tirosRifleNesteQuadro : (disparoNesteQuadro ? 1 : 0);
        disparoNesteQuadro = false;
        tirosRifleNesteQuadro = 0;
        if (usandoRifle)
            PosicionarRifle();

        bool alinharPose = pesoTiro > 0f && tronco != null && malhaArma != null;
        if (cameraDoJogo == null || (tiros == 0 && !alinharPose))
        {
            if (usandoRifle)
                AjustarMaoDeApoio();
            return;
        }

        Ray raio = cameraDoJogo.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool acertou = BuscarAcerto(raio, out RaycastHit hit);
        if (alinharPose)
        {
            Vector3 alvo = acertou && Vector3.Dot(hit.point - tronco.position, raio.direction) > 0.5f
                ? hit.point
                : raio.GetPoint(alcance);
            AlinharCano(alvo);
        }
        if (usandoRifle)
            AjustarMaoDeApoio();

        if (tiros == 0)
            return;

        if (usandoRifle && fonteRajadaRifle != null)
        {
            if (!fonteRajadaRifle.isPlaying)
                fonteRajadaRifle.Play();
        }
        else if (fonteTiro != null)
        {
            AudioClip som = usandoRifle ? somRifle : fonteTiro.clip;
            if (som != null)
                fonteTiro.PlayOneShot(som);
        }

        ParticleSystem clarao = usandoRifle ? claraoRifle : claraoTiro;
        if (clarao != null)
        {
            clarao.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            clarao.Play();
        }

        for (int i = 0; i < tiros; i++)
        {
            if (i > 0)
                acertou = BuscarAcerto(raio, out hit);
            if (acertou && hit.collider.GetComponentInParent<ZumbiInimigo>() is { } zumbi)
                zumbi.ReceberDano(dano, hit.point);
        }
    }

    private void PosicionarRifle()
    {
        rifle.localRotation = rotacaoBaseRifle;
        Vector3 pegada = pistola.position;
        if (maoEsquerda != null)
        {
            Vector3 entreMaos = maoEsquerda.position - pegada;
            if (entreMaos.sqrMagnitude > 0.001f)
            {
                Vector3 entrePegadas = rifle.TransformVector(apoioRifle - empunhaduraRifle);
                rifle.rotation = Quaternion.FromToRotation(entrePegadas, entreMaos) * rifle.rotation;
            }
        }

        rifle.position += pegada - rifle.TransformPoint(empunhaduraRifle);
    }

    private void AjustarMaoDeApoio()
    {
        if (bracoEsquerdo == null || antebracoEsquerdo == null || maoEsquerda == null)
            return;

        Vector3 ombro = bracoEsquerdo.position;
        Vector3 cotovelo = antebracoEsquerdo.position;
        Vector3 pulso = maoEsquerda.position;
        Vector3 ateApoio = rifle.TransformPoint(apoioRifle) - ombro;
        float braco = Vector3.Distance(ombro, cotovelo);
        float antebraco = Vector3.Distance(cotovelo, pulso);
        if (ateApoio.sqrMagnitude < 0.0001f || braco < 0.01f || antebraco < 0.01f)
            return;

        Vector3 direcao = ateApoio.normalized;
        float distancia = Mathf.Clamp(ateApoio.magnitude,
            Mathf.Abs(braco - antebraco) + 0.001f, braco + antebraco - 0.001f);
        Vector3 dobra = Vector3.ProjectOnPlane(cotovelo - ombro, direcao).normalized;
        if (dobra.sqrMagnitude < 0.001f)
            dobra = Vector3.Cross(direcao, rifle.right).normalized;

        // Resolve os dois segmentos do braço mantendo a dobra do cotovelo da animação.
        float eixo = (braco * braco - antebraco * antebraco + distancia * distancia) / (2f * distancia);
        float altura = Mathf.Sqrt(Mathf.Max(0f, braco * braco - eixo * eixo));
        Vector3 cotoveloDesejado = ombro + direcao * eixo + dobra * altura;
        Vector3 pulsoDesejado = ombro + direcao * distancia;
        Quaternion rotacaoMao = maoEsquerda.rotation;

        bracoEsquerdo.rotation = Quaternion.FromToRotation(cotovelo - ombro, cotoveloDesejado - ombro)
            * bracoEsquerdo.rotation;
        cotovelo = antebracoEsquerdo.position;
        antebracoEsquerdo.rotation = Quaternion.FromToRotation(maoEsquerda.position - cotovelo, pulsoDesejado - cotovelo)
            * antebracoEsquerdo.rotation;
        maoEsquerda.rotation = rotacaoMao;
    }

    private bool BuscarAcerto(Ray raio, out RaycastHit hit)
    {
        if (!Physics.Raycast(raio, out hit, alcance, camadasDoTiro, QueryTriggerInteraction.Ignore))
            return false;
        if (!hit.collider.transform.IsChildOf(transform))
            return true;

        float distanciaMaisProxima = float.PositiveInfinity;
        foreach (RaycastHit candidato in Physics.RaycastAll(
                     raio, alcance, camadasDoTiro, QueryTriggerInteraction.Ignore))
        {
            if (candidato.distance < distanciaMaisProxima &&
                !candidato.collider.transform.IsChildOf(transform))
            {
                distanciaMaisProxima = candidato.distance;
                hit = candidato;
            }
        }

        return !float.IsPositiveInfinity(distanciaMaisProxima);
    }

    private void AlinharCano(Vector3 alvo)
    {
        Vector3 boca = malhaArma.TransformPoint(bocaLocal);
        Vector3 direcaoCano = malhaArma.TransformDirection(direcaoCanoLocal);
        Vector3 troncoAteAlvo = alvo - tronco.position;
        Vector3 troncoAteBoca = boca - tronco.position;
        float projecaoBoca = Vector3.Dot(troncoAteBoca, direcaoCano);
        Vector3 deslocamentoCano = troncoAteBoca - projecaoBoca * direcaoCano;
        float distanciaAoLongoDoCano = troncoAteAlvo.sqrMagnitude - deslocamentoCano.sqrMagnitude;
        if (distanciaAoLongoDoCano <= 0f)
            return;

        float projecaoAlvo = Mathf.Sqrt(distanciaAoLongoDoCano);
        if (projecaoAlvo <= projecaoBoca)
            return;

        // Compara pontos à mesma distância do peito, mantendo mãos e arma juntas.
        Vector3 pontoNaLinhaDoCano = deslocamentoCano + projecaoAlvo * direcaoCano;
        Quaternion ajuste = Quaternion.FromToRotation(pontoNaLinhaDoCano, troncoAteAlvo);
        tronco.rotation = Quaternion.Slerp(Quaternion.identity, ajuste, pesoTiro) * tronco.rotation;
    }
}
