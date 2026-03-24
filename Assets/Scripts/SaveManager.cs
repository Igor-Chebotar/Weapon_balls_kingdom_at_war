using UnityEngine;
using System.IO;

public static class SaveManager
{
    static string _path;
    static string path
    {
        get
        {
            if (_path == null)
                _path = Application.persistentDataPath + "/save.json";
            return _path;
        }
    }

    public static void SaveProfile(UserProfile profile)
    {
        string json = JsonUtility.ToJson(profile, true);
        File.WriteAllText(path, json);
    }

    public static UserProfile LoadProfile()
    {
        if (!File.Exists(path))
        {
            var p = new UserProfile();
            p.gold = 0;
            p.unlockedWeapons.Add("Sword");
            p.highestWave = 0;
            return p;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<UserProfile>(json);
    }

    public static void DeleteSave()
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
