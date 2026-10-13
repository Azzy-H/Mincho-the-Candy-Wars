# StellarisEvent 事件系统 —— 上游同步说明

## 上游与副本

| | 位置 |
|---|---|
| **上游（source of truth）** | `E:\SteamLibrary\steamapps\common\RimWorld\Mods\ProdrakeRace\Source\PDR\StellarisEvent` |
| **本仓库副本** | `Source\MinchoCandyWars\StellarisEvent` |
| **同步脚本** | `Source\MinchoCandyWars\Sync-StellarisEvent.ps1` |

`StellarisEvent` 目录是本仓库中**唯一一处从 ProdrakeRace 整体移植**的子系统，目录内容始终是上游的纯净镜像。
同步脚本放在目录之外，因此该目录里不会混入本仓库独有的文件。

## 副本与上游之间的全部差异

移植时脚本只施加下面 5 项变换，除此之外逐字一致：

1. 换行统一为 `LF`（上游用 CRLF）
2. UTF-8 无 BOM
3. 命名空间 `PDR` → `MinchoCandyWars.StellarisEvent`
   （含 XML 文档示例里的 `Class="PDR.Xxx"` 与 `typeof(PDR.StellarisEventDef)`）
4. 调试菜单分类 `"PDROD"` → `"Stellaris Events"`
5. 每个 `.cs` 首行加 `#nullable disable`

第 5 项的原因：本工程启用了 `<Nullable>enable</Nullable>`，而上游与 ArachnaeSwarm 的副本都没有启用。
用该指令让这批移植文件保持与上游逐字一致，**以后重新移植不需要再逐处补可空注解**
（此前正是因为要人工补 100 多处注解，导致比对和重新移植代价很高）。

## 重新移植

```powershell
cd Source\MinchoCandyWars

# 1) 先校验当前副本是否已经落后于上游（只读，不改文件）
powershell -ExecutionPolicy Bypass -File .\Sync-StellarisEvent.ps1 -Verify

# 2) 执行重新移植，并编译验证（要求 0 错误 0 警告）
powershell -ExecutionPolicy Bypass -File .\Sync-StellarisEvent.ps1 -Build
```

`-Verify` 会逐个文件报告是否等于「上游 + 上述变换」；脚本也会提示目标目录里
存在、但上游已经没有对应文件的残留项，便于确认是否需要删除。

> 脚本含中文，必须保存为 **UTF-8 with BOM**，否则 Windows PowerShell 5.1 会按 ANSI 解析而报语法错误。

## 本次重新移植带来的变化

上游在这一版扩充较多，本仓库此前停在更早的骨架版本。本次移植新增 10 个文件：

| 新增文件 | 作用 |
|---|---|
| `OnActionDef.cs` | `OnActionDef` 信号定义：按 `triggerList` 顺序触发 + `randomTriggerList` 加权随机抽一个 |
| `OnActionDebugConsole.cs` | onAction 调试控制台 |
| `StellarisSuperEventWindow.cs` | 超事件窗口布局（立绘顶部全宽、内容面板居中） |
| `StellarisToastWindow.cs` | 吐司窗口（无选项、5 秒真实时间倒计时、带队列） |
| `Effect_AddIdeoDevelopmentPoints.cs` | 增加文化发展点（需 Ideology） |
| `Effect_UnlockResearch.cs` | 解锁科技 |
| `Trigger_DayPeriod.cs` | 按日/月/年周期判定 |
| `Trigger_FlagExist.cs` | 判定 flag 是否存在 |
| `Trigger_IdeoCanReform.cs` | 判定文化可否改革（需 Ideology） |
| `Trigger_ResearchComplete.cs` | 判定科技是否已完成 |

已有 21 个文件同步到上游当前版本，主要增强：

- `StellarisEventManager`：周期触发器（`On_Per_Day` / `On_Per_Month` / `On_Per_Year` / `On_Per_Day_Anytime` / `On_Game_Start`）、
  吐司队列与存读档恢复、onAction 信号调度与触发计数
- `StellarisEventFlagManager`：延时触发（`ScheduleDelayedFire`）、事件窗口存读档恢复、吐司与周期触发器的存档字段
- `StellarisEventDef`：`isSuperEvent` / `isToastEvent` / `forceChoose`、`triggers` 列表与 `idGroup` 分组逻辑（AND / OR / NOT）、
  本地化参数 `{key}` 注入、描述文本 `key.1 / key.2` 随机变体
- `Effect_SetFlag`：`Create / Override / Add / Subtract / Multiply / Divide` 模式、`local` 标记
- `Effect_DropSupply` / `Effect_FactionRaid` / `Effect_SpawnPawnFromFlag`：新增 Pawn 空投、材料、投放位置、袭击策略等字段

## 兼容性

现网 XML（如 `1.6\Defs\QuestScriptDefs\MCW_Intro_Spy.xml` 中的 `MCW_Event_QS1_HuntedMincho`）
**无需改动**：`title` / `desc` / `backgroundPath` / `pictures.path` / `immediate` / `options` / `after`、
`Effect_DropSupply` 的 `thingDef` + `num`、`Effect_SetFlag` 的 `flagName` / `valueType` / `boolValue` / `ticks` / `local`
在移植后字段名全部保持不变。

新增能力（超事件、吐司、onAction、周期触发、新触发器与效果）都是**可选**的，只有 XML 显式使用才会生效。

`StellarisEventFlagManager` 是 `GameComponent`，RimWorld 会自动实例化所有非抽象 `GameComponent` 子类
（`Verse.Game.FillComponents`），因此**不需要**在 XML 里注册 `<gameComponents>`——上游同样没有注册。

## 其他副本

同一套框架在别处还有副本，命名空间各不相同，互不冲突：

| 副本 | 命名空间 | 可空注解 |
|---|---|---|
| ProdrakeRace | `PDR` | 无 |
| ArachnaeSwarm（ARAExtensionMission） | `ArachnaeSwarm.Event` | 无 |
| 本仓库 | `MinchoCandyWars.StellarisEvent` | `#nullable disable` |
