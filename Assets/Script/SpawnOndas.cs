using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnOndas : MonoBehaviour
{
    [SerializeField] private ZumbiInimigo prefabZumbi;
    [SerializeField] private Transform jogador;
    [SerializeField] private Transform[] pontosDeSpawn;
    [SerializeField, Min(1)] private int zumbisNaPrimeiraOnda = 3;
    [SerializeField, Min(0)] private int aumentoPorOnda = 2;
    [SerializeField, Min(0.1f)] private float intervaloEntreZumbis = 0.8f;
    [SerializeField, Min(0f)] private float pausaEntreOndas = 5f;
    [SerializeField] private SistemaDrops sistemaDrops;

    private const int LimiteZumbisVivos = 250;
    private const int ReforcosPorQuadro = 4;
    private const int ZumbisPorCaveira = 50;
    private const double DuracaoSkull = 120d;
    private readonly List<ZumbiInimigo> zumbisVivos = new();
    private IReadOnlyList<ZumbiInimigo> leituraZumbisVivos;
    private VidaJogador vidaJogador;
    private TiroJogador tiroJogador;
    private double fimSkull;
    private int proximaVagaSkull;
    private int reforcosPendentes;

    public int OndaAtual { get; private set; }
    public int Abates { get; private set; }
    public int CaveirasSkull { get; private set; }
    public bool SkullAtiva => fimSkull > 0d;
    public int SegundosSkull => Mathf.CeilToInt((float)System.Math.Max(0d, fimSkull - Time.timeAsDouble));
    public SistemaDrops Drops => sistemaDrops;
    public bool EhJogador(VidaJogador vida) => vida == vidaJogador;
    public IReadOnlyList<ZumbiInimigo> InimigosAtivos =>
        leituraZumbisVivos ??= zumbisVivos.AsReadOnly();

    public int ZumbisVivos
    {
        get
        {
            int quantidade = 0;
            foreach (ZumbiInimigo zumbi in zumbisVivos)
            {
                if (zumbi != null && zumbi.EstaVivo)
                    quantidade++;
            }

            return quantidade;
        }
    }

    private bool JogadorAtivo =>
        jogador != null && jogador.gameObject.activeInHierarchy &&
        vidaJogador is not { VidaAtual: 0 };

    private IEnumerator Start()
    {
        if (prefabZumbi == null || jogador == null ||
            pontosDeSpawn == null || pontosDeSpawn.Length == 0 ||
            System.Array.Exists(pontosDeSpawn, ponto => ponto == null))
        {
            Debug.LogError(
                "SpawnOndas: preencha o prefab, o jogador e todos os pontos de spawn.",
                this);
            yield break;
        }

        vidaJogador = jogador.GetComponentInParent<VidaJogador>();
        tiroJogador = jogador.GetComponentInParent<TiroJogador>();
        HudPartida.Vincular(this, vidaJogador);

        int onda = 1;
        int quantidade = zumbisNaPrimeiraOnda;
        while (JogadorAtivo)
        {
            while (SkullAtiva && JogadorAtivo)
                yield return null;
            if (!JogadorAtivo)
                yield break;

            OndaAtual = onda;
            int pontosLiberados = Mathf.Min(onda, pontosDeSpawn.Length);
            // Começa pelo ponto mais recente e alterna entre os liberados.
            int indiceDoPonto = pontosLiberados - 1;
            Debug.Log($"Onda {onda}: {quantidade} zumbis em {pontosLiberados} ponto(s).", this);

            for (int i = 0; i < quantidade; i++)
            {
                while (JogadorAtivo && (SkullAtiva || !TemVagaParaZumbi()))
                    yield return null;

                if (!JogadorAtivo)
                    yield break;

                Transform ponto = pontosDeSpawn[indiceDoPonto];
                indiceDoPonto = (indiceDoPonto + 1) % pontosLiberados;
                CriarZumbi(ponto.position, ponto.rotation);

                if (i < quantidade - 1)
                    yield return EsperarFluxoNormal(intervaloEntreZumbis);
            }

            yield return AguardarZumbis();
            if (!JogadorAtivo)
                yield break;

            yield return EsperarFluxoNormal(pausaEntreOndas);
            // Uma Skull Wave também pode começar durante o intervalo entre ondas.
            yield return AguardarZumbis();
            onda++;
            quantidade += aumentoPorOnda;
        }
    }

    private IEnumerator AguardarZumbis()
    {
        while (JogadorAtivo && (SkullAtiva || zumbisVivos.Count > 0))
        {
            zumbisVivos.RemoveAll(zumbi => zumbi == null || !zumbi.EstaVivo);
            if (SkullAtiva || zumbisVivos.Count > 0)
                yield return null;
        }
    }

    private IEnumerator EsperarFluxoNormal(float segundos)
    {
        while (JogadorAtivo && (SkullAtiva || segundos > 0f))
        {
            yield return null;
            if (!SkullAtiva)
                segundos -= Time.deltaTime;
        }
    }

    private void CriarZumbi(Vector3 posicao, Quaternion rotacao)
    {
        ZumbiInimigo zumbi = Instantiate(prefabZumbi, posicao, rotacao);
        zumbi.DefinirAlvo(jogador);
        zumbisVivos.Add(zumbi);
        zumbi.Eliminado += RegistrarAbate;
    }

    private bool TemVagaParaZumbi()
    {
        if (zumbisVivos.Count < LimiteZumbisVivos)
            return true;

        zumbisVivos.RemoveAll(zumbi => zumbi == null || !zumbi.EstaVivo);
        return zumbisVivos.Count < LimiteZumbisVivos;
    }

    public bool TentarAtivarCaveira()
    {
        if (OndaAtual < 1 || !JogadorAtivo || tiroJogador == null)
            return false;

        int faltam = (CaveirasSkull + 1) * ZumbisPorCaveira - ZumbisVivos;
        if (!SkullAtiva && (faltam < 0 || !tiroJogador.EquiparRifle()))
            return false;

        CaveirasSkull++;
        fimSkull = System.Math.Max(fimSkull, Time.timeAsDouble) + DuracaoSkull;
        reforcosPendentes = Mathf.Max(reforcosPendentes, faltam);
        return true;
    }

    private void CriarReforcosSkull()
    {
        int pontosLiberados = Mathf.Min(OndaAtual, pontosDeSpawn.Length);
        for (int i = 0; i < ReforcosPorQuadro && reforcosPendentes > 0 && TemVagaParaZumbi(); i++)
        {
            int indice = proximaVagaSkull++;
            Transform ponto = pontosDeSpawn[indice % pontosLiberados];
            int vaga = indice / pontosLiberados;
            float angulo = vaga * 2.399963f;
            float raio = 1.15f * Mathf.Sqrt(vaga);
            Vector3 posicao = ponto.position + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * raio;
            posicao = NavMesh.SamplePosition(posicao, out NavMeshHit piso, 2f, NavMesh.AllAreas)
                ? piso.position : ponto.position;
            CriarZumbi(posicao, ponto.rotation);
            reforcosPendentes--;
        }
    }

    private void Update()
    {
        if (!SkullAtiva)
            return;

        if (JogadorAtivo && Time.timeAsDouble < fimSkull)
        {
            if (reforcosPendentes > 0 && Time.deltaTime > 0f)
                CriarReforcosSkull();
            return;
        }

        fimSkull = 0d;
        CaveirasSkull = 0;
        proximaVagaSkull = 0;
        reforcosPendentes = 0;
        if (tiroJogador != null)
            tiroJogador.DesequiparRifle();
        if (JogadorAtivo && sistemaDrops != null)
            sistemaDrops.Notificar("EVENTO ENCERRADO");
    }

    private void RegistrarAbate(ZumbiInimigo zumbi)
    {
        zumbi.Eliminado -= RegistrarAbate;
        if (!zumbisVivos.Remove(zumbi))
            return;

        Abates++;
        if (JogadorAtivo && sistemaDrops != null)
            sistemaDrops.SortearDrop(zumbi.transform.position, this);
    }

    private void OnDestroy()
    {
        foreach (ZumbiInimigo zumbi in zumbisVivos)
        {
            if (zumbi != null)
                zumbi.Eliminado -= RegistrarAbate;
        }
    }
}
