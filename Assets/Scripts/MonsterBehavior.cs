using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterBehavior : MonoBehaviour
{
    [HideInInspector]
    public bool patrol; //should go forward
    [HideInInspector]
    public bool turn; //should turn

    public Rigidbody2D rb;
    public Transform groundCheck; //checks if blocks have ended
    public LayerMask groundLayer; //layer location

    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        patrol = true;   
    }

    // Update is called once per frame
    void Update()
    {
        if (patrol)
        {
            Patrol();
        }
    }

    private void FixedUpdate()
    {
        if (patrol)
        {
            turn = !Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer); //if monster reached end of ground
        }
    }

    void Patrol()
    {
        if (turn) //moster reached end of ground and needs to turn
        {
            Flip();
        }
        rb.velocity = new Vector2(speed * Time.fixedDeltaTime, rb.velocity.y); //speed and direction of monster
    }

    void Flip() //flips character and changes direction
    {
        patrol = false;
        transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
        speed *= -1;
        patrol = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene("LoseScreen");
        }
    }
}
