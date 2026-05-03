using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private BoxCollider2D attackCollider;

    private readonly HashSet<Enemy1> hitEnemies = new HashSet<Enemy1>();
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

        Enemy1 enemy = other.GetComponentInParent<Enemy1>();
        if (enemy == null || hitEnemies.Contains(enemy))
        {
            return;
        }

        hitEnemies.Add(enemy);
        enemy.RecibirDanio(damage);
    }
}