# SqlSugar.Tools 使用教程（中文）

本仓库是一个基于 **NanUI + Vue3 + Element Plus** 的桌面工具，用于：

- **连接多种数据库**：SQL Server / MySQL / PostgreSQL / Oracle / SQLite
- **生成实体映射类**：自动带上 SqlSugar 特性（表注释、列注释、主键、自增、必填/可空）
- **分层代码生成（增强版）**：按“原公司模板生成器”风格输出 **Model/DAL/BLL/Common/WebApi**，并额外输出 `_base / Services / PowerMenu / Model partial+search` 等，让项目“拷贝进去即可跑”，再按业务补齐控制器逻辑即可。

> 本文档以“你打开工具之后，如何从 0 到 1 生成出规范代码”为主线，包含命名规则、注释规范、枚举/导航规范、输出结构、常见问题。

---

## 一、快速开始（从连接数据库到生成实体）

### 1. 打开工具

启动后进入首页，点击 **代码生成**。

### 2. 添加数据库连接

在上方菜单选择对应数据库类型：

- SQL Server / MySql / Oracle / PostgreSQL / SQLite

然后进入连接配置：

- 填写连接信息
- 点击 **测试连接**（部分数据库会顺便加载数据库列表）
- 点击 **确定**

### 3. 加载表并搜索

连接成功后左侧会加载表列表：

- 支持输入表名进行模糊搜索
- 点击某张表即可预览生成代码（右侧）

### 4. 预览 / 保存

- **预览**：点击表即可在右侧看到代码
- **保存单表**：预览后按 `Ctrl+S` 保存单表
- **分层生成**：勾选表 → 分层生成向导 → 选择模板/层 → 开始生成 → 生成完成可一键打开目录

---

## 二、实体类生成规范（重点）

生成的实体映射类会尽量做到“同事一眼能读懂、ORM 一眼能识别”。

### 1) 表特性（类上）

每个实体类会生成类似：

- `[SugarTable("原表名", TableDescription="中文表注释")]`

用于：

- 映射真实表名（实体类名可与表名不同）
- 在代码中保留表中文说明

### 2) 字段特性（每个属性上）

每个属性头顶会生成 `[SugarColumn(...)]`，并包含：

- **`ColumnDescription`**：列中文注释（来自数据库列备注/扩展属性）
- **`IsPrimaryKey`**：主键（若数据库识别为主键）
- **`IsIdentity`**：自增（若数据库识别为自增）
- **`IsNullable`**：必填/可空（来自数据库的 `AllowDBNull` 元数据）

说明：

- **表里真实存在的列**：不需要加 `IsIgnore=false` 之类的“多此一举参数”
- **非表字段/扩展字段/计算字段/导航字段**：必须加
  - `[SugarColumn(IsIgnore = true)]`
  - 如果是导航属性，还要再加 `[Navigate(...)]`

---

## 三、命名规范（外键 / 导航）

### 1) 现代外键命名（推荐）

外键字段 = **主表名 + Id**

示例：

- `UserId`
- `RoleId`
- `CategoryId`

> 这套是现代 C# 项目最常见写法，可读性最好，也便于生成器做“逻辑外键推断”。

### 2) 外键识别优先级（很重要）

生成导航属性时，我们按以下优先级识别关联关系：

1. **真实外键（数据库 FK 约束）**：最准确，优先使用
2. **逻辑外键（命名推断）**：当数据库没建 FK 约束时，按 `UserId` 这类规则推断
3. **兼容旧规则（可选）**：例如历史项目的 `fk_` 前缀（如你不需要可关闭）

---

## 四、枚举生成规范（稳定且可控）

推荐在“列备注/扩展属性”里用统一标记声明枚举：

示例：

- `(Enum:0=禁用,1=启用)`
- `(Enum:0=Disabled;1=Enabled)`

生成器将会：

- 解析出枚举项和值
- 自动生成对应 `enum`（增强中：输出到 `Common/Enums/` 或实体 partial）

> 不建议用“status/type 关键词猜测”这种纯启发式方式，误判概率高。

---

## 五、分层(MVC)代码生成（增强版：可直接落项目）

> 详细“生成产物说明”（从大到小、每层包含哪些目录/文件/作用）见：`docs/生成产物说明.md`

### 1) 输出结构（与模板生成器风格对齐）

以 `RootNamespacePrefix.ProjectName` 为根（可配置），默认会生成这些“项目目录”（注意：目录名就是命名空间）：

- `{前缀}.{项目}.Model`
- `{前缀}.{项目}.DAL`
- `{前缀}.{项目}.BLL`
- `{前缀}.{项目}.Common`
- `{前缀}.{项目}.WebApi`

### 2) Model 层（重点：generat/temp + 每表目录）

Model 采用“生成与扩展分离”的结构，避免你改动生成文件被覆盖：

- `generat/<表名>/Model<表名>.cs`：**表映射实体**（带 SugarTable/SugarColumn，字段严格对应数据库列）
- `temp/<表名>/Model<表名>_partial.cs`：**扩展部分类**（导航/枚举/非表字段放这里，默认不映射）
- `temp/<表名>/Model<表名>_search.cs`：**后台搜索类**（后台筛选/查询参数放这里）

### 3) DAL/Common/BLL（DIP + SqlSugarScope）

我们推荐使用 `SqlSugarScope` 并做依赖倒置：

- 上层（BLL / Controller）只依赖接口（如 `IRepository<T>`、`IService`）
- 底层（DAL）持有 `ISqlSugarDbContext`，内部暴露 `SqlSugarScope Db`

这样带来的好处：

- 代码结构清晰、可测试、可替换
- 多库/事务/读写分离更好扩展

### 4) WebApi（统一返回体 + _base + 权限骨架）

WebApi 默认会生成：

- `_base`：基础控制器链（`BaseController` 等）
- `_base/Results`：统一返回体 `SysResult/SysResultString/SysResult_layui_table`（替代 `Ok()`）
- `_base/HelperPowers`：权限菜单 `PowerMenu_表名.cs`（每表一份）
- `Services`：依赖注入注册扩展（`AddGeneratedServices()`）
- `Controllers`：每表 Controller（默认输出 `get_page/do_add/do_edit/do_delete` 结构，后续按业务补齐查询条件/主键删除等）

### 5) 为什么不用 `Ok()`？

原工具使用统一返回体是为了：

- 前端/调用方统一判断 `code/msg/data`
- 方便扩展：分页、错误码、traceId、权限提示等
- 避免 `Ok(object)` “只有 data 没有语义”的痛点

增强版生成器会默认提供常用 CRUD 封装（DAL 层）：

- 查询：`GetById / GetList / Page`
- 新增：`Insert / InsertBatch`
- 更新：`Update / UpdateColumns(只更新指定列)`
- 删除：`DeleteById / Delete(where)`

并提供对应 `Async` 版本。

---

## 六、常见问题（FAQ）

### 1) 为什么建议“真实 FK 优先”？

因为 FK 约束是数据库层面的事实来源：

- 推断不会错
- 能自动生成更可靠的导航属性

### 2) 我们没建 FK 约束怎么办？

遵守 `UserId/RoleId` 这类现代命名规范即可，生成器会进行逻辑推断（增强中会提供“冲突提示/手工修正”）。

### 3) `IsNullable` 和 C# 的 `?` 有什么关系？

- `IsNullable` 表示数据库列是否允许 NULL（映射语义）
- `int? / DateTime?` 表示 C# 值类型可空（类型表达）

生成器会尽量保持两者一致。

---

## 七、建议的数据库注释规范（强烈推荐）

为了让生成代码“像手写的一样清晰”，建议你在建表/维护表时统一补齐：

- 表注释（TableDesc）
- 列注释（ColumnDesc）
- 枚举标记（`(Enum:...)`）

---

## 八、更新记录（本次增强）

- 实体类生成：新增/规范化
  - `[SugarTable(..., TableDescription=...)]`
  - `[SugarColumn(ColumnDescription=..., IsPrimaryKey/IsIdentity/IsNullable)]`
- 新增 `CodeGen` 能力：
  - 真实 FK 读取（多数据库）
  - 逻辑外键推断（`UserId` 规则）
  - 枚举备注解析（`(Enum:...)`）
  - 分层脚手架（Common/DAL 的 SqlSugarScope + Repository 封装）
  - WebApi 统一返回体（SysResult*）+ `_base/Services/HelperPowers`
  - Model 结构对齐（generat/temp + partial/search）

