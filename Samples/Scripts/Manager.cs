using UnityEngine;
using MParysz.ContextSteeringBehavior;

public class Manager : MonoBehaviour {
  [SerializeField] private GameObject target;
  [SerializeField] private float detectTargetRadius = 10.0f;
  [SerializeField] private float detectObstaclesRatio = 0.1f;

  private ContextSteeringBehavior contextSteeringBehavior;
  private BoxCollider2D boxCollider2D;
  private Rigidbody2D rigidBody2D;

  private void Awake() {
    rigidBody2D = GetComponent<Rigidbody2D>();
    boxCollider2D = GetComponent<BoxCollider2D>();
    contextSteeringBehavior = GetComponent<ContextSteeringBehavior>();
  }

  private void Start() {
    var colliderSize = boxCollider2D.bounds.size + new Vector3(boxCollider2D.offset.x, boxCollider2D.offset.y);
    contextSteeringBehavior.Init(target, detectTargetRadius, colliderSize.x, detectObstaclesRatio);
  }

  private void Update() {
    var direction = contextSteeringBehavior.GetDirectionToMove();
    rigidBody2D.velocity = new Vector2(direction.x * 200.0f * Time.deltaTime, direction.y * 200.0f * Time.deltaTime);
  }
}
