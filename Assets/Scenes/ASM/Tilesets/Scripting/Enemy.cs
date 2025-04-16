using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Transform player;               // Gán Player từ Inspector
    public float speed = 2f;               // Tốc độ chạy
    public float attackRange = 1.5f;       // Khoảng cách tấn công
    public float attackCooldown = 1f;      // Thời gian giữa mỗi đòn
    private float lastAttackTime;
    Vector2 movement;
    Vector3 originalScale;
    public int health = 50;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            // Đuổi theo player
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

            // Animation chạy (nếu có)
            if (animator != null)
            {
                animator.SetBool("isRunning", true);
            }
        }
        else
        {
            // Dừng chạy
            if (animator != null)
            {
                animator.SetBool("isRunning", false);
            }

            // Tấn công nếu đủ thời gian
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
        if (movement.x > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (movement.x < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    void Attack()
    {
        // Gọi animation hoặc xử lý tấn công ở đây
        Debug.Log("Enemy attacks!");

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Gây damage cho player (nếu có)
    }
    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy took " + damage + " damage.");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died.");
        Destroy(gameObject);
    }
}
