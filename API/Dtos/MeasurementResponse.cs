namespace InvernApp.Api.Dtos;

/// <summary>
/// Representación pública de una medición almacenada.
/// </summary>
public record MeasurementResponse(
    long Id,
    string DeviceId,
    long SequenceNumber,
    DateTime Timestamp,
    DateTime ReceivedAt,
    double Temperature,
    double AirHumidity,
    double? SoilHumidity,
    bool SoilHumiditySimulated,
    double Luminosity);