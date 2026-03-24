using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "WeaponBalls/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string Name;
    public WeaponType Type;
    public Sprite VisualSprite; // пока null
    public float Mass = 1f;
    public float BaseDamage = 10f;
    public GameObject ProjectilePrefab;
    public float AttackRate = 0.5f;
}
