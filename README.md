# BTRefSensSearch（蓝牙灵敏度探底自动化测试工具）

基于 **WPF / .NET 10** 的桌面工具，通过 SCPI 远程控制 **Rohde & Schwarz CMW500** 综测仪，对 **经典蓝牙（BR/EDR）** 与 **低功耗蓝牙（BLE）** 设备执行接收灵敏度（Sensitivity）自动探底测试。

工具以「逐级逼近 + 三次确认」的搜索算法，自动找到误码率 / 丢包率恰好达到门限时的最小 RF 电平，并将各封包类型、各信道的结果实时呈现在表格中。

---

## 功能特性

- **双测试模式**
  - **Classic Bluetooth（BR/EDR）**：DH5、2-DH5、3-DH5，测量 **BER**（误码率）。
  - **BLE（LE）**：LE 1M、LE 2M，测量 **PER**（丢包率）。
- **三步探底算法**：粗探（−1 dB）→ 细搜（+0.1 dB）→ 三次确认，兼顾速度与准确度。
- **多信道并列测试**：每种封包类型自动测试 3 个信道，结果以表格实时展示。
- **实时状态与日志**：连接状态指示灯、当前阶段提示、带时间戳的滚动日志。
- **测试控制**：支持 开始 / 暂停 / 继续 / 停止，支持随时取消。
- **线损补偿**：可设置 Cable Loss，自动写入仪器的外部衰减（输入 / 输出）。
- **可编辑起测电平**：每行封包类型的起测 RF 电平可在表格中手动修改。
- **零依赖通信**：直接使用原生 TCP Socket（端口 5025）收发 SCPI 指令，不依赖 VISA 运行库。
- **深色主题界面**：自定义暗色 WPF 主题。
- **自包含单文件发布**：通过 GitHub Actions 打 tag 自动构建并发布免安装 EXE。

---

## 界面结构

```
┌────────────────────────────────────────────────────────┐
│ ⚡ CMW500 Connection   VISA 地址 · Connect · 状态指示灯    │
├────────────────────────────────────────────────────────┤
│ ⚙ Test Configuration  测试模式 · Cable Loss · 阶段 · 控制  │
├────────────────────────────────────────────────────────┤
│ 📋 Result   Type · Threshold · Start · Ch1 · Ch2 · Ch3   │
├────────────────────────────────────────────────────────┤
│ 📝 Log      带时间戳的运行日志（自动滚动到底部）           │
└────────────────────────────────────────────────────────┘
```

---

## 探底算法说明

对每个「封包类型 × 信道」组合，`RunSensitivitySearch` 按以下三步搜索灵敏度点（见 `src/ViewModels/MainViewModel.cs`）：

| 步骤 | 名称 | 步进 | 结束条件 |
| --- | --- | --- | --- |
| Step 1 | 粗探 Probe | 每次 **−1.0 dB** | 误码率 / 丢包率 **≥ 门限** 时停止 |
| Step 2 | 细搜 Search | 每次 **+0.1 dB** | 误码率 / 丢包率 **< 门限** 时停止 |
| Step 3 | 三次确认 Confirm | 同一电平重复测 3 次 | 3 次全部 **< 门限** 则判定为灵敏度值；任一次失败则 **+0.1 dB** 回到确认流程 |

> 结果表中每格显示的即为该组合最终确认通过的 RF 电平（dBm）。

### 封包类型与默认参数

| 模式 | 封包类型 | 门限 | 封包数 | Payload | 默认起测电平 |
| --- | --- | --- | --- | --- | --- |
| Classic BT | DH5 | BER < 0.1% | 1000 | 339 B | −93 dBm |
| Classic BT | 2-DH5 (EDR) | BER < 0.01% | 1000 | 679 B | −90 dBm |
| Classic BT | 3-DH5 (EDR) | BER < 0.01% | 1000 | 1021 B | −85 dBm |
| BLE | LE 1M | PER < 30.8% | 3000 | 37 B | −95 dBm |
| BLE | LE 2M | PER < 30.8% | 3000 | 37 B | −90 dBm |

### 测试信道

| 模式 | Ch1 | Ch2 | Ch3 |
| --- | --- | --- | --- |
| Classic BT | RX0 / TX78 | RX39 / TX0 | RX78 / TX0 |
| BLE | Ch 0 | Ch 19 | Ch 39 |

---

## 环境要求

- **操作系统**：Windows 10 / 11
- **运行时**：.NET 10 Desktop Runtime
  （若使用 Release 页发布的自包含单文件 EXE，则无需单独安装运行时）
- **开发环境**：Visual Studio 2022（或更高）/ .NET 10 SDK，需 WPF 工作负载
- **测试仪器**：Rohde & Schwarz CMW500，已配置好与本机互通的网络（SCPI 原始 Socket，端口 **5025**）

---

## 快速开始

### 直接运行（推荐）

1. 前往仓库的 **Releases** 页面，下载最新的 `BTSensitivitySearchV*.exe`（自包含单文件，免安装）。
2. 双击运行。

### 从源码构建

```bash
# 还原并构建
dotnet build src/BTSensitivitySearchV1.0.csproj -c Release

# 运行
dotnet run --project src/BTSensitivitySearchV1.0.csproj

# 发布为自包含单文件 EXE
dotnet publish src/BTSensitivitySearchV1.0.csproj \
  -c Release -r win-x86 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

也可直接用 Visual Studio 打开 `src/BTSensitivitySearchV1.0.slnx` 进行构建 / 调试。

---

## 使用步骤

1. **连接仪器**：在 *CMW500 Connection* 区选择或输入 VISA 地址（如 `TCPIP0::192.0.2.10::INSTR`），点击 **Connect**，状态灯变绿即连接成功。
2. **配置测试**：选择 *Test Mode*（Classic Bluetooth / BLE），填写 *Cable Loss*（线损，dB）。
3. **（可选）调整起测电平**：在结果表 *Start* 列修改各封包类型的起测 RF 电平。
4. **开始测试**：点击 **Start**，工具将逐一测试每种封包类型的各个信道，结果实时填入表格，运行细节输出到日志区。
5. **过程控制**：可随时 **Pause / Resume / Stop**。
6. **查看结果**：表格中每格显示对应信道的灵敏度值（dBm）。

---

## 项目结构

```
BTSensitivitySearch/
├── .github/
│   └── workflows/
│       └── release.yml          # 打 tag 自动构建单文件 EXE 并发布到 Releases
├── docs/
│   └── 蓝牙探底算法.xlsx          # 探底算法参考资料
├── src/
│   ├── App.xaml / App.xaml.cs   # 应用入口与全局暗色主题资源
│   ├── MainWindow.xaml(.cs)     # 主窗口界面与日志自动滚动
│   ├── AssemblyInfo.cs
│   ├── BTSensitivitySearchV1.0.csproj   # 含编译期注入 BuildTime 的自定义 Target
│   ├── BTSensitivitySearchV1.0.slnx
│   ├── Models/
│   │   ├── PacketConfig.cs       # 封包类型、信道、门限等测试参数定义
│   │   └── TestResult.cs         # 测量结果记录类型
│   ├── Services/
│   │   └── CmwController.cs      # CMW500 SCPI 通信与测量逻辑（TCP:5025）
│   ├── ViewModels/
│   │   ├── MainViewModel.cs      # 主逻辑：探底算法、命令、结果表
│   │   ├── ViewModelBase.cs      # INotifyPropertyChanged 基类
│   │   └── RelayCommand.cs       # ICommand 实现
│   └── Properties/PublishProfiles/
│       └── FolderProfile.pubxml  # 本地文件夹发布配置
└── README.md
```

---

## 技术架构

- **框架**：WPF（.NET 10，`net10.0-windows`），启用 Nullable 与 ImplicitUsings。
- **架构模式**：MVVM
  - `Models`：纯数据 / 配置（`PacketConfig`、`ChannelPair`、`BerMeasurement` 等）。
  - `ViewModels`：`MainViewModel` 承载全部交互逻辑与探底算法；`ResultRow` / `ResultCell` 驱动结果表格双向绑定与实时刷新。
  - `Views`：`MainWindow.xaml`，通过 `DataTrigger` 实现状态灯、测试中高亮、暂停 / 继续按钮切换等。
- **仪器通信**（`CmwController`）：
  - 基于 `TcpClient` 直连仪器 5025 端口，ASCII 编码收发 SCPI 指令，`*OPC?` 做同步。
  - 从 VISA 地址（`TCPIP0::<ip>::INSTR`）解析出 IP。
  - 经典蓝牙使用 `...RXQ:BER` 系列指令测 BER，BLE 使用 `...RXQ:PER` 系列指令测 PER，上层通过 `ConfigurePacket` / `SetChannel` / `MeasureErrorRate` 统一分发。
- **并发**：测试在 `Task.Run` 后台线程执行，通过 `Dispatcher.Invoke` 回到 UI 线程刷新；`CancellationTokenSource` 负责停止，`ManualResetEventSlim` 实现暂停 / 继续。
- **无第三方依赖**：仅使用 .NET / WPF 内置能力，无 NuGet 包。

---

## 持续集成 / 发布

`.github/workflows/release.yml` 为通用发布流程：

- **触发**：推送以 `v` / `V` 开头的 tag（如 `v1.0`），或在 Actions 页面手动触发。
- **行为**：自动定位唯一的 `.csproj` → 发布 `win-x64` 自包含单文件 EXE → 按 tag 版本号重命名（如 `BTSensitivitySearchV1.0.exe`）→ 创建 GitHub Release 并附带产物。
- 手动触发时仅上传构建产物（Artifact），不创建 Release。

发布示例：

```bash
git tag v1.0
git push origin v1.0
```

---

## 备注

- 程序内置了一个基于编译时间的使用有效期校验（`BuildInfo.BuildTimeUtc` 在编译期由 csproj 的 `GenerateBuildTime` Target 注入）；超期后将无法连接仪器。如需长期使用，请从源码重新构建。
- 默认 VISA 地址、Cable Loss、门限与信道等参数均可在 `src/Models/PacketConfig.cs` 与 `src/ViewModels/MainViewModel.cs` 中调整。
