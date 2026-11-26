using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class RadarJogador : MonoBehaviour
{
    [SerializeField] private VidaJogador jogador;
    [SerializeField] private SpawnOndas ondas;
    [SerializeField] private RectTransform areaMarcadores;
    [SerializeField] private RectTransform prefabPontoInimigo;
    [SerializeField] private RectTransform marcadorJogador;
    [SerializeField] private RectTransform linhaDeVarredura;

    [SerializeField, Min(0.1f)] private float alcanceEmMetros = 30f;
    [SerializeField, Min(1f)] private float atualizacoesPorSegundo = 10f;
    [SerializeField] private bool acompanharRotacaoJogador = true;
    [SerializeField] private bool ocultarRadarAposMorte = true;
    [SerializeField, Min(0f)] private float velocidadeVarredura = 90f;

    private readonly List<RectTransform> marcadores = new();
    private readonly List<GraficoRadar> marcadoresDeItem = new();
    private int marcadoresVisiveis;
    private float proximaAtualizacao;
    private float anguloVarredura;

    private void OnDisable()
    {
        OcultarMarcadoresExcedentes(0);
        OcultarItensExcedentes(0);
        proximaAtualizacao = 0f;
        anguloVarredura = 0f;

        if (linhaDeVarredura != null)
            linhaDeVarredura.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (jogador == null || (ocultarRadarAposMorte && jogador.VidaAtual <= 0))
        {
            gameObject.SetActive(false);
            return;
        }

        if (linhaDeVarredura != null)
        {
            anguloVarredura = Mathf.Repeat(anguloVarredura + velocidadeVarredura * Time.deltaTime, 360f);
            linhaDeVarredura.localRotation = Quaternion.Euler(0f, 0f, -anguloVarredura);
        }

        if (Time.time < proximaAtualizacao)
            return;

        proximaAtualizacao = Time.time + 1f / atualizacoesPorSegundo;
        AtualizarMarcadores();
    }

    private void AtualizarMarcadores()
    {
        Transform alvo = jogador.transform;
        float angulo = alvo.eulerAngles.y;
        if (marcadorJogador != null)
            marcadorJogador.localRotation = Quaternion.Euler(0f, 0f, acompanharRotacaoJogador ? 0f : -angulo);

        if (ondas == null || areaMarcadores == null || prefabPontoInimigo == null)
        {
            OcultarMarcadoresExcedentes(0);
            OcultarItensExcedentes(0);
            return;
        }

        Rect area = areaMarcadores.rect;
        Rect ponto = prefabPontoInimigo.rect;
        float raioUtil = Mathf.Max(0f, Mathf.Min(area.width, area.height) * 0.5f -
            GraficoRadar.EspessuraBorda - Mathf.Max(ponto.width, ponto.height) * 0.5f);
        float alcanceQuadrado = alcanceEmMetros * alcanceEmMetros;
        float escala = raioUtil / alcanceEmMetros;
        Vector3 origem = alvo.position;
        Quaternion orientacao = acompanharRotacaoJogador
            ? Quaternion.Euler(0f, -angulo, 0f) : Quaternion.identity;

        int visiveis = 0;
        IReadOnlyList<ZumbiInimigo> inimigos = ondas.InimigosAtivos;
        for (int i = 0; i < inimigos.Count; i++)
        {
            ZumbiInimigo zumbi = inimigos[i];
            if (zumbi == null || !zumbi.EstaVivo || !zumbi.gameObject.activeInHierarchy)
                continue;

            Vector3 delta = zumbi.transform.position - origem;
            delta.y = 0f;
            if (delta.sqrMagnitude > alcanceQuadrado)
                continue;

            Vector3 posicao = orientacao * delta;
            if (visiveis == marcadores.Count)
                marcadores.Add(Instantiate(prefabPontoInimigo, areaMarcadores, false));

            RectTransform marcador = marcadores[visiveis];
            marcador.anchoredPosition = new Vector2(posicao.x, posicao.z) * escala;
            if (visiveis >= marcadoresVisiveis)
                marcador.gameObject.SetActive(true);

            visiveis++;
        }

        OcultarMarcadoresExcedentes(visiveis);
        AtualizarItens(origem, orientacao, escala, Mathf.Max(0f, raioUtil - 1f));
    }

    private void AtualizarItens(Vector3 origem, Quaternion orientacao, float escala, float raio)
    {
        int visiveis = 0;
        if (ondas.Drops != null)
        {
            IReadOnlyList<ItemColetavel> itens = ondas.Drops.Ativos;
            for (int i = 0; i < itens.Count; i++)
            {
                ItemColetavel item = itens[i];
                if (item == null || !item.Disponivel)
                    continue;

                if (visiveis == marcadoresDeItem.Count)
                {
                    RectTransform novo = Instantiate(prefabPontoInimigo, areaMarcadores, false);
                    novo.sizeDelta = Vector2.one * 8f;
                    GraficoRadar grafico = novo.GetComponent<GraficoRadar>();
                    grafico.ConfigurarComoItem();
                    marcadoresDeItem.Add(grafico);
                }

                GraficoRadar marcador = marcadoresDeItem[visiveis++];
                Vector3 delta = item.transform.position - origem;
                delta.y = 0f;
                Vector3 posicao = orientacao * delta;
                marcador.rectTransform.anchoredPosition =
                    Vector2.ClampMagnitude(new Vector2(posicao.x, posicao.z) * escala, raio);
                marcador.color = item.Tipo == ItemColetavel.TipoItem.Coracao
                    ? GraficoRadar.CorCoracao : GraficoRadar.CorCaveira;
                if (!marcador.gameObject.activeSelf)
                    marcador.gameObject.SetActive(true);
                marcador.rectTransform.SetAsLastSibling();
            }
        }

        OcultarItensExcedentes(visiveis);
    }

    private void OcultarItensExcedentes(int quantidade)
    {
        for (int i = quantidade; i < marcadoresDeItem.Count; i++)
        {
            if (marcadoresDeItem[i] != null)
                marcadoresDeItem[i].gameObject.SetActive(false);
        }
    }

    private void OcultarMarcadoresExcedentes(int visiveis)
    {
        for (int i = visiveis; i < marcadoresVisiveis; i++)
        {
            if (marcadores[i] != null)
                marcadores[i].gameObject.SetActive(false);
        }

        marcadoresVisiveis = visiveis;
    }
}
