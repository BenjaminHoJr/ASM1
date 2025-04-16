using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed = 5f;
    public Rigidbody2D rb;
    public Animator anim;
    Vector2 movement;
    public SpriteRenderer sprite;
    Vector3 originalScale;
    public Animator animator; // Gắn animator nếu có animation
    public float attackRange = 1f;
    public int attackDamage = 10;
    public LayerMask enemyLayers;
    public Transform attackPoint;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        anim.SetFloat("Horizontal", movement.x);
        anim.SetFloat("Vertical", movement.y);
        anim.SetFloat("Speed", movement.sqrMagnitude);

        anim.SetFloat("Speed", Mathf.Abs(movement.x));

        if (movement.x > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (movement.x < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }
    }
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }
    void Attack()
    {
        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Phát hiện kẻ địch trong phạm vi tấn công
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Gửi sát thương cho địch
            enemy.GetComponent<Enemy>().TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Vẽ phạm vi tấn công trong Editor
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
