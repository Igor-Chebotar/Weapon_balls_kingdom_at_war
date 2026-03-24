using UnityEngine;

public enum EnemyType { Knight, HeavyKnight, King }

[CreateAssetMenu(fileName = "NewEnemy", menuName = "WeaponBalls/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public EnemyType type;
    public float maxHealth = 5f;
    public float moveSpeed = 0f;
    public float attackDamage = 1f;
    public float mass = 1.5f;
    public Color color = Color.gray;
    public float stunResistance = 8f;
}
