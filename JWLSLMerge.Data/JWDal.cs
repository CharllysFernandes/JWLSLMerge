using Dapper;
using JWLSLMerge.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace JWLSLMerge.Data
{
    /// <summary>
    /// Classe que lida com operações de acesso a dados usando Dapper para SQLite.
    /// </summary>
    public class JWDal
    {
        private string connectionString;

        /// <summary>
        /// Inicializa uma nova instância da classe JWDal com o caminho do banco de dados.
        /// </summary>
        /// <param name="dbPath">O caminho para o banco de dados SQLite.</param>
        public JWDal(string dbPath)
        {
            connectionString = $"Data Source={dbPath}";
        }

        /// <summary>
        /// Obtém uma lista de registros da tabela especificada.
        /// </summary>
        /// <typeparam name="T">O tipo de objeto representando a tabela.</typeparam>
        /// <returns>Uma coleção de registros da tabela.</returns>
        public IEnumerable<T> TableList<T>()
        {
            using (IDbConnection cnn = new SQLiteConnection(connectionString))
            {
                return cnn.Query<T>($"SELECT * FROM {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Obtém o primeiro registro que corresponde aos critérios de busca.
        /// </summary>
        /// <typeparam name="T">O tipo de objeto representando a tabela.</typeparam>
        /// <param name="item">O objeto que contém os critérios de busca.</param>
        /// <param name="FieldNames">Os nomes dos campos para a cláusula WHERE.</param>
        /// <param name="SetEmptyWhenNull">Indica se campos nulos devem ser definidos como vazios.</param>
        /// <returns>O primeiro registro correspondente aos critérios de busca ou null se não encontrado.</returns>
        public T? GetFirst<T>(T item, string[] FieldNames, bool SetEmptyWhenNull = false)
        {
            using (IDbConnection con = new SQLiteConnection(connectionString))
            {
                string sql = $"SELECT * FROM {typeof(T).Name} WHERE {getWhereClause(FieldNames)}";

                return con.Query<T>(sql, getParameters<T>(item, FieldNames, SetEmptyWhenNull)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Verifica se um registro corresponde aos critérios de busca.
        /// </summary>
        /// <typeparam name="T">O tipo de objeto representando a tabela.</typeparam>
        /// <param name="item">O objeto que contém os critérios de busca.</param>
        /// <param name="FieldNames">Os nomes dos campos para a cláusula WHERE.</param>
        /// <param name="SetEmptyWhenNull">Indica se campos nulos devem ser definidos como vazios.</param>
        /// <returns>true se o registro correspondente existe; false caso contrário.</returns>
        public bool ItemExists<T>(T item, string[] FieldNames, bool SetEmptyWhenNull = false)
        {
            using (IDbConnection con = new SQLiteConnection(connectionString))
            {
                string sql = $"SELECT 1 FROM {typeof(T).Name} WHERE {getWhereClause(FieldNames)}";

                return con.ExecuteScalar<int>(sql, getParameters<T>(item, FieldNames, SetEmptyWhenNull)) > 0;
            }
        }

        /// <summary>
        /// Insere um novo registro na tabela.
        /// </summary>
        /// <typeparam name="T">O tipo de objeto representando a tabela.</typeparam>
        /// <param name="item">O objeto que contém os valores a serem inseridos.</param>
        /// <returns>O ID do registro inserido.</returns>
        public int ItemInsert<T>(T item)
        {
            using (IDbConnection con = new SQLiteConnection(connectionString))
            {
                string sql =
                    $"INSERT INTO {typeof(T).Name} ({getFieldNames<T>()}) " +
                    $"VALUES ({getFieldNames<T>(true)});" +
                    "SELECT last_insert_rowid();";

                return con.ExecuteScalar<int>(sql, getParameters<T>(item));
            }
        }

        /// <summary>
        /// Define a última modificação no banco de dados.
        /// </summary>
        /// <returns>A data e hora da última modificação.</returns>
        public string SetLastModification()
        {
            string dt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            using (IDbConnection con = new SQLiteConnection(connectionString))
            {
                string sql = $"UPDATE LastModified SET LastModified = '{dt}'";

                con.Execute(sql);
            }

            return dt;
        }

        private DynamicParameters getParameters<T>(T objeto, string[]? FieldNames = null, bool SetEmptyWhenNull = false)
        {
            FieldNames = FieldNames?.Select(p => p.ToLower()).ToArray() ?? new string[0];
            var parameters = new DynamicParameters();

            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                if (!propertyInfo.GetCustomAttributes(true).Any(a => a is IgnoreAttribute) &&
                    (FieldNames.Length == 0 || FieldNames.Contains(propertyInfo.Name.ToLower())))
                {
                    object? value = propertyInfo.GetValue(objeto);
                    if (value == null && SetEmptyWhenNull) value = "";

                    parameters.Add(propertyInfo.Name, value);
                }
            }

            return parameters;
        }

        private string getWhereClause(string[] FieldNames)
        {
            return string.Join(" AND ", FieldNames.Select(p => $"IFNULL({p}, '') = @{p}").ToArray());
        }

        private string getFieldNames<T>(bool includeAtSymbol = false)
        {
            List<string> names = new List<string>();

            foreach (var propriedade in typeof(T).GetProperties())
            {
                if (!propriedade.GetCustomAttributes(true).Any(a => a is IgnoreAttribute))
                {
                    names.Add((includeAtSymbol ? "@" : "") + propriedade.Name);
                }
            }

            return string.Join(",", names.ToArray());
        }
    }
}
