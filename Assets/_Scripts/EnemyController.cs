using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private enum EnemyType 
    {
        Bird,
        Humanoid
    };
    private enum AttackList 
    {
        Close,
        Range,
        Collision
    };

    [Header("Components")]
    private Rigidbody2D _rigidBody;
    private Animator _animator;
    [SerializeField] private GameObject _projectile;

    [Header("GameObjects")]
    private CinemachineVirtualCamera cinemachine;
    private PlayerController player;
    private Rigidbody2D player_rb;
    [SerializeField] private LayerMask playerMask;
    private GameManager gameManager;
    private HealthBar healthbar;

    [Header("Statistics")]
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] public float health = 9;
    [SerializeField] private int damage = 2;
    [SerializeField] private float awakeDistance = 6f;
    [SerializeField] private Vector2 enemyHead = new(0, 0.75f);
    [SerializeField] private Vector2 headSize = new(1.5f, 0.5f);
    [SerializeField] private float attackRange = 1.3f;
    //[SerializeField] private float attackRangeOffset = 0.7f;
    [SerializeField] private EnemyType type = EnemyType.Bird;
    [SerializeField] private AttackList attackType = AttackList.Close;
    [SerializeField] private float attackDelay = 1.5f;
    //[SerializeField] private float damageDelay = 1f;

    [Header("Conditionals")]
    [SerializeField] private bool isAwake;
    [SerializeField] private bool headKill;
    [SerializeField] private bool isOnHead;
    [SerializeField] private bool isDead;
    [SerializeField] private bool receiveDamage;
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isInView;

    private void Awake()
    {
        cinemachine = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        player_rb = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        gameManager = FindObjectOfType<GameManager>();
        healthbar = GetComponentInChildren<HealthBar>();
        healthbar.SetMaxHealth(health);
    }

    void Update()
    {
        Vector2 direction = player.transform.position - transform.position;
        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (isDead)
        {
            if(type == EnemyType.Bird)
            {
                _rigidBody.gravityScale = 2;
                _rigidBody.velocity = Vector2.down *3;
            }
            else if(type == EnemyType.Humanoid)
            {
                _rigidBody.velocity = Vector2.zero;
            }
        }
        else
        {
            if (distance <= awakeDistance)
            {
                if (type == EnemyType.Bird)
                    _rigidBody.velocity = direction.normalized * movementSpeed;
                else if(type == EnemyType.Humanoid)
                {
                    bool isInAttackRange = Physics2D.OverlapCircle(transform.position, attackRange, playerMask);
                    if (isInAttackRange)
                    {
                        _rigidBody.velocity = new Vector2(0, _rigidBody.velocity.y);
                        _animator.SetBool("Run", false);


                        if (attackType == AttackList.Range)
                            ValidatePlayerIsInView(direction);

                        if (canAttack)
                        {
                            isAttacking = true;
                            if(attackType == AttackList.Range && isInView)
                            {
                                Invoke(nameof(EnemyAttack), 0.2f);
                            }
                            else if(attackType == AttackList.Close)
                                Invoke(nameof(EnemyAttack), 0.2f);
                        }
                    }
                    else if(!isAttacking && !isInAttackRange)
                    {
                        _animator.SetBool("Run", true);
                        _rigidBody.velocity = new Vector2(direction.normalized.x * movementSpeed, _rigidBody.velocity.y) ;
                    } 
                    else if(isAttacking && !isInAttackRange)
                    {
                        _rigidBody.velocity = new Vector2(0, _rigidBody.velocity.y);
                    }

                }
            }
            else
            {
                if (type == EnemyType.Humanoid)
                {
                    _rigidBody.velocity = new Vector2(0, _rigidBody.velocity.y);
                    _animator.SetBool("Run", false);
                } 
                else
                {
                    _rigidBody.velocity = Vector2.zero;
                }
            }
            ChangeDirection(direction.x);
        }
    }

    /// <summary>
    /// Change enemy direction toward player
    /// </summary>
    /// <param name="directionX">direction toward player</param>
    private void ChangeDirection(float directionX)
    {
        if (directionX >= 0 && transform.eulerAngles.y == 180) //Look right
        {
            transform.eulerAngles = Vector3.up * 0;
        }
        else if (directionX <= 0 && transform.eulerAngles.y == 0) //Look left
        {
            transform.eulerAngles = Vector3.up * 180;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if(headKill)
                isOnHead = Physics2D.OverlapBox((Vector2)transform.position + enemyHead, headSize, 0f, playerMask);

            if (isOnHead && headKill)
                Destroy(gameObject);
            else if(attackType == AttackList.Collision)
            {
                DealPlayerDamage();
            }
        }

        if (collision.gameObject.CompareTag("Ground") && isDead)
        {
            if(type == EnemyType.Bird)
            {
                _rigidBody.gravityScale = 0;
                gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
                Invoke("DestroyEnemy", 2f);
                this.enabled = false;
            }
        }
        
    }

    /// <summary>
    /// Deal player damage, add force to player and enemy in opposite directions depending on attack type
    /// </summary>
    public void DealPlayerDamage()
    {
        //int enemyForce = 7000;
        //if (type != EnemyType.Bird)
        //    enemyForce = 3000;

        //int playerForce = 400;
        //if (player.isOnGround)
        //    playerForce = 2000;

        //if (attackType != AttackList.Range)
        //{
        //    _rigidBody.AddForce((transform.position - player.transform.position).normalized * enemyForce, ForceMode2D.Force);
        //    player_rb.AddForce((player.transform.position - transform.position).normalized * playerForce, ForceMode2D.Force);
        //}

        gameManager.ReceiveDamage(damage);
    }

    /// <summary>
    /// Deal enemy damage, validate if health health and call destroy if <= 0
    /// </summary>
    /// <param name="damage">Damage to deal to enemy</param>
    public void ReceiveDamage(float damage)
    {
        Debug.Log("Damage");
        health -= damage;
        healthbar.SetHealthBarValue(health);
        StartCoroutine(DamageAnimation());
        if (health <= 0)
        {
            isDead = true;
            _animator.SetTrigger("Die");

            if (type == EnemyType.Humanoid)
            {
                _rigidBody.gravityScale = 0;
                gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
                Invoke("DestroyEnemy", 2f);
                this.enabled = false;
            }
        } 
        else
        {
            //_rigidBody.AddForce((transform.position - player.transform.position).normalized * 7000, ForceMode2D.Force);

            //receiveDamage = true;
            //_animator.SetBool("ReceiveDamage", true);
            //_animator.SetTrigger("Damage");
            //Invoke("StopReceiveDamage", damageDelay);
        }
    }

    /// <summary>
    /// Enable and disable renderer to create damage animation
    /// </summary>
    private IEnumerator DamageAnimation()
    {
        float delay = 0.1f;
        var gamerenderer = GetComponent<Renderer>();
        gamerenderer.enabled = false;
        yield return new WaitForSeconds(delay);
        gamerenderer.enabled = true;
        yield return new WaitForSeconds(delay);
        gamerenderer.enabled = false;
        yield return new WaitForSeconds(delay);
        gamerenderer.enabled = true;
    }

    /// <summary>
    /// Destroy enemy from game
    /// </summary>
    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Change enemy receiving damage bool
    /// </summary>
    //private void StopReceiveDamage()
    //{
    //    Debug.Log("Stop Damage");
    //    _animator.SetBool("ReceiveDamage", false);
    //    receiveDamage = false;
    //}

    /// <summary>
    /// Start enemy attack animation
    /// </summary>
    private void EnemyAttack()
    {
        canAttack = false;
        _animator.SetBool("Attack", true);
    }

    /// <summary>
    /// Stop enemy attack animation and wait attackDelay to enable attack
    /// </summary>
    private void StopAttack()
    {
        _animator.SetBool("Attack", false);
        isAttacking = false;
        Invoke(nameof(EnableAttack), attackDelay);
    }

    /// <summary>
    /// Change enemy canAttack status
    /// </summary>
    private void EnableAttack()
    {
        canAttack = true;
    }

    /// <summary>
    /// Instantiate ranger projectile in screen
    /// </summary>
    private void EnableRangeAttack()
    {
        var newArrow = Instantiate(_projectile, gameObject.transform.position, _projectile.transform.rotation);
        var direction = player.transform.position - transform.position;
        newArrow.GetComponent<ProjectileController>().ShootProjectile(direction, "Enemy", damage);
    }


    /// <summary>
    /// Validate if player is in field of view
    /// </summary>
    /// <param name="direction">Direction from enemy to player</param>
    private void ValidatePlayerIsInView(Vector2 direction)
    {
        RaycastHit2D rangerRayCast = Physics2D.Raycast(transform.position, direction, awakeDistance);

        Debug.DrawLine(transform.position, player.transform.position, Color.green);
        if (rangerRayCast.collider.CompareTag("Player"))
            isInView = true;
        else
            isInView = false;
    }

    private void OnDrawGizmosSelected()
    {
        if(headKill)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube((Vector2)transform.position + enemyHead, headSize);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, awakeDistance);

        if(attackType == AttackList.Close || attackType == AttackList.Range)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        
    }

}
