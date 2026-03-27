using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour 
{
    private static T instance;
    public static T Instance => instance;

    protected virtual void Awake()
    {
        T typeOfClass = FindAnyObjectByType<T>();

        if (typeOfClass == this || typeOfClass == null)
        {
            Debug.Log($"{typeof(T)} singleton created");
            T targetTypeObj = GetComponent<T>();
            instance = targetTypeObj;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(targetTypeObj.gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
