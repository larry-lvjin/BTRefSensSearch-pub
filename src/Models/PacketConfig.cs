namespace BTSensitivitySearchV1._0.Models;

public enum TestMode
{
    ClassicBT,
    BLE
}

public enum PacketType
{
    DH5,
    EDR_2DH5,
    EDR_3DH5,
    BLE_1M,
    BLE_2M
}

public record ChannelPair(int RxChannel, int TxChannel, string DisplayName);

public class PacketConfig
{
    public TestMode TestMode { get; }
    public PacketType Type { get; }
    public string DisplayName { get; }
    public int PacketCount { get; }
    public double ErrorRateThresholdPercent { get; }
    public int PayloadBytes { get; }
    public string PhySuffix { get; }
    public double DefaultStartLevel { get; }

    PacketConfig(TestMode mode, PacketType type, string displayName,
        int packetCount, double threshold, int payloadBytes,
        double defaultStartLevel, string phySuffix = "")
    {
        TestMode = mode;
        Type = type;
        DisplayName = displayName;
        PacketCount = packetCount;
        ErrorRateThresholdPercent = threshold;
        PayloadBytes = payloadBytes;
        DefaultStartLevel = defaultStartLevel;
        PhySuffix = phySuffix;
    }

    public static PacketConfig DH5 { get; } = new(
        TestMode.ClassicBT, PacketType.DH5, "DH5", 1000, 0.1, 339, -93);

    public static PacketConfig EDR2DH5 { get; } = new(
        TestMode.ClassicBT, PacketType.EDR_2DH5, "2-DH5", 1000, 0.01, 679, -90);

    public static PacketConfig EDR3DH5 { get; } = new(
        TestMode.ClassicBT, PacketType.EDR_3DH5, "3-DH5", 1000, 0.01, 1021, -85);

    public static PacketConfig BLE1M { get; } = new(
        TestMode.BLE, PacketType.BLE_1M, "1M", 3000, 30.8, 37, -95, "LE1M");

    public static PacketConfig BLE2M { get; } = new(
        TestMode.BLE, PacketType.BLE_2M, "2M", 3000, 30.8, 37, -90, "LE2M");

    public static PacketConfig[] ClassicBTConfigs { get; } = [DH5, EDR2DH5, EDR3DH5];
    public static PacketConfig[] BLEConfigs { get; } = [BLE1M, BLE2M];

    public static ChannelPair[] ClassicBTChannels { get; } =
    [
        new(0, 78, "RX0/TX78"),
        new(39, 0, "RX39/TX0"),
        new(78, 0, "RX78/TX0")
    ];

    public static ChannelPair[] BLEChannels { get; } =
    [
        new(0, 0, "Ch 0"),
        new(19, 19, "Ch 19"),
        new(39, 39, "Ch 39")
    ];

    public override string ToString() => DisplayName;
}
