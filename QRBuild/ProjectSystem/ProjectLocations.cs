using System;
using System.Collections.Generic;
using System.Reflection;

namespace QRBuild.ProjectSystem
{
    public abstract class ProjectLocations
    {
        public IDictionary<string, string> GetLocations()
        {
            Dictionary<string, string> result = new Dictionary<string,string>();

            Type type = GetType();
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Instance);
            foreach (var fieldInfo in fields) {
                if (fieldInfo.FieldType == typeof(System.String)) {
                    object valueObject = fieldInfo.GetValue(this);
                    string value = valueObject as string;
                    if (value != null) {
                        result[fieldInfo.Name] = value;
                    }
                }
            }

            return result;
        }
    }
}
