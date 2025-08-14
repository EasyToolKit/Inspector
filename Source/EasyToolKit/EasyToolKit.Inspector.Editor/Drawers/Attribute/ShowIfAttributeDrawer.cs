using EasyToolKit.Core;
using EasyToolKit.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    [DrawerPriority(DrawerPriorityLevel.Super + 10000)]
    public class ShowIfAttributeDrawer : EasyAttributeDrawer<ShowIfAttribute>
    {
        private ICodeValueResolver _conditionResolver;

        protected override void Initialize()
        {
            var targetType = this.GetTargetTypeForResolver();

            _conditionResolver = CodeValueResolver.CreateWeak(Attribute.Condition, null, targetType);
        }

        protected override void DrawProperty(GUIContent label)
        {
            if (_conditionResolver.HasError(out var error))
            {
                EasyEditorGUI.MessageBox(error, MessageType.Error);
                return;
            }

            var resolveTarget = this.GetTargetForResolver();
            var condition = _conditionResolver.ResolveWeak(resolveTarget);
            var value = Attribute.Value;
            var show = Equals(condition, value);

            if (show)
            {
                CallNextDrawer(label);
            }
        }
    }
}
