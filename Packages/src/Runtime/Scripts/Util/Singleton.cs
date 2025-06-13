using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M.CodeEditor
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType(typeof(T)) as T;

                    GameObject obj = null;
                    if (instance == null)
                    {
                        obj = new GameObject();
                        instance = obj.AddComponent<T>();
                    }
                    else
                    {
                        obj = instance.gameObject;
                    }

                    DontDestroyOnLoad(obj);
                    obj.name = typeof(T).ToString() + "(Singleton)";
                }

                return instance;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            instance = null;
        }
    }
}
