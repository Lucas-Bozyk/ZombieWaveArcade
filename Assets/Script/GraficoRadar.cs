using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Gráfico do Radar")]
public sealed class GraficoRadar : MaskableGraphic
{
    private enum Formato { Fundo, Varredura, Jogador, Inimigo, Item }

    [SerializeField] private Formato formato;

    internal const float EspessuraBorda = 2f;
    internal static readonly Color32 CorCoracao = new Color32(255, 135, 210, 255);
    internal static readonly Color32 CorCaveira = new Color32(255, 221, 95, 255);
    private const int Segmentos = 96;
    private static readonly Color32 VerdeBorda = new Color32(57, 255, 136, 255);
    private static readonly Color32 VerdeGrade = new Color32(57, 255, 136, 64);

    protected override void OnPopulateMesh(VertexHelper malha)
    {
        malha.Clear();

        Rect retangulo = rectTransform.rect;
        Vector2 centro = retangulo.center;
        float raio = Mathf.Min(retangulo.width, retangulo.height) * 0.5f;
        if (raio <= 0f)
            return;

        switch (formato)
        {
            case Formato.Fundo:
                DesenharDisco(malha, centro, raio, Segmentos, color);
                float raioInterno = Mathf.Max(0f, raio - EspessuraBorda);
                for (int i = 1; i <= 3; i++)
                    DesenharAnel(malha, centro, raioInterno * i / 4f, 1f, VerdeGrade);

                Vector2 horizontal = Vector2.right * raioInterno;
                Vector2 vertical = Vector2.up * raioInterno;
                DesenharLinha(malha, centro - horizontal, centro + horizontal, 1f, VerdeGrade);
                DesenharLinha(malha, centro - vertical, centro + vertical, 1f, VerdeGrade);
                DesenharAnel(malha, centro, raio, EspessuraBorda, VerdeBorda);
                break;

            case Formato.Varredura:
                DesenharLinha(malha, centro, centro + Vector2.up * Mathf.Max(0f, raio - EspessuraBorda), 1.5f, color);
                break;

            case Formato.Jogador:
                malha.AddVert(centro + new Vector2(0f, raio), color, Vector2.zero);
                malha.AddVert(centro + new Vector2(raio, -raio), color, Vector2.zero);
                malha.AddVert(centro + new Vector2(-raio, -raio), color, Vector2.zero);
                malha.AddTriangle(0, 1, 2);
                break;

            case Formato.Inimigo:
                DesenharDisco(malha, centro, raio, 16, color);
                break;

            case Formato.Item:
                DesenharDisco(malha, centro, raio, 4, color);
                break;
        }
    }

    internal void ConfigurarComoItem()
    {
        formato = Formato.Item;
        SetVerticesDirty();
    }

    private static void DesenharDisco(VertexHelper malha, Vector2 centro, float raio, int segmentos, Color32 cor)
    {
        int indice = malha.currentVertCount;
        malha.AddVert(centro, cor, Vector2.zero);
        for (int i = 0; i < segmentos; i++)
        {
            Vector2 direcao = Direcao(i, segmentos);
            malha.AddVert(centro + direcao * raio, cor, Vector2.zero);
        }

        for (int i = 0; i < segmentos; i++)
            malha.AddTriangle(indice, indice + 1 + (i + 1) % segmentos, indice + 1 + i);
    }

    private static void DesenharAnel(VertexHelper malha, Vector2 centro, float raio, float espessura, Color32 cor)
    {
        int indice = malha.currentVertCount;
        float interno = Mathf.Max(0f, raio - espessura);
        for (int i = 0; i < Segmentos; i++)
        {
            Vector2 direcao = Direcao(i, Segmentos);
            malha.AddVert(centro + direcao * raio, cor, Vector2.zero);
            malha.AddVert(centro + direcao * interno, cor, Vector2.zero);
        }

        for (int i = 0; i < Segmentos; i++)
        {
            int atual = indice + 2 * i;
            int seguinte = indice + 2 * ((i + 1) % Segmentos);
            malha.AddTriangle(atual, atual + 1, seguinte);
            malha.AddTriangle(atual + 1, seguinte + 1, seguinte);
        }
    }

    private static void DesenharLinha(VertexHelper malha, Vector2 inicio, Vector2 fim, float largura, Color32 cor)
    {
        Vector2 direcao = (fim - inicio).normalized;
        Vector2 lado = new Vector2(-direcao.y, direcao.x) * (largura * 0.5f);
        int indice = malha.currentVertCount;
        malha.AddVert(inicio + lado, cor, Vector2.zero);
        malha.AddVert(fim + lado, cor, Vector2.zero);
        malha.AddVert(fim - lado, cor, Vector2.zero);
        malha.AddVert(inicio - lado, cor, Vector2.zero);
        malha.AddTriangle(indice, indice + 1, indice + 2);
        malha.AddTriangle(indice, indice + 2, indice + 3);
    }

    private static Vector2 Direcao(int indice, int total)
    {
        float angulo = indice * (2f * Mathf.PI / total);
        return new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
    }
}
