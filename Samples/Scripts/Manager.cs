using UnityEngine;
using MParysz.ContextSteeringBehavior;

public class Manager : MonoBehaviour {
  [SerializeField] private GameObject target;
  [SerializeField] private float cornerDetectionDistance = 2.0f;
  [SerializeField] private float obstaclesDetectionDistance = 2.0f;

  private ContextSteeringBehavior contextSteeringBehavior;
  private BoxCollider2D boxCollider2D;
  private Rigidbody2D rigidBody2D;
  private float timeDelay = 0.5f;
  private float timeValue = 0.0f;

  private void Awake() {
    rigidBody2D = GetComponent<Rigidbody2D>();
    boxCollider2D = GetComponent<BoxCollider2D>();
    contextSteeringBehavior = GetComponent<ContextSteeringBehavior>();
  }

  private void Start() {
    var size = transform.lossyScale;
    contextSteeringBehavior.Init(target.transform.position, size);
  }

  private void Update() {
    if (timeDelay > timeValue) {
      timeValue += Time.deltaTime;
      return;
    }

    contextSteeringBehavior.UpdateTarget(target.transform.position);
    contextSteeringBehavior.UpdateDetectionDistance(obstaclesDetectionDistance, cornerDetectionDistance);

    var direction = contextSteeringBehavior.GetDirectionToMove();

    rigidBody2D.velocity = new Vector2(direction.x * 1000.0f * Time.deltaTime, direction.y * 1000.0f * Time.deltaTime);
  }
}
