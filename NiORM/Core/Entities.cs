﻿using NiORM.Interfaces;

namespace NiORM.Core
{
    /// <summary>
    /// An object for CRUD actions
    /// </summary>
    /// <typeparam name="T">Type Of Object related to table. It should be inherited from ITable</typeparam>
    public class Entities<T> where T : ITable, new()
    {
        private string ConnectionString { get; init; }
        private SqlMaster<T> SqlMaster { get; init; }
        public Entities(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
            SqlMaster = new SqlMaster<T>(ConnectionString);
        }

        private string TableName = ObjectDescriber<T, int>.GetTableName(new T());

        /// <summary>
        /// A method for first row in table
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault() => SqlMaster.Get(Query: $"SELECT TOP(1) * FROM {this.TableName}").FirstOrDefault();

        /// <summary>
        ///   A method for first row in table with conditions in TSQL
        /// </summary>
        /// <param name="Query">TSQL Query</param>
        /// <returns></returns>
        public T FirstOrDefault(string Query) => SqlMaster.Get($"SELECT TOP(1) * FROM {this.TableName} WHERE {Query}").FirstOrDefault();

        /// <summary>
        /// A method for find an object using its primary key (Just for tables with one PK)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T Find(string id)
        {
            var Keys = ObjectDescriber<T, int>.GetPrimaryKeys(new T());

            if (Keys.Count != 1)
                throw new Exception("The count of arguments are not same as PrimaryKeys");
            var Entity = new T();
            ObjectDescriber<T, int>.SetValue(Entity, Keys[0], int.Parse(id));
            return SqlMaster.Get($"Select top(1) * from {this.TableName} where  [{Keys[0]}]= {ObjectDescriber<T, string>.ToSqlFormat(Entity, Keys[0])}").FirstOrDefault();
        }

        /// <summary>
        /// A method for find an object using its primary key (Just for tables with one PK)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T Find(int id) => Find(id.ToString());

        /// <summary>
        /// A method for find an object using its primary key (Just for tables with two PK)
        /// </summary>
        /// <param name="firstId">Primary Key</param>
        /// <param name="secondId">Primary Key</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T Find(string firstId, string secondId)
        {
            var Keys = ObjectDescriber<T, int>.GetPrimaryKeys(new T());

            if (Keys.Count != 2)
                throw new Exception("The count of arguments are not same as PrimaryKeys");
            var Entity = new T();
            ObjectDescriber<T, int>.SetValue(Entity, Keys[0], int.Parse(firstId));
            ObjectDescriber<T, int>.SetValue(Entity, Keys[1], int.Parse(secondId));
            return SqlMaster.Get($@"SELECT TOP(1) * FROM {this.TableName}
                                    WHERE
                                        [{Keys[0]}]= {ObjectDescriber<T, string>.ToSqlFormat(Entity, Keys[0])}
                                        AND
                                         [{Keys[1]}]= {ObjectDescriber<T, string>.ToSqlFormat(Entity, Keys[1])}").FirstOrDefault();

        }

        /// <summary>
        /// A method for find an object using its primary key (Just for tables with two PK)
        /// </summary>
        /// <param name="firstId">Primary Key</param>
        /// <param name="secondId">Primary Key</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T Find(int firstId, string secondId) => Find(firstId.ToString(), secondId.ToString());

        public List<T> ToList()
        {
            var Properties = ObjectDescriber<T, string>.GetProperties(new T());
            var Addition = "";
            var PrimaryKeys = ObjectDescriber<T, int>.GetPrimaryKeys(new T());

            if (Properties.Any(c => c == "IsActive"))
            {
                Addition = $"ORDER BY IsActive DESC,{PrimaryKeys.FirstOrDefault()}";
            }

            return SqlMaster.Get($"SELECT * FROM {this.TableName} {Addition}").ToList();
        }

        /// <summary>
        /// A method for fetching table with Query in where
        /// </summary>
        /// <param name="Query"></param>
        /// <returns></returns>
        public List<T> Query(string Query)
        {
            return SqlMaster.Get(Query).ToList();
        }

        /// <summary>
        /// A method for executing a TSQL command
        /// </summary>
        /// <param name="Query"></param>
        public void Execute(string Query)
        {
            SqlMaster.Execute(Query);
        }

        /// <summary>
        /// A method for fetching table with Query in where
        /// </summary>
        /// <param name="Query"></param>
        /// <returns></returns>
        public List<T> List(string Query)
        {
            return SqlMaster.Get($"SELECT * FROM {this.TableName} WHERE {Query}").ToList();
        }

        public List<T> Where(Func<T, bool> Predict)
        {
            return ToList().Where(Predict).ToList();
        }
        /// <summary>
        /// A extension for linq's WHERE
        /// </summary>
        /// <param name="Predict">A dictionary for predict</param>
        /// <returns></returns>
        public List<T> Where((string, string) Predict)
        {
            return List($" [{Predict.Item1}]='{Predict.Item2}'").ToList();

        }

        private string GetType()
        {
            return new T().GetType().ToString().Split(".").LastOrDefault();
        }

        /// <summary>
        /// A method for adding a row
        /// </summary>
        /// <param name="entity">object we are adding</param>
        /// <exception cref="Exception"></exception>
        public int Add(T entity)
        {

            var Type = GetType();
            if (entity is IView)
            {
                throw new Exception($"type: {Type} can't be added or edited because it's just View");
            }
            if (entity is IUpdatable updatable)
            {
                updatable.CreatedDateTime = DateTime.Now;
                updatable.UpdatedDateTime = DateTime.Now;
                entity = (T)updatable;
            }

            var ListOfProperties = ObjectDescriber<T, int>
                                   .GetProperties(entity)
                                   .ToList();
            var PrimaryKeysDetails = ObjectDescriber<T, int>.GetPrimaryKeyDetails(entity).ToList();
           
             ListOfProperties.AddRange(PrimaryKeysDetails.Where(c => c.IsAutoIncremental == false).Select(c=>c.Name).ToList());
            ListOfProperties = ListOfProperties.Distinct().ToList();
            var Query = $@"INSERT INTO {this.TableName} 
                           (
                            {string.Join(",\n", ListOfProperties.Select(c => $"[{c}]").ToList())}
                            )
                            Values
                            (
                             {string.Join(",\n", ListOfProperties.Select(c => ObjectDescriber<T, int>.ToSqlFormat(entity, c)).ToList())}
                             )";

            SqlMaster.Execute(Query);

            var id = 0;
            var primaryKeys = ObjectDescriber<T, int>.GetPrimaryKeys(entity);
            entity = this.Query($"SELECT  {primaryKeys.FirstOrDefault()} FROM {this.TableName} ORDER BY {string.Join(",",primaryKeys)} DESC").FirstOrDefault();
            return ObjectDescriber<T, int>.GetValue(entity, primaryKeys.FirstOrDefault());
        }

        /// <summary>
        /// A method for editing a row
        /// </summary>
        /// <param name="entity">object we are editing</param>
        /// <exception cref="Exception"></exception>
        public void Update(T entity)
        {
            var Type = GetType();

            if (entity is IView)
            {
                throw new Exception($"type: {Type} can't be added or edited because it's just View");
            }
            if (entity is IUpdatable updatable)
            {
                updatable.UpdatedDateTime = DateTime.Now;
                entity = (T)updatable;
            }
            var ListOfProperties = ObjectDescriber<T, int>
               .GetProperties(entity ).ToList();

            var PrimaryKeys = ObjectDescriber<T, int>.GetPrimaryKeys(entity);
            var PrimaryKeysDetails = ObjectDescriber<T, int>.GetPrimaryKeyDetails(entity).ToList();
            ListOfProperties.AddRange(PrimaryKeysDetails.Where(c => c.IsAutoIncremental == false).Select(c => c.Name).ToList());
            ListOfProperties = ListOfProperties.Distinct().ToList();
             
            var Query = $@"UPDATE {this.TableName}
                           SET {string.Join(",\n", ListOfProperties.Select(c => $"[{c}]={ObjectDescriber<T, int>.ToSqlFormat(entity, c)}").ToList())}
                           WHERE {string.Join(" AND ", PrimaryKeys.Select(c => $" [{c}]= {ObjectDescriber<T, int>.ToSqlFormat(entity, c)}").ToList())}";

            SqlMaster.Execute(Query);
        }

        /// <summary>
        /// A method for removing a row
        /// </summary>
        /// <param name="entity">object we are removing</param>
        public void Remove(T entity)
        {
            var PrimaryKeys = ObjectDescriber<T, int>.GetPrimaryKeys(entity);

            var Query = $@"DELETE {this.TableName}  
                            WHERE {string.Join(" AND ", PrimaryKeys.Select(c => $" [{c}]= {ObjectDescriber<T, int>.ToSqlFormat(entity, c)}").ToList())}";
            SqlMaster.Execute(Query);
        }
    }
}
