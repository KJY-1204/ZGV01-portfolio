// EditMode 테스트에서 컴포넌트 생명주기와 직렬화 필드를 다루는 도우미다.
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TestUtil
{
    // EditMode에서는 AddComponent가 Awake를 부르지 않으므로 직접 호출한다.
    public static void Awake(Component component)
    {
        component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);
    }

    // EditMode에서는 OnEnable/OnDisable도 자동 호출되지 않으므로 이름으로 직접 호출한다.
    public static void Call(Component component, string method)
    {
        component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(component, null);
    }

    public static void SetEnum(Object target, string field, int index)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).enumValueIndex = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetObject(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
