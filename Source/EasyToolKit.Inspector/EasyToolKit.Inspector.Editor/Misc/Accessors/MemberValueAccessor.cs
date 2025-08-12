using System;
using System.Reflection;
using EasyToolKit.Core;
using EasyToolKit.ThirdParty.OdinSerializer.Utilities;

namespace EasyToolKit.Inspector.Editor
{
    public class MemberValueAccessor<TOwner, TValue> : ValueAccessor<TOwner, TValue>
    {
        private readonly MemberInfo _memberInfo;
        private readonly ValueGetter<TOwner, TValue> _getter;
        private readonly ValueSetter<TOwner, TValue> _setter;

        public MemberValueAccessor(MemberInfo memberInfo)
        {
            _getter = memberInfo.GetInstanceMemberValueGetter<TOwner, TValue>();
            _setter = memberInfo.GetInstanceMemberValueSetter<TOwner, TValue>();

            _memberInfo = memberInfo;
        }

        public override void SetValue(ref TOwner target, TValue collection)
        {
            _setter(ref target, collection);
        }

        public override TValue GetValue(ref TOwner target)
        {
            return _getter(ref target);
        }
    }
}
