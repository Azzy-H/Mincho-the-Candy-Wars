# StellarisEvent — Trigger & Effect 参考文档

> 命名空间: `MinchoCandyWars.StellarisEvent` | 适用 RimWorld 1.6

---

## 目录

- [1. 概述](#1-概述)
- [2. Trigger（触发器）](#2-trigger触发器)
  - [2.1 基类：StellarisEventTrigger](#21-基类stellarseventtrigger)
  - [2.2 TriggerLogic 枚举](#22-triggerlogic-枚举)
  - [2.3 idGroup 分组判定规则](#23-idgroup-分组判定规则)
  - [2.4 Triggers_FactionExists](#24-triggers_factionexists)
  - [2.5 Trigger_FlagCheck](#25-trigger_flagcheck)
  - [2.6 FloatCompareMode 枚举](#26-floatcomparemode-枚举)
- [3. Effect（效果）](#3-effect效果)
  - [3.1 基类：StellarisEventEffect](#31-基类stellarseventeffect)
  - [3.2 StellarisEventEffectList（容器）](#32-stellarseventeffectlist容器)
  - [3.3 Effect_SetFlag](#33-effect_setflag)
  - [3.4 FlagSetMode 枚举](#34-flagsetmode-枚举)
  - [3.5 Effect_FetchFlagAsParam](#35-effect_fetchflagasparam)
  - [3.6 本地化参数系统](#36-本地化参数系统)
  - [3.7 Effect_FireEvent](#37-effect_fireevent)
  - [3.8 Effect_DropSupply](#38-effect_dropsupply)
  - [3.9 Effect_FireIncident](#39-effect_fireincident)
- [4. 辅助类型速查](#4-辅助类型速查)
  - [4.1 FlagValueType](#41-flagvaluetype)
  - [4.2 Flag 系统 API](#42-flag-系统-api)
- [5. 完整 XML 示例](#5-完整-xml-示例)

---

## 1. 概述

StellarisEvent 框架有三个核心概念：

| 概念 | 角色 | 存放位置 |
|------|------|----------|
| **Trigger** | 判定条件（能否触发 / 选项是否可用 / 立绘是否可选） | `<triggers>` / `option.trigger` / `picture.trigger` |
| **Effect** | 执行效果（修改游戏状态、抓取参数） | `immediate` / `option.action` / `after` |
| **Flag** | 跨事件的状态记录（内存 + 存档持久化） | 由 `StellarisEventFlagManager` 管理 |
| **LocalParam** | 本地化参数注入（`{key}` → 运行时值） | `Effect_FetchFlagAsParam` 填充，窗口渲染时 `InjectParams()` 替换 |

三者协作流程：

```
事件触发:
  triggers 全部通过 → immediate 执行 → 弹窗 → 玩家选 option
  → option.action 执行 → after 执行 → 结束
```

---

## 2. Trigger（触发器）

### 2.1 基类：StellarisEventTrigger

所有 trigger 继承自此基类。

```csharp
public class StellarisEventTrigger
{
    public string idGroup;           // 分组标识，为空 = 独立判定
    public TriggerLogic logic = AND; // 逻辑运算符（idGroup 非空时生效）

    public virtual bool CanTrigger() => true; // 子类覆写
}
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `idGroup` | `string` | `null` | 分组标识。为空时该 trigger 独立判定（必须通过）。非空时与同事件内相同 idGroup 的 trigger 一起按分组逻辑判定 |
| `logic` | `TriggerLogic` | `AND` | 逻辑运算符。仅在 `idGroup` 非空时生效 |

---

### 2.2 TriggerLogic 枚举

```csharp
public enum TriggerLogic { AND, OR, NOT }
```

| 值 | 行为 |
|----|------|
| `AND` | 原始结果必须为 `true`。默认值 |
| `OR` | 同组内任一 OR 为 `true` 则**所有** OR 均视为 `true` |
| `NOT` | 翻转该 trigger 的原始输出（`true` → `false`，`false` → `true`） |

---

### 2.3 idGroup 分组判定规则

当多个 trigger 设置了相同的 `idGroup` 时，分组判定过程如下：

```
Step 1 — 收集所有同组 trigger 的原始 CanTrigger() 结果
Step 2 — NOT 翻转：logic=NOT 的项结果取反
Step 3 — OR  提升：若有任一 OR 的原始结果为 true，则该组内 ALL OR = true
Step 4 — 全真判定：所有结果的 NOT/OR/AND 必须全部为 true，idGroup 才算通过
```

**示例 —— "任意敌人存在"：**
```xml
<!-- Pirate 或 Tribal 任一存在 → 该组视为通过 -->
<li Class="Triggers_FactionExists">
  <factionDef>Pirate</factionDef>
  <idGroup>enemyCheck</idGroup>
  <logic>OR</logic>
</li>
<li Class="Triggers_FactionExists">
  <factionDef>Tribal</factionDef>
  <idGroup>enemyCheck</idGroup>
  <logic>OR</logic>
</li>
```

---

### 2.4 Triggers_FactionExists

**全限定类名：** `MinchoCandyWars.StellarisEvent.Triggers_FactionExists`

判定指定 `FactionDef` 对应的派系是否已在世界中生成。

| 字段 | 类型 | 说明 |
|------|------|------|
| `factionDef` | `FactionDef` | 目标派系定义（XML 中用 defName 引用） |
| （继承）`idGroup` | `string` | 分组标识 |
| （继承）`logic` | `TriggerLogic` | 逻辑运算符 |

**逻辑：** `Find.FactionManager.FirstFactionOfDef(factionDef) != null`

**XML 示例：**
```xml
<!-- 独立判定 -->
<li Class="MinchoCandyWars.StellarisEvent.Triggers_FactionExists">
  <factionDef>ARA_MinchoCandyWars</factionDef>
</li>

<!-- 分组判定 -->
<li Class="MinchoCandyWars.StellarisEvent.Triggers_FactionExists">
  <factionDef>Outlander</factionDef>
  <idGroup>anyFriendly</idGroup>
  <logic>OR</logic>
</li>
```

---

### 2.5 Trigger_FlagCheck

**全限定类名：** `MinchoCandyWars.StellarisEvent.Trigger_FlagCheck`

判定指定 flag 是否存在且类型与值均匹配。未找到、类型不同、值不匹配均返回 `false`。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `flagName` | `string` | `null` | 要检查的 flag 名 |
| `checkType` | `FlagValueType` | `Bool` | 期望的数据类型 |
| `floatCompare` | `FloatCompareMode` | `Equal` | Float 比较模式（仅 `checkType=Float` 时生效） |
| `expectedFloat` | `float` | `0` | 期望的 float 值 |
| `expectedString` | `string` | `null` | 期望的 string 值 |
| `expectedBool` | `bool` | `false` | 期望的 bool 值 |
| （继承）`idGroup` | `string` | — | 分组标识 |
| （继承）`logic` | `TriggerLogic` | — | 逻辑运算符 |

**XML 示例：**

```xml
<!-- Bool: 检查 questDone 是否为 true -->
<li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck">
  <flagName>questDone</flagName>
  <checkType>Bool</checkType>
  <expectedBool>true</expectedBool>
</li>

<!-- String: 检查 title 是否为 "Warlord" -->
<li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck">
  <flagName>title</flagName>
  <checkType>String</checkType>
  <expectedString>Warlord</expectedString>
</li>

<!-- Float: 检查 score >= 100 -->
<li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck">
  <flagName>score</flagName>
  <checkType>Float</checkType>
  <expectedFloat>100</expectedFloat>
  <floatCompare>GreaterOrEqual</floatCompare>
</li>

<!-- Float: 检查 timer < 30（即超时前） -->
<li Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck">
  <flagName>timer</flagName>
  <checkType>Float</checkType>
  <expectedFloat>30</expectedFloat>
  <floatCompare>LessThan</floatCompare>
</li>
```

---

### 2.6 FloatCompareMode 枚举

```csharp
public enum FloatCompareMode
{
    Equal,          // flag == expected
    GreaterThan,    // flag >  expected
    LessThan,       // flag <  expected
    GreaterOrEqual, // flag >= expected
    LessOrEqual     // flag <= expected
}
```

仅在 `Trigger_FlagCheck.checkType == Float` 时生效，且 `floatCompare` 默认值为 `Equal`（向后兼容）。

---

## 3. Effect（效果）

### 3.1 基类：StellarisEventEffect

```csharp
public class StellarisEventEffect
{
    public virtual void Execute() { }
}
```

所有自定义 effect 继承此类并覆写 `Execute()`。

---

### 3.2 StellarisEventEffectList（容器）

```csharp
public class StellarisEventEffectList
{
    public List<StellarisEventEffect> effects = new();

    public void Execute()
    {
        // 从上到下依次执行 effects
    }
}
```

`immediate`、`after`、`option.action` 均使用此类型。XML 中通过 `<effects>` 子元素列表配置。

---

### 3.3 Effect_SetFlag

**全限定类名：** `MinchoCandyWars.StellarisEvent.Effect_SetFlag`

设置一个 flag。支持创建、覆盖和 float 运算。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `flagName` | `string` | `null` | 目标 flag 名 |
| `valueType` | `FlagValueType` | `Bool` | 值类型 |
| `floatValue` | `float` | `0` | float 值 |
| `stringValue` | `string` | `null` | string 值 |
| `boolValue` | `bool` | `false` | bool 值 |
| `setMode` | `FlagSetMode` | `Override` | 设置模式 |
| `ticks` | `int?` | `null` | 剩余 tick（null = 永久） |

**XML 示例：**

```xml
<!-- Create: 仅在尚不存在时写入，已有则跳过 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
  <flagName>firstContact</flagName>
  <valueType>Bool</valueType>
  <boolValue>true</boolValue>
  <setMode>Create</setMode>
</li>

<!-- Override: 始终覆盖写入 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
  <flagName>title</flagName>
  <valueType>String</valueType>
  <stringValue>Warlord</stringValue>
  <setMode>Override</setMode>
</li>

<!-- Add: float 加法；不存在则从 0+value 创建 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
  <flagName>score</flagName>
  <valueType>Float</valueType>
  <floatValue>100</floatValue>
  <setMode>Add</setMode>
</li>

<!-- 带过期时间的 flag（60000 ticks ≈ 24 游戏小时） -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
  <flagName>warActive</flagName>
  <valueType>Bool</valueType>
  <boolValue>true</boolValue>
  <setMode>Override</setMode>
  <ticks>60000</ticks>
</li>
```

---

### 3.4 FlagSetMode 枚举

```csharp
public enum FlagSetMode { Create, Override, Add, Subtract, Multiply, Divide }
```

| 模式 | 行为 | 不存在时 | 存在但非 float |
|------|------|----------|----------------|
| `Create` | 仅当 flag 不存在时才写入 | 创建 | 跳过 |
| `Override` | 始终覆盖写入 | 创建 | 覆盖（类型也会变） |
| `Add` | `flag += value`（仅 float） | 以 0 为初值创建 | 跳过 |
| `Subtract` | `flag -= value`（仅 float） | 以 0 为初值创建 | 跳过 |
| `Multiply` | `flag *= value`（仅 float） | 跳过 | 跳过 |
| `Divide` | `flag /= value`（仅 float） | 跳过 | 跳过 |

> **注意：** Add / Subtract / Multiply / Divide 仅在 `valueType == Float` 时生效。若配置为非 Float 类型，这些模式将无操作。Divide 有除零保护。

---

### 3.5 Effect_FetchFlagAsParam

**全限定类名：** `MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam`

从指定 flag 抓取值，存入事件的 `localParams`，供后续窗口渲染时注入到 title/desc/option label 等文本中。
**专为 `immediate` 设计**（此时窗口尚未构建，参数可在渲染前就绪）。

若同一 `immediate` 中配置多个此 effect，按 XML 顺序依次填充参数；窗口渲染时一次性全部注入。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `flagName` | `string` | `null` | 要抓取值的 flag 名 |
| `paramKey` | `string` | `null` | 存入的参数 key。为空时默认使用 `flagName` 作为 key |

**XML 示例：**
```xml
<!-- 从 enemyName flag 抓取 string，以 key="enemy" 存入参数 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam">
  <flagName>enemyName</flagName>
  <paramKey>enemy</paramKey>
</li>

<!-- paramKey 为空则用 flagName 作为 key -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam">
  <flagName>threatLevel</flagName>
</li>
```

---

### 3.6 本地化参数系统

StellarisEvent 支持在事件文本中嵌入运行时值。工作流程：

```
XML def 中定义 Effect_FetchFlagAsParam（immediate 阶段）
  → 从指定 flag 抓取值，按 paramKey 存入 StellarisEventDef.localParams
  → 窗口渲染时，InjectParams() 将文本中 {key} 替换为对应 value
```

**占位语法：** 在 `title`、`desc`、`option.label`、`option.desc`、`option.disabledReason` 中使用 `{key}` 占位。

**层级控制：** 本地化文件中的翻译文本亦可通过 `{key}` 引用参数。若翻译文本不含某 key，该占位不会被替换，保持原样显示。

**典型用法示例：**

```xml
<!-- immediate 中先抓取参数 -->
<immediate>
  <effects>
    <li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam">
      <flagName>enemyName</flagName>
      <paramKey>enemy</paramKey>
    </li>
    <li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam">
      <flagName>threatLevel</flagName>
      <paramKey>threat</paramKey>
    </li>
  </effects>
</immediate>
```

对应的本地化 Key 可写为：

| Key | 文本 |
|-----|------|
| `ARA_Event_Title` | `{enemy} 的威胁` |
| `ARA_Event_Desc` | `虫巢探测到 {enemy} 派系的敌意信号。威胁等级：{threat}` |
| `ARA_Option_Fight` | `迎战 {enemy}` |
| `ARA_Option_Flee` | `撤离` |

若 `enemyName` flag 值为 `"海盗"`，`threatLevel` flag 值为 `42.00`，渲染结果为：

> **标题：** 海盗 的威胁
> **描述：** 虫巢探测到 海盗 派系的敌意信号。威胁等级：42.00
> **选项：** 迎战 海盗

---

### 3.7 Effect_FireEvent

**全限定类名：** `MinchoCandyWars.StellarisEvent.Effect_FireEvent`

触发另一个已注册的 `StellarisEventDef`。默认走目标事件的 trigger 判定（不通过则不触发）。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `defName` | `string` | `null` | 目标事件的 defName |
| `forceFire` | `bool` | `false` | 是否跳过目标事件的 trigger 判定，强制触发 |

**典型用法 —— 事件链：**
```xml
<!-- 完成当前事件后自动打开下一个事件 -->
<after>
  <effects>
    <li Class="MinchoCandyWars.StellarisEvent.Effect_FireEvent">
      <defName>ARA_WarAftermath</defName>
    </li>
  </effects>
</after>
```

**注意：** 若目标事件同时可被正常触发，需注意避免循环触发（B → A → B ...）。建议用 flag 配合 `Trigger_FlagCheck` 做互斥保护。

---

### 3.8 Effect_DropSupply

**全限定类名：** `MinchoCandyWars.StellarisEvent.Effect_DropSupply`

向玩家殖民地空投物资或单位。空投落点自动选取安全位置，附带空投舱动画。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `thingDef` | `ThingDef` | `null` | 空投物品定义（若 `pawnKindDef` 非空则忽略） |
| `pawnKindDef` | `PawnKindDef` | `null` | 空投单位种类。**优先于 thingDef** |
| `factionDef` | `FactionDef` | `null` | 派系。为空则使用 PawnKind 默认派系 |
| `num` | `int` | `1` | 数量（物品 = 总个数/堆叠，Pawn = 人数） |

**优先级：** `pawnKindDef` 非空 → 生成 Pawn 空投；否则 `thingDef` 非空 → 生成物品空投。

**物品堆叠：** 可堆叠物品（如 Silver, 钢铁）自动按 `stackLimit` 拆分为多堆；不可堆叠物品（如武器）逐个创建。

**XML 示例：**

```xml
<!-- 空投 500 白银 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply">
  <thingDef>Silver</thingDef>
  <num>500</num>
</li>

<!-- 空投 3 个虫巢战士，己方派系 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply">
  <pawnKindDef>ARA_Warrior</pawnKindDef>
  <factionDef>ARA_MinchoCandyWars</factionDef>
  <num>3</num>
</li>

<!-- 空投 5 把冲锋枪 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_DropSupply">
  <thingDef>Gun_SMG</thingDef>
  <num>5</num>
</li>
```

---

### 3.9 Effect_FireIncident

**全限定类名：** `MinchoCandyWars.StellarisEvent.Effect_FireIncident`

触发指定的 RimWorld 原生 IncidentDef（袭击、商队、热浪等）。

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `defName` | `string` | `null` | IncidentDef 的 defName |

**逻辑：** 使用 `StorytellerUtility.DefaultParmsNow()` 构造参数，在玩家殖民地地图上触发。

**XML 示例：**

```xml
<!-- 触发一次敌方袭击 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_FireIncident">
  <defName>RaidEnemy</defName>
</li>

<!-- 触发商队到达 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_FireIncident">
  <defName>TraderCaravanArrival</defName>
</li>

<!-- 触发热浪 -->
<li Class="MinchoCandyWars.StellarisEvent.Effect_FireIncident">
  <defName>HeatWave</defName>
</li>
```

---

## 4. 辅助类型速查

### 4.1 FlagValueType

```csharp
public enum FlagValueType { Float, String, Bool }
```

用于 flag 数据类型标识，在 `Trigger_FlagCheck.checkType` 和 `Effect_SetFlag.valueType` 中使用。

### 4.2 Flag 系统 API

`StellarisEventFlagManager` 是 GameComponent 单例，在 C# Action 中可直接调用其静态方法：

| 方法 | 签名 | 说明 |
|------|------|------|
| `SetFlag` | `(string name, float/string/bool value, int? ticks)` | 设置/覆写 flag |
| `GetFlag` | `(string name) → StellarisEventFlag?` | 获取 flag 对象 |
| `HasFlag` | `(string name) → bool` | 是否存在 |
| `RemoveFlag` | `(string name)` | 删除 |
| `ClearAll` | `()` | 清空全部 |
| `CheckFlag` | `(string name, FlagValueType type, object value) → bool` | 类型+值全匹配 |
| `ExtendTicks` | `(string name, int additional)` | 延长 tick |
| `MakePermanent` | `(string name)` | 取消 tick，设为永久 |

---

## 5. 完整 XML 示例

以下是一个完整事件定义的 XML 配置，展示 trigger、immediate、option.action、after 的使用方法：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Defs>
  <MinchoCandyWars.StellarisEvent.StellarisEventDef>
    <defName>ARA_WarDeclaration</defName>
    <title>{enemy} 的威胁</title>
    <desc>虫巢探测到 {enemy} 派系的敌意信号。威胁等级：{threat}</desc>
    <backgroundPath>UI/StellarisEvent/Bg_War</backgroundPath>

    <!-- ===== triggers：全部通过事件才触发 ===== -->
    <triggers>
      <!-- 独立判定：己方派系必须存在 -->
      <li Class="MinchoCandyWars.StellarisEvent.Triggers_FactionExists">
        <factionDef>ARA_MinchoCandyWars</factionDef>
      </li>
      <!-- 分组判定：任一敌对派系存在 -->
      <li Class="MinchoCandyWars.StellarisEvent.Triggers_FactionExists">
        <factionDef>Pirate</factionDef>
        <idGroup>enemyExists</idGroup>
        <logic>OR</logic>
      </li>
      <li Class="MinchoCandyWars.StellarisEvent.Triggers_FactionExists">
        <factionDef>Tribal</factionDef>
        <idGroup>enemyExists</idGroup>
        <logic>OR</logic>
      </li>
    </triggers>

    <!-- ===== immediate：触发后、弹窗前执行 ===== -->
    <immediate>
      <effects>
        <!-- 先抓取本地化参数（必须在 SetFlag 之前，确保参数为当前值） -->
        <li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam">
          <flagName>enemyName</flagName>
          <paramKey>enemy</paramKey>
        </li>
        <li Class="MinchoCandyWars.StellarisEvent.Effect_FetchFlagAsParam">
          <flagName>threatLevel</flagName>
          <paramKey>threat</paramKey>
        </li>
        <!-- 仅首次创建 -->
        <li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
          <flagName>firstWar</flagName>
          <valueType>Bool</valueType>
          <boolValue>true</boolValue>
          <setMode>Create</setMode>
        </li>
        <!-- 覆盖写入 -->
        <li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
          <flagName>warActive</flagName>
          <valueType>Bool</valueType>
          <boolValue>true</boolValue>
          <setMode>Override</setMode>
        </li>
        <li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
          <flagName>warScore</flagName>
          <valueType>Float</valueType>
          <floatValue>0</floatValue>
          <setMode>Override</setMode>
        </li>
      </effects>
    </immediate>

    <!-- ===== options：玩家选择 ===== -->
    <options>
      <!-- 选项1：迎战（仅在敌国真的存在时可用） -->
      <li>
        <label>迎战 {enemy}</label>
        <desc>集结虫群，对抗 {enemy}（威胁 {threat}）。</desc>
        <trigger Class="MinchoCandyWars.StellarisEvent.Trigger_FlagCheck">
          <flagName>enemyNationDefeated</flagName>
          <checkType>Bool</checkType>
          <expectedBool>false</expectedBool>
        </trigger>
        <action>
          <effects>
            <li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
              <flagName>warScore</flagName>
              <valueType>Float</valueType>
              <floatValue>50</floatValue>
              <setMode>Add</setMode>
            </li>
          </effects>
        </action>
      </li>
      <!-- 选项2：撤退 -->
      <li>
        <label>撤退</label>
        <desc>保存实力，等待时机。</desc>
        <action>
          <effects>
            <li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
              <flagName>warScore</flagName>
              <valueType>Float</valueType>
              <floatValue>10</floatValue>
              <setMode>Subtract</setMode>
            </li>
          </effects>
        </action>
      </li>
    </options>

    <!-- ===== after：option.action 执行完后执行 ===== -->
    <after>
      <effects>
        <li Class="MinchoCandyWars.StellarisEvent.Effect_SetFlag">
          <flagName>eventDone</flagName>
          <valueType>Bool</valueType>
          <boolValue>true</boolValue>
          <!-- 60000 ticks ≈ 24 游戏小时 -->
          <ticks>60000</ticks>
        </li>
      </effects>
    </after>

    <!-- ===== pictures：立绘 ===== -->
    <pictures>
      <li>
        <path>UI/StellarisEvent/Portrait_Queen</path>
        <portraitOffsetX>0</portraitOffsetX>
        <portraitOffsetY>0</portraitOffsetY>
      </li>
    </pictures>

  </MinchoCandyWars.StellarisEvent.StellarisEventDef>
</Defs>
```

---

## 附录：开发调试

- **Flag 控制台：** Dev 菜单 → `Stellaris Events → Open Flag Console`，可检视/添加/删除 flag
- **事件控制台：** Dev 菜单 → `Stellaris Events → Open Event Console`，可检视/强制触发事件
- **游戏时间参考：** 2500 ticks = 1 游戏小时，60000 ticks ≈ 1 游戏天
