using SiPacul.Domain.Common.Events;

namespace SiPacul.Domain.Events.MasterData;

/// <summary>
/// Domain event yang terjadi ketika komoditas baru berhasil dibuat.
/// </summary>
public sealed record CommodityCreatedDomainEvent(
    Guid CommodityId,
    string CommodityName,
    Guid CommodityCategoryId)
    : DomainEvent;
