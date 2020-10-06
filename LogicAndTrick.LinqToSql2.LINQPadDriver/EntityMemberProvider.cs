using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LINQPad;

namespace LogicAndTrick.LinqToSQL2.LINQPadDriver
{
    public class EntityMemberProvider : ICustomMemberProvider
    {
        private readonly object _object;
        private readonly List<EntityInfo> _infos;

        private static readonly Dictionary<Type, List<EntityInfo>> InfoCache = new Dictionary<Type, List<EntityInfo>>();

        private static List<EntityInfo> GetEntityInfo(Type type)
        {
            if (!InfoCache.ContainsKey(type)) InfoCache[type] = type.GetProperties().Select(x => new EntityInfo(x)).ToList();
            return InfoCache[type];
        }

        public static bool AppliesTo(object obj)
        {
            var type = obj.GetType();
            return InfoCache.ContainsKey(type) || type.GetCustomAttributesData().Any(x => x.AttributeType.Name == "TableAttribute");
        }

        public EntityMemberProvider(object obj)
        {
            _object = obj;
            var type = obj.GetType();
            _infos = GetEntityInfo(type);
        }

        public IEnumerable<string> GetNames()
        {
            return _infos.Select(x => x.Name);
        }

        public IEnumerable<Type> GetTypes()
        {
            return _infos.Select(x => x.Type);
        }

        public IEnumerable<object> GetValues()
        {
            return _infos.Select(x => x.GetValue(_object));
        }

        private class EntityInfo
        {
            private readonly PropertyInfo _pi;
            public string Name { get; }
            public Type Type { get; }
            public bool IsReference { get; }

            public EntityInfo(PropertyInfo pi)
            {
                _pi = pi;
                Name = pi.Name;
                Type = pi.PropertyType;
                var attr = pi.GetCustomAttributesData();
                var assoc = attr.FirstOrDefault(x => x.AttributeType.Name == "AssociationAttribute");
                IsReference = assoc != null;
            }

            public object GetValue(object obj)
            {
                if (IsReference)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(Type)) return Util.OnDemand(Name, () => (IEnumerable) _pi.GetValue(obj), true);
                    return Util.OnDemand(Name, () => _pi.GetValue(obj), true);
                }
                else
                {
                    return _pi.GetValue(obj);
                }
            }
        }
    }
}
