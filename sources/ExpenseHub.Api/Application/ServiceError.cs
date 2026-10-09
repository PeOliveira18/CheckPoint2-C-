namespace ExpenseHub.Api.Application;

/// <summary>
/// Categorias de falha de uma operação de serviço, mapeadas para status HTTP.
/// </summary>
internal enum ServiceError
{
    /// <summary>Entrada inválida (400).</summary>
    Validation,

    /// <summary>Recurso inexistente ou fora do escopo de leitura (404).</summary>
    NotFound,

    /// <summary>Autenticado, mas sem permissão para a operação neste recurso (403).</summary>
    Forbidden,

    /// <summary>Operação incompatível com o estado atual (409).</summary>
    Conflict,
}
