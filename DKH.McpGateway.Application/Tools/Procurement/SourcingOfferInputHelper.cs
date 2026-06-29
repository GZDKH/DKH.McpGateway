using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using Google.Protobuf.Collections;

namespace DKH.McpGateway.Application.Tools.Procurement;

/// <summary>
/// Shared JSON parsing for sourcing offer collection inputs (price breaks, cost components, participants, certificates).
/// Used by both CreateSourcingOfferTool and UpdateSourcingOfferTool.
/// </summary>
internal static class SourcingOfferInputHelper
{
    /// <summary>
    /// Parses and populates the four collection fields on a sourcing offer request.
    /// Returns null on success, or an error message string on parse failure.
    /// </summary>
    internal static string? PopulateCollections(
        RepeatedField<SourcingOfferPriceBreakInput> priceBreaksField,
        RepeatedField<SourcingOfferCostComponentInput> costComponentsField,
        RepeatedField<SourcingOfferParticipantInput> participantsField,
        RepeatedField<SourcingOfferCertificateInput> certificatesField,
        string? priceBreaks,
        string? costComponents,
        string? participants,
        string? certificates)
    {
        if (!string.IsNullOrWhiteSpace(priceBreaks))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(priceBreaks, McpJsonDefaults.Options);
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        var minQty = item.GetProperty("minQuantity").GetInt32();
                        var amt = item.GetProperty("unitPriceAmount").GetDecimal();
                        var cur = item.GetProperty("unitPriceCurrency").GetString() ?? string.Empty;
                        priceBreaksField.Add(new SourcingOfferPriceBreakInput
                        {
                            MinQuantity = minQty,
                            UnitPriceAmount = new DecimalValue(amt),
                            Currency = cur,
                        });
                    }
                }
            }
            catch (Exception)
            {
                return "priceBreaks JSON is invalid";
            }
        }

        if (!string.IsNullOrWhiteSpace(costComponents))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(costComponents, McpJsonDefaults.Options);
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        var kind = Enum.Parse<SourcingOfferCostKind>(
                            item.GetProperty("kind").GetString() ?? string.Empty, ignoreCase: true);
                        var amt = item.GetProperty("amount").GetDecimal();
                        var cur = item.GetProperty("currency").GetString() ?? string.Empty;
                        costComponentsField.Add(new SourcingOfferCostComponentInput
                        {
                            Kind = kind,
                            Amount = new DecimalValue(amt),
                            Currency = cur,
                        });
                    }
                }
            }
            catch (Exception)
            {
                return "costComponents JSON is invalid";
            }
        }

        if (!string.IsNullOrWhiteSpace(participants))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(participants, McpJsonDefaults.Options);
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        var cpId = item.GetProperty("counterpartyId").GetString() ?? string.Empty;
                        var role = Enum.Parse<SourcingParticipantRole>(
                            item.GetProperty("role").GetString() ?? string.Empty, ignoreCase: true);
                        participantsField.Add(new SourcingOfferParticipantInput
                        {
                            CounterpartyId = new GuidValue(cpId),
                            Role = role,
                        });
                    }
                }
            }
            catch (Exception)
            {
                return "participants JSON is invalid";
            }
        }

        if (!string.IsNullOrWhiteSpace(certificates))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(certificates, McpJsonDefaults.Options);
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        var certType = item.GetProperty("certificateType").GetString() ?? string.Empty;
                        var number = item.GetProperty("number").GetString() ?? string.Empty;
                        var issuerId = item.GetProperty("issuerCounterpartyId").GetString() ?? string.Empty;
                        var cert = new SourcingOfferCertificateInput
                        {
                            CertificateType = certType,
                            Number = number,
                            IssuerCounterpartyId = new GuidValue(issuerId),
                        };

                        if (item.TryGetProperty("validFrom", out var vf) && vf.ValueKind != JsonValueKind.Null)
                        {
                            cert.ValidFrom = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                                DateTime.Parse(vf.GetString()!, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
                        }

                        if (item.TryGetProperty("validTo", out var vt) && vt.ValueKind != JsonValueKind.Null)
                        {
                            cert.ValidTo = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                                DateTime.Parse(vt.GetString()!, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime());
                        }

                        if (item.TryGetProperty("documentRef", out var dr) && dr.ValueKind != JsonValueKind.Null)
                        {
                            cert.DocumentRef = new GuidValue(dr.GetString()!);
                        }

                        certificatesField.Add(cert);
                    }
                }
            }
            catch (Exception)
            {
                return "certificates JSON is invalid";
            }
        }

        return null;
    }
}
