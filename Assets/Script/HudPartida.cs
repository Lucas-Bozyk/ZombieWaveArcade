using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HudPartida : MonoBehaviour
{
    [SerializeField] private VidaJogador jogador;
    [SerializeField] private SpawnOndas ondas;
    [SerializeField] private RectTransform painelVida;
    [SerializeField] private RectTransform painelPartida;
    [SerializeField] private Image barraVida;
    [SerializeField] private TMP_Text textoVida;
    [SerializeField] private TMP_Text textoOnda;
    [SerializeField] private TMP_Text textoAbates;
    [SerializeField] private TMP_Text textoZumbisVivos;
    [SerializeField] private TMP_Text textoTempo;

    private static readonly Color32 VidaNormal = new Color32(105, 205, 153, 255);
    private static readonly Color32 VidaBaixa = new Color32(239, 91, 83, 255);

    private int ultimaVida = -1;
    private int ultimaOnda = -1;
    private int ultimosAbates = -1;
    private int ultimosZumbisVivos = -1;
    private int ultimoSegundo = -1;
    private TiroJogador tiroJogador;
    private TMP_Text textoEquipamento;
    private TMP_Text textoSkull;
    private TMP_Text textoNotificacao;
    private TMP_Text textoDrop;
    private int ultimoTempoSkull = -1;
    private int ultimasCaveirasSkull;
    private float proximaBuscaDrop;

    internal static void Vincular(SpawnOndas ondas, VidaJogador jogador)
    {
        foreach (GameObject raiz in ondas.gameObject.scene.GetRootGameObjects())
        {
            if (!raiz.TryGetComponent<Canvas>(out _))
                continue;

            Transform layout = raiz.transform.Find("HUD");
            if (layout == null)
                continue;

            if (!layout.TryGetComponent<HudPartida>(out var hud))
                hud = layout.gameObject.AddComponent<HudPartida>();

            hud.ondas = ondas;
            hud.jogador = jogador;
            hud.VincularCampos();
            return;
        }
    }

    private void OnEnable()
    {
        VincularCampos();
    }

    private void VincularCampos()
    {
        painelVida = painelVida != null ? painelVida : transform.Find("Painel Vida") as RectTransform;
        painelPartida = painelPartida != null
            ? painelPartida : transform.Find("Painel Partida") as RectTransform;

        if (painelVida != null)
        {
            textoVida = ResolverCampo(painelVida, textoVida, "Valor Vida");
            barraVida = ResolverCampo(painelVida, barraVida, "Fundo Barra Vida/Barra Vida");
        }

        if (painelPartida != null)
        {
            textoOnda = ResolverCampo(painelPartida, textoOnda, "Valor Onda");
            textoAbates = ResolverCampo(painelPartida, textoAbates, "Valor Abates");
            textoZumbisVivos = ResolverCampo(painelPartida, textoZumbisVivos, "Valor ZumbisVivos");
            textoTempo = ResolverCampo(painelPartida, textoTempo, "Valor Tempo");
        }

        if (textoEquipamento == null)
        {
            textoEquipamento = CriarLinha("Equipamento", 18f, 20f, Color.white);
            textoSkull = CriarLinha("Skull Wave", 42f, 26f, new Color32(255, 48, 48, 255));
            textoNotificacao = CriarLinha("Notificação", 73f, 20f, Color.white);
            textoDrop = CriarLinha("Drop mais próximo", 97f, 18f, Color.white);
        }

        if (jogador != null)
            tiroJogador = jogador.GetComponentInParent<TiroJogador>();

        OnRectTransformDimensionsChange();
    }

    private TMP_Text CriarLinha(string nome, float margem, float tamanho, Color cor)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        objeto.layer = gameObject.layer;
        var area = (RectTransform)objeto.transform;
        area.SetParent(transform, false);
        area.anchorMin = new Vector2(0.25f, 1f);
        area.anchorMax = new Vector2(0.75f, 1f);
        area.pivot = new Vector2(0.5f, 1f);
        area.anchoredPosition = new Vector2(0f, -margem);
        area.sizeDelta = new Vector2(0f, tamanho + 8f);

        TMP_Text texto = objeto.GetComponent<TMP_Text>();
        if (textoTempo != null && textoTempo.font != null)
            texto.font = textoTempo.font;
        texto.fontSize = tamanho;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = cor;
        texto.raycastTarget = false;
        return texto;
    }

    private static T ResolverCampo<T>(Transform painel, T campo, string caminho) where T : Component
    {
        return campo != null ? campo : painel.Find(caminho)?.GetComponent<T>();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (painelVida == null || painelPartida == null)
            return;

        Rect area = ((RectTransform)transform).rect;
        if (area.width <= 0f || area.height <= 0f)
            return;

        float escala = Mathf.Clamp(Mathf.Min(area.width / 1280f, area.height / 720f), 0.65f, 1.5f);
        painelVida.localScale = Vector3.one * escala;
        painelPartida.localScale = Vector3.one * escala;
    }

    private void LateUpdate()
    {
        if (jogador != null && jogador.VidaAtual != ultimaVida)
        {
            ultimaVida = jogador.VidaAtual;
            if (textoVida != null)
                textoVida.text = $"{ultimaVida} / {jogador.VidaMaxima}";

            if (barraVida != null)
            {
                float proporcao = Mathf.Clamp01((float)ultimaVida / jogador.VidaMaxima);
                barraVida.rectTransform.anchorMax = new Vector2(proporcao, 1f);
                barraVida.color = proporcao <= 0.25f ? VidaBaixa : VidaNormal;
            }
        }

        if (ondas != null)
        {
            AtualizarNumero(textoOnda, ondas.OndaAtual, ref ultimaOnda);
            AtualizarNumero(textoAbates, ondas.Abates, ref ultimosAbates);
            AtualizarNumero(textoZumbisVivos, ondas.ZumbisVivos, ref ultimosZumbisVivos);
        }

        if (textoTempo != null && jogador != null && jogador.VidaAtual > 0)
        {
            int segundo = Mathf.FloorToInt(Time.timeSinceLevelLoad);
            if (segundo != ultimoSegundo)
            {
                ultimoSegundo = segundo;
                textoTempo.text = $"{segundo / 60:00}:{segundo % 60:00}";
            }
        }

        AtualizarEventoEDrops();
    }

    private void AtualizarEventoEDrops()
    {
        bool emPartida = jogador != null && jogador.VidaAtual > 0 && ondas != null;
        textoEquipamento.gameObject.SetActive(emPartida && tiroJogador != null);
        textoSkull.gameObject.SetActive(emPartida && ondas.SkullAtiva);
        textoNotificacao.gameObject.SetActive(emPartida);
        textoDrop.gameObject.SetActive(emPartida);
        if (!emPartida)
            return;

        if (tiroJogador != null)
            textoEquipamento.text = tiroJogador.EquipamentoAtual;
        if (ondas.SkullAtiva)
        {
            int segundos = ondas.SegundosSkull;
            int caveiras = ondas.CaveirasSkull;
            if (segundos != ultimoTempoSkull || caveiras != ultimasCaveirasSkull)
            {
                ultimoTempoSkull = segundos;
                ultimasCaveirasSkull = caveiras;
                string titulo = caveiras > 1 ? $"SKULL WAVE ×{caveiras}" : "SKULL WAVE";
                textoSkull.text = $"{titulo}  {segundos / 60:00}:{segundos % 60:00}";
            }
        }
        else
            ultimoTempoSkull = -1;

        SistemaDrops drops = ondas.Drops;
        textoNotificacao.text = drops != null ? drops.Mensagem : string.Empty;
        if (Time.time < proximaBuscaDrop)
            return;

        proximaBuscaDrop = Time.time + 0.1f;
        ItemColetavel maisProximo = null;
        float distancia = float.PositiveInfinity;
        if (drops != null)
        {
            var itens = drops.Ativos;
            Vector3 origem = jogador.transform.position;
            for (int i = 0; i < itens.Count; i++)
            {
                ItemColetavel item = itens[i];
                if (item == null || !item.Disponivel)
                    continue;

                Vector3 delta = item.transform.position - origem;
                delta.y = 0f;
                if (delta.sqrMagnitude < distancia)
                {
                    maisProximo = item;
                    distancia = delta.sqrMagnitude;
                }
            }
        }

        if (maisProximo == null)
        {
            textoDrop.text = string.Empty;
            return;
        }

        bool coracao = maisProximo.Tipo == ItemColetavel.TipoItem.Coracao;
        textoDrop.color = coracao ? GraficoRadar.CorCoracao : GraficoRadar.CorCaveira;
        textoDrop.text = $"{(coracao ? "CORAÇÃO" : "CAVEIRA")}  {Mathf.RoundToInt(Mathf.Sqrt(distancia))} m";
    }

    private static void AtualizarNumero(TMP_Text texto, int valor, ref int anterior)
    {
        if (texto == null || valor == anterior)
            return;

        anterior = valor;
        texto.text = valor.ToString();
    }
}
