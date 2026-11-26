using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public sealed class ItemColetavel : MonoBehaviour
{
    public enum TipoItem { Coracao, Caveira }

    [SerializeField] private TipoItem tipo;
    [SerializeField] private Transform visual;
    private SistemaDrops sistema;
    private SpawnOndas ondas;
    private bool coletado;

    public TipoItem Tipo => tipo;
    public bool Disponivel => !coletado && isActiveAndEnabled;

    internal void Configurar(SistemaDrops origem, SpawnOndas spawner)
    {
        sistema = origem;
        ondas = spawner;
    }

    private void Update()
    {
        if (visual == null)
            return;

        visual.localPosition = Vector3.up * (0.65f + Mathf.Sin(Time.time * 3f) * 0.16f);
        visual.Rotate(0f, 85f * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider outro) => TentarColetar(outro);
    private void OnTriggerStay(Collider outro) => TentarColetar(outro);

    private void TentarColetar(Collider outro)
    {
        if (coletado || sistema == null || ondas == null ||
            outro.GetComponentInParent<VidaJogador>() is not { VidaAtual: > 0 } jogador ||
            !ondas.EhJogador(jogador))
            return;

        coletado = true;
        bool sucesso = tipo == TipoItem.Coracao ? jogador.RestaurarVida() : ondas.TentarAtivarCaveira();
        if (!sucesso)
        {
            coletado = false;
            return;
        }

        sistema.RegistrarColeta(this, ondas.CaveirasSkull);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
