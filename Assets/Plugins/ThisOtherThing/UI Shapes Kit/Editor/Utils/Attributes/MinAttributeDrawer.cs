using UnityEditor;
using UnityEngine;
using MinAttribute = ThisOtherThing.Utils.MinAttribute;

namespace ThisOtherThing
{
    [CustomPropertyDrawer(typeof(MinAttribute))]
    public class MinDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = (MinAttribute)this.attribute;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    var valueI = EditorGUI.IntField(position, label, property.intValue);
                    property.intValue = Mathf.Max(valueI, attribute.minInt);
                    break;

                case SerializedPropertyType.Float:
                    var valueF = EditorGUI.FloatField(position, label, property.floatValue);
                    property.floatValue = Mathf.Max(valueF, attribute.minFloat);
                    break;
            }
        }
    }
}