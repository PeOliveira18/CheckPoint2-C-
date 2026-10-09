namespace ExpenseHub.Api.Domain;

/// <summary>
/// Ações registradas no histórico de um reembolso.
/// </summary>
internal enum ExpenseAction
{
    /// <summary>Criação do rascunho.</summary>
    Created = 0,

    /// <summary>Alteração de campos enquanto em Draft.</summary>
    Updated = 1,

    /// <summary>Envio do rascunho para aprovação.</summary>
    Submitted = 2,

    /// <summary>Aprovação por um Approver.</summary>
    Approved = 3,

    /// <summary>Reprovação por um Approver, com justificativa.</summary>
    Rejected = 4,

    /// <summary>Registro do pagamento pelo Finance.</summary>
    Paid = 5,
}
