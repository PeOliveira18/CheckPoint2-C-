using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using ExpenseHub.Core.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I03 — contrato da mensagem e propriedades de publicação.
/// </summary>
[TestClass]
public sealed class ReportMessageContractTests
{
    private static readonly Guid _reportId = Guid.Parse("6f1c2a5e-3b7d-4c1a-9e2f-0a1b2c3d4e5f");

    /// <summary>A mensagem leva só o reportId, no formato {"reportId": "..."}.</summary>
    [TestMethod]
    public void MessageCarriesOnlyReportId()
    {
        using JsonDocument json = JsonDocument.Parse(new ReportMessage(_reportId).ToBytes());

        JsonElement root = json.RootElement;
        Assert.AreEqual(1, root.EnumerateObject().Count());
        Assert.AreEqual(_reportId, root.GetProperty("reportId").GetGuid());
    }

    /// <summary>O payload publicado manualmente no painel do RabbitMQ é aceito (usado no experimento E03).</summary>
    [TestMethod]
    public void ManualPayloadIsParsed()
    {
        byte[] body = Encoding.UTF8.GetBytes("{\"reportId\": \"6f1c2a5e-3b7d-4c1a-9e2f-0a1b2c3d4e5f\"}");

        Assert.AreEqual(_reportId, ReportMessage.TryParse(body)?.ReportId);
    }

    /// <summary>A mensagem é persistente, em JSON e identificada pelo reportId.</summary>
    [TestMethod]
    public void PublishPropertiesArePersistentAndCorrelated()
    {
        BasicProperties properties = ReportQueue.CreateProperties(_reportId);

        Assert.IsTrue(properties.Persistent);
        Assert.AreEqual(DeliveryModes.Persistent, properties.DeliveryMode);
        Assert.AreEqual("application/json", properties.ContentType);
        Assert.AreEqual(_reportId.ToString(), properties.MessageId);
        Assert.AreEqual(_reportId.ToString(), properties.CorrelationId);
    }

    /// <summary>A fila padrão é a combinada com o Compose.</summary>
    [TestMethod]
    public void DefaultQueueNameMatchesComposeContract()
    {
        Assert.AreEqual("expensehub.reports", new RabbitMqOptions().QueueName);
    }
}
