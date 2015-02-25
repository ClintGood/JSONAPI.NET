﻿using JSONAPI.Attributes;
using JSONAPI.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JSONAPI.Extensions;

namespace JSONAPI.Core
{
    public class ModelManager : IModelManager
    {
        public ModelManager(IPluralizationService pluralizationService)
        {
            _pluralizationService = pluralizationService;
        }

        protected IPluralizationService _pluralizationService = null;
        public IPluralizationService PluralizationService
        {
            get
            {
                return _pluralizationService;
            }
        }

        #region Cache storage

        protected Lazy<Dictionary<Type, PropertyInfo>> _idProperties
            = new Lazy<Dictionary<Type,PropertyInfo>>(
                () => new Dictionary<Type, PropertyInfo>()
            );

        protected Lazy<Dictionary<Type, Dictionary<string, PropertyInfo>>> _propertyMaps
            = new Lazy<Dictionary<Type, Dictionary<string, PropertyInfo>>>(
                () => new Dictionary<Type, Dictionary<string, PropertyInfo>>()
            );

        protected Lazy<Dictionary<Type, string>> _resourceTypeNamesByType
            = new Lazy<Dictionary<Type, string>>(
                () => new Dictionary<Type, string>()
            );

        protected Lazy<Dictionary<string, Type>> _typesByResourceTypeName
            = new Lazy<Dictionary<string, Type>>(
                () => new Dictionary<string, Type>()
            );

        protected Lazy<Dictionary<Type, bool>> _isSerializedAsMany
            = new Lazy<Dictionary<Type, bool>>(
                () => new Dictionary<Type, bool>()
            );

        protected Lazy<Dictionary<Type, Type>> _getElementType
            = new Lazy<Dictionary<Type, Type>>(
                () => new Dictionary<Type, Type>()
            );

        #endregion

        #region Id property determination

        public PropertyInfo GetIdProperty(Type type)
        {
            PropertyInfo idprop = null;

            var idPropCache = _idProperties.Value;

            lock (idPropCache)
            {
                if (idPropCache.TryGetValue(type, out idprop)) return idprop;

                // First, look for UseAsIdAttribute
                idprop = type.GetProperties()
                .Where(p => p.CustomAttributes.Any(attr => attr.AttributeType == typeof(UseAsIdAttribute)))
                .FirstOrDefault();
                if (idprop == null)
                {
                    idprop = type.GetProperty("Id");
                }

                if (idprop == null)
                    throw new InvalidOperationException(String.Format("Unable to determine Id property for type {0}", type));

                idPropCache.Add(type, idprop);
            }

            return idprop;
        }

        #endregion

        #region Property Maps

        protected IDictionary<string, PropertyInfo> GetPropertyMap(Type type)
        {
            Dictionary<string, PropertyInfo> propMap = null;

            var propMapCache = _propertyMaps.Value;

            lock (propMapCache)
            {
                if (propMapCache.TryGetValue(type, out propMap)) return propMap;

                propMap = new Dictionary<string, PropertyInfo>();
                PropertyInfo[] props = type.GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    propMap[GetJsonKeyForProperty(prop)] = prop;
                }

                propMapCache.Add(type, propMap);
            }

            return propMap;
        }

        public PropertyInfo[] GetProperties(Type type)
        {
            return GetPropertyMap(type).Values.ToArray();
        }

        public PropertyInfo GetPropertyForJsonKey(Type type, string jsonKey)
        {
            PropertyInfo propInfo;
            if (GetPropertyMap(type).TryGetValue(jsonKey, out propInfo)) return propInfo;
            else return null; // Or, throw an exception here??
        }

        #endregion

        public string GetResourceTypeNameForType(Type type)
        {
            if (IsSerializedAsMany(type))
                type = GetElementType(type);

            var currentType = type;
            while (currentType != null && currentType != typeof(Object))
            {
                string resourceTypeName;
                if (_resourceTypeNamesByType.Value.TryGetValue(currentType, out resourceTypeName)) return resourceTypeName;

                // This particular type wasn't registered, but maybe the base type was
                currentType = currentType.BaseType;
            }

            throw new InvalidOperationException(String.Format("The type `{0}` was not registered.", type.FullName));
        }

        public Type GetTypeByResourceTypeName(string resourceTypeName)
        {
            Type type;
            if (_typesByResourceTypeName.Value.TryGetValue(resourceTypeName, out type)) return type;

            throw new InvalidOperationException(String.Format("The resource type name `{0}` was not registered.", resourceTypeName));
        }

        /// <summary>
        /// Registers a type with this ModelManager.
        /// </summary>
        /// <param name="type">The type to register.</param>
        public void RegisterResourceType(Type type)
        {
            var resourceTypeName = CalculateResourceTypeNameForType(type);
            RegisterResourceType(type, resourceTypeName);
        }

        /// <summary>
        /// Registeres a type with this ModelManager, using a default resource type name.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="resourceTypeName">The resource type name to use</param>
        public void RegisterResourceType(Type type, string resourceTypeName)
        {
            lock (_resourceTypeNamesByType.Value)
            {
                lock (_typesByResourceTypeName.Value)
                {
                    if (_resourceTypeNamesByType.Value.ContainsKey(type))
                        throw new InvalidOperationException(String.Format("The type `{0}` has already been registered.",
                            type.FullName));

                    if (_typesByResourceTypeName.Value.ContainsKey(resourceTypeName))
                        throw new InvalidOperationException(
                            String.Format("The resource type name `{0}` has already been registered.", resourceTypeName));

                    _resourceTypeNamesByType.Value[type] = resourceTypeName;
                    _typesByResourceTypeName.Value[resourceTypeName] = type;
                }
            }
        }

        /// <summary>
        /// Determines the resource type name for a given type.
        /// </summary>
        /// <param name="type">The type to calculate the resouce type name for</param>
        /// <returns>The type's resource type name</returns>
        protected virtual string CalculateResourceTypeNameForType(Type type)
        {
            var attrs = type.CustomAttributes.Where(x => x.AttributeType == typeof(Newtonsoft.Json.JsonObjectAttribute)).ToList();

            string title = type.Name;
            if (attrs.Any())
            {
                var titles = attrs.First().NamedArguments.Where(arg => arg.MemberName == "Title")
                    .Select(arg => arg.TypedValue.Value.ToString()).ToList();
                if (titles.Any()) title = titles.First();
            }

            return FormatPropertyName(PluralizationService.Pluralize(title)).Dasherize();
        }

        public string GetJsonKeyForProperty(PropertyInfo propInfo)
        {
            return FormatPropertyName(propInfo.Name);
            //TODO: Respect [JsonProperty(PropertyName = "FooBar")], and probably cache the result.
        }

        protected static string FormatPropertyName(string propertyName)
        {
            string result = propertyName.Substring(0, 1).ToLower() + propertyName.Substring(1);
            return result;
        }

        public bool IsSerializedAsMany(Type type)
        {
            bool isMany;

            var isManyCache = _isSerializedAsMany.Value;

            lock (isManyCache)
            {
                if (isManyCache.TryGetValue(type, out isMany)) return isMany;

                isMany =
                    type.IsArray ||
                    (type.GetInterfaces().Contains(typeof(System.Collections.IEnumerable)) && type.IsGenericType);

                isManyCache.Add(type, isMany);
            }

            return isMany;
        }

        public Type GetElementType(Type manyType)
        {
            Type etype = null;

            var etypeCache = _getElementType.Value;

            lock (etypeCache)
            {
                if (etypeCache.TryGetValue(manyType, out etype)) return etype;

                if (manyType.IsGenericType)
                    etype = manyType.GetGenericArguments()[0];
                else
                    etype = manyType.GetElementType();

                etypeCache.Add(manyType, etype);
            }

            return etype;
        }

    }
}
