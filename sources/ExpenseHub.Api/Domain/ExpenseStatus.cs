namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estados possíveis de um reembolso. <see cref="Rejected"/> e <see cref="Paid"/> são finais.
/// </summary>
internal enum ExpenseStatus
{
    /// <summary>Rascunho editável pelo proprietário.</summary>
    Draft = 0,

    /// <summary>Enviado e aguardando decisão de um Approver.</summary>
    Submitted = 1,

    /// <summary>Aprovado e aguardando pagamento pelo Finance.</summary>
    Approved = 2,

    /// <summary>Reprovado com justificativa (estado final).</summary>
    Rejected = 3,

    /// <summary>Pagamento registrado (estado final).</summary>
    Paid = 4,
}
