using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using QFSW.QC;

namespace Perditio
{
    public class Utils
    {
        public static void LogEntireFucker(Transform child_transform, int depth = 0)
        {
            string padding = "";
            for (int i = 0; i <= depth; i++)
            {
                padding += '\t';
            }

            Debug.Log(string.Format("{0}name: {1}", padding, child_transform.name));

            if (depth >= 10)
            {
                return;
            }

            foreach (Transform child_child in child_transform)
            {
                LogEntireFucker(child_child, depth + 1);
            }
        }

        public static void LogEntireScene(GameObject any_gameobject)
        {
            UnityEngine.SceneManagement.Scene scene = any_gameobject.scene;
            Debug.Log(string.Format("Scene {0}:", scene.name));
            foreach (GameObject root_child in scene.GetRootGameObjects())
            {
                LogEntireFucker(root_child.transform);
            }
        }

        public static void LogQuantumConsole(string message)
        {
            Debug.Log($"Perditio LogQuantumConsole: {message}");
            try
            {
                QuantumConsole.Instance.LogToConsole(message);
            }
            catch (Exception e)
            { 
                //Debug.Log($"Perditio Quantum Console Failed With: {e.ToString()}");
            }
        }

        public static MethodInfo GetPrivateMethod(object obj, string method_name)
        {
            return obj.GetType().GetMethod(method_name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        public static T GetPrivateValue<T>(object obj, string field_name)
        {
            return (T)obj.GetType().GetField(field_name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        }

        public static void SetPrivateValue(object obj, string field_name, object new_value)
        {
            obj.GetType().GetField(field_name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, new_value);
        }

        public static void SetPrivateValue(object obj, string fieldName, object new_value, Type type)
        {
            type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, new_value);
        }
    }
}
