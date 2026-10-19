namespace BTSensitivitySearchV1._0.Models;

public record BerMeasurement(double ErrorRatePercent, double RfLevelDbm, int PacketCount);

public record SensitivityResult(
    PacketType PacketType,
    int Channel,
    double SensitivityDbm,
    double FinalErrorRatePercent,
    int TotalMeasurements,
    DateTime Timestamp);
