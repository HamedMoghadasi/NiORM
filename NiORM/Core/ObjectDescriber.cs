﻿using NiORM.Attributes;
using System.Diagnostics;
using System.Reflection;

namespace NiORM.Core
{
    public static class ObjectDescriber<T, TValue> where T : new()
    {
        public static List<string> GetProperties(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            var objectType = entity.GetType();
            var Keys = GetPrimaryKeyDetails(entity);
            var ListOfPropertyInfo = new List<PropertyInfo>(objectType.GetProperties()).ToList();

            var properties = ListOfPropertyInfo.Select(c => c.Name);
            
            return properties.ToList();
        }

        public static List<string> GetPrimaryKeys(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity)); 

            return entity.GetType().GetProperties()
              .Where(property =>
              property.GetCustomAttributes(true)
              .Any(customeAttribute => customeAttribute is PrimaryKey)).Select(c=>c.Name).ToList();

        }
        public static List<PrimaryKeyDetails> GetPrimaryKeyDetails(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            return entity.GetType().GetProperties()
              .SelectMany(property =>
              property.GetCustomAttributes(true)
              .Where(customeAttribute => customeAttribute is PrimaryKey)
              .Select(customeAttribute => new PrimaryKeyDetails(property.Name, ((PrimaryKey)customeAttribute).IsAutoIncremental))).ToList();

        }

        public static string GetTableName(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            var entityType = entity.GetType();

            try
            {
                var attributes = Attribute.GetCustomAttributes(entityType);
                foreach (var attribute in attributes)
                {
                    if (attribute is TableName)
                    {
                        return ((TableName)attribute).Name;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            throw new Exception($"class '{entityType.Name}' should have attribute 'TableName'");
        }

        public static string ToSqlFormat(T entity, string Key)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            PropertyInfo propertyInfo = entity.GetType().GetProperty(Key) ?? throw new ArgumentNullException(nameof(entity));
            var Value = propertyInfo.GetValue(entity, null);
            return ConvertToSqlFormat(Value);
        }

        public static TValue GetValue(T entity, string Key)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            PropertyInfo propertyInfo = entity.GetType().GetProperty(Key) ?? throw new ArgumentNullException(nameof(entity));
            var propertyInfoValue = propertyInfo.GetValue(entity, null) ?? throw new ArgumentNullException(nameof(entity));
            return (TValue)propertyInfoValue;
        }

        public static void SetValue(T entity, string Key, TValue Value)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (Value is null) throw new ArgumentNullException(nameof(entity));

            try
            {
                PropertyInfo propertyInfo = entity.GetType().GetProperty(Key) ?? throw new ArgumentNullException(nameof(entity));
                var propertyType = propertyInfo.PropertyType;

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Value.ToString() == string.Empty)
                {
                    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? throw new ArgumentNullException(nameof(entity));
                    var underlyingTypeCode = GetTypeCode(underlyingType);
                    switch (underlyingTypeCode)
                    {
                        case TypeCode.Int32:
                            propertyInfo.SetValue(entity, (int?)null);
                            return;
                        case TypeCode.Double:
                            propertyInfo.SetValue(entity, (double?)null);
                            return;
                        case TypeCode.DateTime:
                            propertyInfo.SetValue(entity, (DateTime?)null);
                            return;
                        case TypeCode.Int64:
                            propertyInfo.SetValue(entity, (long?)null);
                            return;
                        case TypeCode.Single:
                            propertyInfo.SetValue(entity, (float?)null);
                            return;
                        case TypeCode.String:
                            propertyInfo.SetValue(entity, null);
                            return;
                        case TypeCode.Boolean:
                            propertyInfo.SetValue(entity, (bool?)null);
                            return;
                        case TypeCode.Object:
                            propertyInfo.SetValue(entity, null);
                            return;
                        default:
                            propertyInfo.SetValue(entity, Value);
                            return;
                    }
                }
                else
                {
                    var type = GetTypeCode(propertyType);
                    switch (type)
                    {
                        case TypeCode.Int32:
                            propertyInfo.SetValue(entity, int.Parse(Value.ToString()));
                            return;
                        case TypeCode.Double:
                            propertyInfo.SetValue(entity, double.Parse(Value.ToString()));
                            return;
                        case TypeCode.DateTime:
                            propertyInfo.SetValue(entity, DateTime.Parse(Value.ToString()));
                            return;
                        case TypeCode.Int64:
                            propertyInfo.SetValue(entity, long.Parse(Value.ToString()));
                            return;
                        case TypeCode.Single:
                            propertyInfo.SetValue(entity, float.Parse(Value.ToString()));
                            return;
                        case TypeCode.String:
                            propertyInfo.SetValue(entity, (Value.ToString()));
                            return;
                        case TypeCode.Boolean:
                            propertyInfo.SetValue(entity, (Value.ToString() == "True"));
                            return;
                        case TypeCode.Object:
                            propertyInfo.SetValue(entity, Value);
                            return;
                        default:
                            propertyInfo.SetValue(entity, Value);
                            return;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public static TypeCode GetTypeCode(Type type)
        {
            if (type == typeof(Enum)) return TypeCode.Int32;
            return Type.GetTypeCode(type);
        }

        private static string ConvertToSqlFormat(object? Value)
        {
            if (Value == null)
                return "null";
            if (Value is string)
                return $"N'{Value}'";
            if (Value is int | Value is float | Value is long | Value is double)
                return Value.ToString();
            if (Value is DateTime time)
                return $"'{time:yyyy-MM-dd HH:mm:ss.ss}'";
            if (Value is bool value)
                return value ? "1" : "0";

            return string.Empty;
        }
    }
}
