using System;
using System.Collections.Generic;
using System.Reflection;



namespace Doot.Tests
{
    class PrivateObject
    {
        readonly object target;
        readonly Type targetType;
        readonly Dictionary<string, FieldInfo> cachedFields;

        public PrivateObject(object target)
        {
            this.target = target;
            targetType = target.GetType();
            cachedFields = new Dictionary<string, FieldInfo>();
        }

        public T GetField<T>(string name)
        {
            if (!cachedFields.TryGetValue(name, out FieldInfo field))
            {
                field = targetType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                cachedFields[name] = field ?? throw new ArgumentException($"Field '{name}' not found in type '{targetType}'");
            }

            return (T)field.GetValue(target);
        }
    }
}
