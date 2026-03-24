using System;
using System.Collections.Generic;

[Serializable]
public class UserProfile
{
    public int gold;
    public List<string> unlockedWeapons = new List<string>();
    public int highestWave;
}
