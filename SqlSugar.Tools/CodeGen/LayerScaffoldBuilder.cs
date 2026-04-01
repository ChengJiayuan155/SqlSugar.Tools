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
using System.Linq.Expressions;
using System.Threading.Tasks;
using SqlSugar;

using {commonNs};

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

