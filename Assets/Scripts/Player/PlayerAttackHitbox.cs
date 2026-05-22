using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private BoxCollider2D attackCollider;

    private readonly HashSet<Component> hitEnemies = new HashSet<Component>();
    private bool attackActive;

    private void Awake()
    {
        if (attackCollider == null)
        {
            attackCollider = GetComponent<BoxCollider2D>();
        }

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    public void BeginAttack()
    {
        attackActive = true;
        hitEnemies.Clear();

        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
    }

    public void EndAttack()
    {
        attackActive = false;
        hitEnemies.Clear();

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnDisable()
    {
        EndAttack();
    }

    private void TryHit(Collider2D other)
    {
        if (!attackActive || other == null)
        {
            return;
        }

        GameObject enemyRoot = other.transform.root.gameObject;
        if (!other.CompareTag(enemyTag) && !enemyRoot.CompareTag(enemyTag))
        {
            return;
        }

        Enemy1 enemy1 = other.GetComponentInParent<Enemy1>();
        Enemy2 enemy2 = other.GetComponentInParent<Enemy2>();

        if (enemy1 == null && enemy2 == null)
        {
            return;
        }

        Component targetEnemy = (Component)enemy1 ?? (Component)enemy2;
        if (hitEnemies.Contains(targetEnemy))
        {
            return;
        }

        hitEnemies.Add(targetEnemy);

        if (enemy1 != null)
        {
            enemy1.RecibirDanio(damage);
        }
        else if (enemy2 != null)
        {
            enemy2.RecibirDanio(damage);
        }
    }
}