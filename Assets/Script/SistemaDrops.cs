using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class SistemaDrops : MonoBehaviour
{
    [SerializeField] private ItemColetavel coracao;
    [SerializeField] private ItemColetavel caveira;
    [SerializeField, Range(0f, 1f)] private float chanceDeDrop = 0.25f;
    [SerializeField, Range(0f, 1f)] private float chanceDeCoracao = 0.25f;

    private readonly List<ItemColetavel> ativos = new();
    private IReadOnlyList<ItemColetavel> leituraAtivos;
    private float mensagemAte;
    private string mensagem;

    public IReadOnlyList<ItemColetavel> Ativos => leituraAtivos ??= ativos.AsReadOnly();
    public string Mensagem => Time.time < mensagemAte ? mensagem : string.Empty;

    internal void SortearDrop(Vector3 posicao, SpawnOndas ondas)
    {
        if (Random.value >= chanceDeDrop)
            return;

        ItemColetavel prefab = Random.value < chanceDeCoracao ? coracao : caveira;
        if (prefab == null)
            return;

        if (NavMesh.SamplePosition(posicao, out NavMeshHit piso, 1.5f, NavMesh.AllAreas))
            posicao = piso.position;

        ItemColetavel item = Instantiate(prefab, posicao, Quaternion.identity, transform);
        item.Configurar(this, ondas);
        ativos.Add(item);
    }

    internal void RegistrarColeta(ItemColetavel item, int caveirasSkull)
    {
        ativos.Remove(item);
        if (item.Tipo == ItemColetavel.TipoItem.Coracao)
            Notificar("VIDA RESTAURADA");
        else
            Notificar(caveirasSkull > 1 ? "SKULL WAVE ESTENDIDA" : "SKULL WAVE ATIVADA");
    }

    internal void Notificar(string texto)
    {
        mensagem = texto;
        mensagemAte = Time.time + 3f;
    }
}
