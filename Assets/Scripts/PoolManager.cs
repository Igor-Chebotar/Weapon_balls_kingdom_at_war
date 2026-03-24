using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [System.Serializable]
    public class PoolEntry
    {
        public string tag;
        public GameObject prefab;
        public int startSize = 5;
    }

    [SerializeField] List<PoolEntry> pools;

    Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // инициализация пулов
        foreach (var p in pools)
        {
            prefabLookup[p.tag] = p.prefab;
            var q = new Queue<GameObject>();
            for (int i = 0; i < p.startSize; i++)
            {
                var obj = Instantiate(p.prefab);
                obj.SetActive(false);
                q.Enqueue(obj);
            }
            poolDict[p.tag] = q;
        }
    }

    public GameObject SpawnObject(string tag, Vector2 pos)
    {
        // ленивое создание если пул кончился или не было
        if (!poolDict.ContainsKey(tag))
        {
            if (prefabLookup.ContainsKey(tag))
            {
                poolDict[tag] = new Queue<GameObject>();
            }
            else
            {
                return null;
            }
        }

        GameObject obj;
        if (poolDict[tag].Count > 0)
        {
            obj = poolDict[tag].Dequeue();
        }
        else
        {
            // докинуть если кончились
            if (!prefabLookup.ContainsKey(tag)) return null;
            obj = Instantiate(prefabLookup[tag]);
        }

        obj.transform.position = pos;
        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        // чистим имя
        string cleanName = obj.name.Replace("(Clone)", "").Trim();

        if (!poolDict.ContainsKey(cleanName))
            poolDict[cleanName] = new Queue<GameObject>();

        poolDict[cleanName].Enqueue(obj);
    }
}
