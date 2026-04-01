using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlSugar.Tools.CodeGen
{
    public static class LayerScaffoldBuilder
    {
        public static List<GeneratedFile> BuildCommonAndDalScaffold(LayerGenOptions opt)
        {
            if (opt == null) opt = new LayerGenOptions();

            var files = new List<GeneratedFile>();

            if (opt.GenerateCommon)
            {
                files.Add(new GeneratedFile
                {
                    RelativePath = $"Common/ISqlSugarDbContext.cs",
                    Content = BuildISqlSugarDbContext(opt)
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = $"Common/SqlSugarDbContext.cs",
                    Content = BuildSqlSugarDbContext(opt)
                });
            }

            if (opt.GenerateDal)
            {
                files.Add(new GeneratedFile
                {
                    RelativePath = $"DAL/IRepository.cs",
                    Content = BuildIRepository(opt)
                });
                files.Add(new GeneratedFile
                {
                    RelativePath = $"DAL/Repository.cs",
                    Content = BuildRepository(opt)
                });
            }

            return files;
        }

        public static List<GeneratedFile> BuildBllAndWebApiForEntities(LayerGenOptions opt, IEnumerable<string> entityNames)
        {
            if (opt == null) opt = new LayerGenOptions();
            var names = (entityNames ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var files = new List<GeneratedFile>();

            if (opt.GenerateBll)
            {
                files.Add(new GeneratedFile
                {
                    RelativePath = $"BLL/BaseBll.cs",
                    Content = BuildBaseBll(opt)
                });
                foreach (var n in names)
                {
                    files.Add(new GeneratedFile
                    {
                        RelativePath = $"BLL/generat/{n}/Bll{n}.cs",
                        Content = BuildBllGenerated(opt, n)
                    });
                    files.Add(new GeneratedFile
                    {
                        RelativePath = $"BLL/temp/{n}/Bll{n}_partial.cs",
                        Content = BuildBllPartial(opt, n)
                    });
                }
            }

            if (opt.GenerateWebApi)
            {
                foreach (var n in names)
                {
                    files.Add(new GeneratedFile
                    {
                        RelativePath = $"WebApi/Controllers/{n}Controller.cs",
                        Content = BuildWebApiController(opt, n)
                    });
                }
            }

            return files;
        }

        private static string BuildBaseBll(LayerGenOptions opt)
        {
            var ns = opt.BllNamespace;
            var dalNs = opt.DalNamespace;
            var modelNs = opt.ModelNamespace;
            return $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using {dalNs};
using {opt.WebApiNamespace}._base.Results;

namespace {ns}
{{
    /// <summary>
    /// 基础业务层封装：基于 IRepository 组合常用 CRUD。
    /// </summary>
    public abstract class BaseBll<T> where T : class, new()
    {{
        protected readonly IRepository<T> Repo;

        protected BaseBll(IRepository<T> repo)
        {{
            Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }}

        public virtual T GetById(object id) => Repo.GetById(id);
        public virtual Task<T> GetByIdAsync(object id) => Repo.GetByIdAsync(id);
        public virtual List<T> GetList(Expression<Func<T, bool>> where = null) => Repo.GetList(where);
        public virtual Task<List<T>> GetListAsync(Expression<Func<T, bool>> where = null) => Repo.GetListAsync(where);
        public virtual int Add(T entity) => Repo.Insert(entity);
        public virtual Task<int> AddAsync(T entity) => Repo.InsertAsync(entity);
        public virtual int Update(T entity) => Repo.Update(entity);
        public virtual Task<int> UpdateAsync(T entity) => Repo.UpdateAsync(entity);
        public virtual int DeleteById(object id) => Repo.DeleteById(id);
        public virtual Task<int> DeleteByIdAsync(object id) => Repo.DeleteByIdAsync(id);
        public virtual int Del(Expression<Func<T, bool>> where) => Repo.Delete(where);
        public virtual Task<int> DelAsync(Expression<Func<T, bool>> where) => Repo.DeleteAsync(where);

        /// <summary>
        /// 分页查询（默认使用 searchDTO 反射构建 where：字段同名=等值；string=Contains；xxx_start/xxx_end=区间）
        /// </summary>
        public virtual PagedResult<T> GetPage(object searchDto, int page, int pageSize)
        {{
            return Repo.GetPage(searchDto, page, pageSize);
        }}

        public virtual Task<PagedResult<T>> GetPageAsync(object searchDto, int page, int pageSize)
        {{
            return Repo.GetPageAsync(searchDto, page, pageSize);
        }}
    }}
}}
";
        }

        private static string BuildBllGenerated(LayerGenOptions opt, string entityName)
        {
            var ns = opt.BllNamespace;
            var dalNs = opt.DalNamespace;
            var modelNs = opt.ModelNamespace;
            var modelTypeName = $"Model{entityName}";
            return $@"using {dalNs};
using {modelNs};

namespace {ns}
{{
    /// <summary>
    /// 业务逻辑层（生成）：{entityName}
    /// </summary>
    public partial class Bll{entityName} : BaseBll<{modelTypeName}>
    {{
        public Bll{entityName}(IRepository<{modelTypeName}> repo) : base(repo) {{ }}
    }}
}}
";
        }

        private static string BuildBllPartial(LayerGenOptions opt, string entityName)
        {
            var ns = opt.BllNamespace;
            var modelNs = opt.ModelNamespace;
            var modelTypeName = $"Model{entityName}";
            return $@"using {modelNs};

namespace {ns}
{{
    /// <summary>
    /// 业务逻辑层（扩展/自定义）：{entityName}
    /// </summary>
    public partial class Bll{entityName} : BaseBll<{modelTypeName}>
    {{
    }}
}}
";
        }

        private static string BuildWebApiController(LayerGenOptions opt, string entityName)
        {
            var ns = opt.WebApiNamespace;
            var bllNs = opt.BllNamespace;
            var modelNs = opt.ModelNamespace;
            var modelTypeName = $"Model{entityName}";
            var bllVar = char.ToLowerInvariant(entityName[0]) + entityName.Substring(1) + "Bll";
            var bllField = "_" + bllVar;
            return $@"using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

using {bllNs};
using {modelNs};
using {ns}._base;
using {ns}._base.HelperPowers;
using {ns}._base.Results;

namespace {ns}.Controllers
{{
    [ApiController]
    [Route(""api/[controller]"")]
    public class {entityName}Controller : BaseController
    {{
        private readonly Bll{entityName} {bllField};

        public {entityName}Controller(Bll{entityName} {bllVar})
        {{
            {bllField} = {bllVar};
        }}

        [HttpPost]
        [HasPower(PowerAction.查看列表)]
        public SysResult_layui_table get_page([FromForm] {modelTypeName}_search param, [FromForm] formparam_PagerInfo_LayUI pager)
        {{
            var pageResult = {bllField}.GetPage(param, pager.page, pager.limit);
            return SysResult.Return_layui(pageResult);
        }}

        [HttpPost]
        [HasPower(PowerAction.添加)]
        public SysResultString do_add([FromBody] {modelTypeName} param)
        {{
            var row = {bllField}.Add(param);
            return row > 0 ? SuccessString(""添加成功"") : ErrorString(""添加失败"");
        }}

        [HttpPost]
        [HasPower(PowerAction.编辑)]
        public SysResultString do_edit([FromBody] {modelTypeName} param)
        {{
            var row = {bllField}.Update(param);
            return row > 0 ? SuccessString(""编辑成功"") : ErrorString(""编辑失败"");
        }}

        [HttpPost]
        [HasPower(PowerAction.删除)]
        public SysResultString do_delete([FromBody] formbody_param_del param)
        {{
            // TODO: 按业务补齐主键字段与批量删除
            if (param == null || string.IsNullOrWhiteSpace(param.ids)) return ErrorString(""ids 不能为空"");
            return SuccessString(""删除成功"");
        }}
    }}
}}
";
        }

        private static string BuildISqlSugarDbContext(LayerGenOptions opt)
        {
            var ns = opt.CommonNamespace;
            return $@"using SqlSugar;

namespace {ns}
{{
    public interface ISqlSugarDbContext
    {{
        SqlSugarScope Db {{ get; }}
    }}
}}
";
        }

        private static string BuildSqlSugarDbContext(LayerGenOptions opt)
        {
            var ns = opt.CommonNamespace;
            return $@"using System;
using SqlSugar;

namespace {ns}
{{
    /// <summary>
    /// SqlSugarScope 包装：用于依赖倒置（上层依赖 ISqlSugarDbContext）。
    /// 注意：Sugar 连接配置（ConnectionConfig / 读写分离 / 多库）由宿主项目提供。
    /// </summary>
    public class SqlSugarDbContext : ISqlSugarDbContext
    {{
        public SqlSugarScope Db {{ get; }}

        public SqlSugarDbContext(SqlSugarScope db)
        {{
            Db = db ?? throw new ArgumentNullException(nameof(db));
        }}
    }}
}}
";
        }

        private static string BuildIRepository(LayerGenOptions opt)
        {
            var ns = opt.DalNamespace;
            var commonNs = opt.CommonNamespace;
            return $@"using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SqlSugar;

using {commonNs};
using {opt.WebApiNamespace}._base.Results;

namespace {ns}
{{
    public interface IRepository<T> where T : class, new()
    {{
        ISqlSugarDbContext Context {{ get; }}
        SqlSugarScope Db {{ get; }}
        ISugarQueryable<T> Queryable();

        // 查询
        T GetById(object id);
        Task<T> GetByIdAsync(object id);
        List<T> GetList(Expression<Func<T, bool>> where = null);
        Task<List<T>> GetListAsync(Expression<Func<T, bool>> where = null);

        // 分页（配合 SysResult.Return_layui）
        PagedResult<T> GetPage(object searchDto, int page, int pageSize);
        Task<PagedResult<T>> GetPageAsync(object searchDto, int page, int pageSize);

        // 新增
        int Insert(T entity);
        Task<int> InsertAsync(T entity);
        int InsertBatch(IEnumerable<T> entities);
        Task<int> InsertBatchAsync(IEnumerable<T> entities);

        // 更新
        int Update(T entity);
        Task<int> UpdateAsync(T entity);

        /// <summary>
        /// 只更新指定列（常用：只更新前端传来的变更字段）
        /// </summary>
        int UpdateColumns(T entity, Expression<Func<T, object>> columns);
        Task<int> UpdateColumnsAsync(T entity, Expression<Func<T, object>> columns);

        // 删除
        int DeleteById(object id);
        Task<int> DeleteByIdAsync(object id);
        int Delete(Expression<Func<T, bool>> where);
        Task<int> DeleteAsync(Expression<Func<T, bool>> where);
    }}
}}
";
        }

        private static string BuildRepository(LayerGenOptions opt)
        {
            var ns = opt.DalNamespace;
            var commonNs = opt.CommonNamespace;
            return $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SqlSugar;

using {commonNs};
using {opt.WebApiNamespace}._base.Results;

namespace {ns}
{{
    public class Repository<T> : IRepository<T> where T : class, new()
    {{
        public ISqlSugarDbContext Context {{ get; }}
        public SqlSugarScope Db => Context.Db;

        public Repository(ISqlSugarDbContext context)
        {{
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }}

        public ISugarQueryable<T> Queryable()
        {{
            return Db.Queryable<T>();
        }}

        public T GetById(object id)
        {{
            return Db.Queryable<T>().InSingle(id);
        }}

        public Task<T> GetByIdAsync(object id)
        {{
            return Db.Queryable<T>().InSingleAsync(id);
        }}

        public List<T> GetList(Expression<Func<T, bool>> where = null)
        {{
            var q = Db.Queryable<T>();
            if (where != null) q = q.Where(where);
            return q.ToList();
        }}

        public Task<List<T>> GetListAsync(Expression<Func<T, bool>> where = null)
        {{
            var q = Db.Queryable<T>();
            if (where != null) q = q.Where(where);
            return q.ToListAsync();
        }}

        public PagedResult<T> GetPage(object searchDto, int page, int pageSize)
        {{
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = Db.Queryable<T>();
            q = ApplySearch(q, searchDto);
            int total = 0;
            var list = q.ToPageList(page, pageSize, ref total);
            return new PagedResult<T> {{ total = total, list = list }};
        }}

        public async Task<PagedResult<T>> GetPageAsync(object searchDto, int page, int pageSize)
        {{
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = Db.Queryable<T>();
            q = ApplySearch(q, searchDto);
            RefAsync<int> total = 0;
            var list = await q.ToPageListAsync(page, pageSize, total);
            return new PagedResult<T> {{ total = total, list = list }};
        }}

        private ISugarQueryable<T> ApplySearch(ISugarQueryable<T> q, object searchDto)
        {{
            if (searchDto == null) return q;
            var dtoType = searchDto.GetType();
            var modelType = typeof(T);

            foreach (var p in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {{
                var name = p.Name;
                var value = p.GetValue(searchDto, null);
                if (value == null) continue;

                // string 空值跳过
                if (value is string s)
                {{
                    if (string.IsNullOrWhiteSpace(s)) continue;
                }}

                // keyword：对模型里所有 string 字段做 OR Contains（默认弱实现，可按业务替换）
                if (string.Equals(name, ""keyword"", StringComparison.OrdinalIgnoreCase))
                {{
                    var kw = value.ToString();
                    if (string.IsNullOrWhiteSpace(kw)) continue;
                    var stringProps = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(x => x.PropertyType == typeof(string))
                        .Select(x => x.Name)
                        .ToList();
                    if (stringProps.Count == 0) continue;

                    var cond = string.Join("" OR "", stringProps.Select(x => $""{{x}} LIKE @kw""));
                    q = q.Where($""({{cond}})"", new {{ kw = $""%{{kw}}%"" }});
                    continue;
                }}

                // xxx_start / xxx_end 区间
                if (name.EndsWith(""_start"", StringComparison.OrdinalIgnoreCase) || name.EndsWith(""_end"", StringComparison.OrdinalIgnoreCase))
                {{
                    var baseName = name.Substring(0, name.LastIndexOf('_'));
                    var mp = modelType.GetProperty(baseName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (mp == null) continue;
                    if (name.EndsWith(""_start"", StringComparison.OrdinalIgnoreCase))
                    {{
                        q = q.Where($""{{mp.Name}} >= @p"", new {{ p = value }});
                    }}
                    else
                    {{
                        q = q.Where($""{{mp.Name}} <= @p"", new {{ p = value }});
                    }}
                    continue;
                }}

                // 同名字段：string=Contains，其它=Equals
                var modelProp = modelType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (modelProp == null) continue;

                if (value is string s2)
                {{
                    q = q.Where($""{{modelProp.Name}} LIKE @p"", new {{ p = $""%{{s2}}%"" }});
                }}
                else
                {{
                    q = q.Where($""{{modelProp.Name}} = @p"", new {{ p = value }});
                }}
            }}

            return q;
        }}

        public int Insert(T entity)
        {{
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return Db.Insertable(entity).ExecuteCommand();
        }}

        public Task<int> InsertAsync(T entity)
        {{
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return Db.Insertable(entity).ExecuteCommandAsync();
        }}

        public int InsertBatch(IEnumerable<T> entities)
        {{
            var list = (entities ?? Enumerable.Empty<T>()).ToList();
            if (list.Count == 0) return 0;
            return Db.Insertable(list).ExecuteCommand();
        }}

        public Task<int> InsertBatchAsync(IEnumerable<T> entities)
        {{
            var list = (entities ?? Enumerable.Empty<T>()).ToList();
            if (list.Count == 0) return Task.FromResult(0);
            return Db.Insertable(list).ExecuteCommandAsync();
        }}

        public int Update(T entity)
        {{
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return Db.Updateable(entity).ExecuteCommand();
        }}

        public Task<int> UpdateAsync(T entity)
        {{
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return Db.Updateable(entity).ExecuteCommandAsync();
        }}

        public int UpdateColumns(T entity, Expression<Func<T, object>> columns)
        {{
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            return Db.Updateable(entity).UpdateColumns(columns).ExecuteCommand();
        }}

        public Task<int> UpdateColumnsAsync(T entity, Expression<Func<T, object>> columns)
        {{
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            return Db.Updateable(entity).UpdateColumns(columns).ExecuteCommandAsync();
        }}

        public int DeleteById(object id)
        {{
            return Db.Deleteable<T>().In(id).ExecuteCommand();
        }}

        public Task<int> DeleteByIdAsync(object id)
        {{
            return Db.Deleteable<T>().In(id).ExecuteCommandAsync();
        }}

        public int Delete(Expression<Func<T, bool>> where)
        {{
            if (where == null) throw new ArgumentNullException(nameof(where));
            return Db.Deleteable<T>().Where(where).ExecuteCommand();
        }}

        public Task<int> DeleteAsync(Expression<Func<T, bool>> where)
        {{
            if (where == null) throw new ArgumentNullException(nameof(where));
            return Db.Deleteable<T>().Where(where).ExecuteCommandAsync();
        }}
    }}
}}
";
        }
    }
}

