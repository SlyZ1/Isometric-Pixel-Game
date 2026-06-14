using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class EnemyAStar : MonoBehaviour
{
    [SerializeField] public Transform target;
    [Space]
    [SerializeField] private LayerMask gridMask;
    [SerializeField] private int graphSize;
    [SerializeField] private float enemySize;
    [SerializeField] private float nextWayDistance = 3f;
    [SerializeField] private float pathUpdateRade = 0.7f;

    private Path path;
    private int currentWayPoint = 0;
    public bool reached = false;

    public Seeker seeker;
    private Rigidbody2D rb;

    [HideInInspector] public float speed;

    [HideInInspector] public Vector2 dir = Vector2.zero;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        CancelInvoke("UpdatePath");
    }

    private void OnEnable()
    {
        GameManager.instance.pgm.graph?.active.Scan();
        InvokeRepeating("UpdatePath", 0f, pathUpdateRade * speed);
    }


    private void UpdatePath()
    {
        try
        {
            if (seeker.IsDone())
            {
                seeker.StartPath(rb.position, target.position + (target.position - transform.position).normalized * 1f, OnPathComplete);
            }
        }
        catch { }
    }


    private void OnPathComplete(Path p)
    {
        if (p.error)
        {
            Debug.Log("error loading path for enemy");
            Debug.Log(p.errorLog);
            return;
        }

        path = p;
        currentWayPoint = 0;
    }


    private void FixedUpdate()
    {
        UpdatePathfinding();
    }


    float dist = 0;
    private void UpdatePathfinding()
    {
        if(target == null)
        {
            dir = Vector2.zero;
            return;
        }

        if (path == null) return;

        if (currentWayPoint >= path.vectorPath.Count && dist <= 0.05f)
        {
            reached = true;
            return;
        }
        else reached = false;

        if (currentWayPoint >= path.vectorPath.Count) return;

        dir = ((Vector2)path.vectorPath[currentWayPoint] - rb.position).normalized;

        dist = Vector2.Distance(rb.position, path.vectorPath[currentWayPoint]);

        if (dist <= nextWayDistance)
        {
            currentWayPoint++;
            if (currentWayPoint >= path.vectorPath.Count) return;

            EnemyManager.instance.SendCheckPoint(EnemyManager.instance.enemiesKey[gameObject], path.vectorPath[currentWayPoint]);
        }
    }
}
