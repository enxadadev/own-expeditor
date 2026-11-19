using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

public interface IGameNames
{
    /// <summary>Nome legível do id; nunca null (cai em <see cref="GameIds.Humanize"/>).</summary>
    string Get(GameNameKind kind, string id);

    /// <summary>Reboque decomposto (dono, cadeia, eixos, comprimento) para o App montar o nome; não depende do save.</summary>
    TrailerDescriptor Trailer(string? definition, string? variant) => TrailerDescriptor.Parse(definition, variant);
}
