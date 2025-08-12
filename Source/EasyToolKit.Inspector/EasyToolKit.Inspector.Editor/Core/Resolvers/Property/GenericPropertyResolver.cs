using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EasyToolKit.Core;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    public class GenericPropertyResolver : PropertyResolver
    {
        private readonly List<InspectorPropertyInfo> _propertyInfos = new List<InspectorPropertyInfo>();

        protected override void Initialize()
        {
            var targetType = Property.ValueEntry.ValueType;
            var memberInfos = targetType.GetAllMembers(BindingFlagsHelper.AllInstance).Where(Filter).OrderBy(Order);

            foreach (var memberInfo in memberInfos)
            {
                if (memberInfo is MethodInfo methodInfo && !methodInfo.GetCustomAttributes().Any())
                {
                    continue;
                }

                if (memberInfo.IsDefined<HideInInspector>())
                {
                    continue;
                }

                var definedShowInInspector = memberInfo.IsDefined<ShowInInspectorAttribute>();
                if (memberInfo is FieldInfo fieldInfo)
                {
                    if (!InspectorPropertyInfoUtility.IsSerializableField(fieldInfo) && !definedShowInInspector)
                    {
                        continue;
                    }

                    if (!definedShowInInspector)
                    {
                        var memberType = memberInfo.GetMemberType();
                        if (!memberType.IsInheritsFrom<UnityEngine.Object>() &&
                            !memberType.IsValueType &&
                            !memberType.IsDefined<SerializableAttribute>())
                        {
                            continue;
                        }
                    }
                }

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    //TODO support property
                    continue;
                }


                _propertyInfos.Add(InspectorPropertyInfo.CreateForMember(memberInfo));
            }
        }

        protected override void Deinitialize()
        {
            _propertyInfos.Clear();
        }

        public override int ChildNameToIndex(string name)
        {
            return _propertyInfos.FindIndex(info => info.PropertyName == name);
        }

        public override int GetChildCount()
        {
            return _propertyInfos.Count;
        }

        public override InspectorPropertyInfo GetChildInfo(int childIndex)
        {
            return _propertyInfos[childIndex];
        }

        private int Order(MemberInfo arg1)
        {
            if (arg1 is FieldInfo) return 1;
            if (arg1 is PropertyInfo) return 2;
            if (arg1 is MethodInfo) return 3;
            return 4;
        }

        private bool Filter(MemberInfo member)
        {
            var targetType = Property.ValueEntry.ValueType;
            if (member.DeclaringType == typeof(object) && targetType != typeof(object)) return false;
            if (!(member is FieldInfo || member is PropertyInfo || member is MethodInfo)) return false;
            if (member is FieldInfo fieldInfo && fieldInfo.IsSpecialName) return false;
            if (member is MethodInfo methodInfo && methodInfo.IsSpecialName) return false;
            if (member is PropertyInfo propertyInfo && propertyInfo.IsSpecialName) return false;
            if (member.IsDefined<CompilerGeneratedAttribute>()) return false;

            return true;
        }
    }
}