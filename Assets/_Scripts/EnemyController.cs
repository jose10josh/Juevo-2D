using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private enum EnemyType // your custom enumeration
    {
        Bird,
        Humanoid
    };
    private enum AttackList // your custom enumeration
    {
        Close,
        Range,
        Collision
    };

    [Header("Components")]
    private Rigidbody2D _rigidBody;
    private Animator _animator;

    [Header("GameObjects")]
    private CinemachineVirtualCamera cinemachine;
    private PlayerController player;
    private Rigidbody2D player_rb;
    [SerializeField] private LayerMask playerMask;
    private GameManager gameManager;

    [Header("Statistics")]
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float health = 9;
    [SerializeField] private int damage = 2;
    [SerializeField] private float awakeDistance = 6f;
    [SerializeField] private Vector2 enemyHead = new(0, 0.75f);
    [SerializeField] private Vector2 headSize = new(1.5f, 0.5f);
    [SerializeField] private float attackRange = 1.3f;
    //[SerializeField] private float attackRangeOffset = 0.7f;
    [SerializeField] private EnemyType type = EnemyType.Bird;
    [SerializeField] private AttackList attackType = AttackList.Close;
    [SerializeField] private float attackDelay = 1.5f;

    [Header("Conditionals")]
    [SerializeField] private bool isAwake;
    [SerializeField] private bool headKill;
    [SerializeField] private bool isOnHead;
    [SerializeField] private bool isDead;
    [SerializeField] private bool receiveDamage;
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isAttacking;

    private void Awake()
    {
        cinemachine = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        _rigidBody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        player_rb = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        Vector2 direction = player.transform.position - transform.position;
        float distance = Vector2.Distance(transform.position, player.transform.position);

        if(isDead)
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
            if (distance < awakeDistance && !receiveDamage)
            {
                if (type == EnemyType.Bird)
                    _rigidBody.velocity = direction.normalized * movementSpeed;
                else if(type == EnemyType.Humanoid)
                {
                    bool isInRange = Physics2D.OverlapCircle(transform.position, attackRange, playerMask);
                    if (isInRange)
                    {
                        _rigidBody.velocity = Vector2.zero;
                        _animator.SetBool("Run", false);

                        if (canAttack)
                        {
                            isAttacking = true;
                            Invoke("HumanoidAttack", 0.2f);
                        }
                    }
                    else if(!isAttacking && !isInRange)
                    {
                        _animator.SetBool("Run", true);
                        _rigidBody.velocity = new Vector2(direction.normalized.x * movementSpeed, _rigidBody.velocity.y) ;
                    } 
                    else if(isAttacking && !isInRange)
                    {
                        _rigidBody.velocity = Vector2.zero;
                    }

                }
            }
            else
            {
                _rigidBody.velocity = Vector2.zero;
                if (type == EnemyType.Humanoid)
                {
                    _animator.SetBool("Run", false);
                }
            }
            ChangeDirection(direction.x);
        }
    }

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
    public void DealPlayerDamage()
    {
        int enemyForce = 7000;
        if (type != EnemyType.Bird)
            enemyForce = 3000;

        _rigidBody.AddForce((transform.position - player.transform.position).normalized * enemyForce, ForceMode2D.Force);

        int playerForce = 400;
        if (player.isOnGround)
            playerForce = 2000;
        player_rb.AddForce((player.transform.position - transform.position).normalized * playerForce, ForceMode2D.Force);

        gameManager.ReceiveDamage(damage);
    }


    public void ReceiveDamage(float damage)
    {
        health -= damage;

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
            _rigidBody.AddForce((transform.position - player.transform.position).normalized * 7000, ForceMode2D.Force);

            receiveDamage = true;
            _animator.SetTrigger("ReceiveDamage");
        }
    }
    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    private void StopReceiveDamage()
    {
        receiveDamage = false;
    }

    private void HumanoidAttack()
    {
        canAttack = false;
        _animator.SetBool("Attack", true);
    }

    //private IEnumerator HumanoidAttack()
    //{
    //    canAttack = false;
    //    Debug.Log("Attak start");
    //    yield return new WaitForSeconds(1.5f);
    //    _animator.SetBool("Attack", true);
    //    yield return new WaitForSeconds(attackDelay);
    //    canAttack = true;
    //    Debug.Log("Attak end");
    //    isAttacking = false;
        
    //}
    private void StopAttack()
    {
        _animator.SetBool("Attack", false);
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
