using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonWithoutStay<T> : MonoBehaviour
    where T : Component
{
    private static T m_instance;
    public static T Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindFirstObjectByType<T>();
                if (m_instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    m_instance = obj.AddComponent<T>();
                }
            }
            return m_instance;
        }
    }
}
