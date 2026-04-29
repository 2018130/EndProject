using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectPool : MonoBehaviour
{
    public static SkillEffectPool Instance;

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();

            for (int i = 0; i < 5; i++)
            {
                CreateNewObject(prefab).SetActive(false);
            }
        }

        GameObject obj = null;

        while (poolDictionary[prefab].Count > 0)
        {
            GameObject pooledObj = poolDictionary[prefab].Dequeue();
            if (pooledObj != null)
            {
                obj = pooledObj;
                break;
            }
        }

        if (obj == null)
        {
            obj = CreateNewObject(prefab);
        }

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);

        return obj;
    }

    private GameObject CreateNewObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);

        PooledObject marker = obj.AddComponent<PooledObject>();
        marker.originalPrefab = prefab;

        poolDictionary[prefab].Enqueue(obj);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if(obj.TryGetComponent<PooledObject>(out var marker))
        {
            obj.SetActive(false);

            if(poolDictionary.ContainsKey(marker.originalPrefab))
            {
                poolDictionary[marker.originalPrefab].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }
}

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject originalPrefab;
}
