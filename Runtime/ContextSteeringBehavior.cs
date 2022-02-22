using System.Collections.Generic;

using UnityEngine;

namespace MParysz.ContextSteeringBehavior {
  internal class BehaviorDirection {
    public Vector2 direction;
    public float distance = 0;
    public float weight = 0;

    public BehaviorDirection() { }

    public BehaviorDirection(BehaviorDirection behaviorDirection) {
      this.direction = new Vector2(behaviorDirection.direction.x, behaviorDirection.direction.y);
      this.distance = behaviorDirection.distance;
      this.weight = behaviorDirection.weight;
    }
  }

  public class ContextSteeringBehavior : MonoBehaviour {
    [Header("References")]
    [SerializeField] private bool displayAvoidDirections = false;
    [SerializeField] private bool displayChaseDirections = false;
    [SerializeField] private bool displayOutcomeDirections = false;
    [SerializeField] private bool displayResultDirection = false;
    [SerializeField] private bool displayAdjustedToCornerDirection = false;

    private List<Vector2> moveDirections = new List<Vector2>();
    private Vector2 target;
    private Vector2 size;
    private float sizeMarginRatio;
    private float obstaclesDetectionDistance;
    private float cornerDetectionDistance;
    private string targetName;
    private int layerMask;
    private bool initialized = false;

    private Vector2 currentDirection;
    private Vector2 currentDirectionBeforeCornerAdjustment;

    public void Init(Vector2 target, Vector2 size, float sizeMarginRatio = 1.0f, float obstaclesDetectionDistance = 1.0f, float cornerDetectionDistance = 1.0f, string targetName = "", int layerMask = 1 << 0) {
      this.size = size;
      this.target = target;
      this.targetName = targetName;
      this.layerMask = layerMask;
      this.sizeMarginRatio = sizeMarginRatio;
      this.obstaclesDetectionDistance = obstaclesDetectionDistance;
      this.cornerDetectionDistance = cornerDetectionDistance;

      this.initialized = true;
    }

    public void UpdateTarget(Vector2 target, string targetName = "") {
      this.target = target;
      this.targetName = targetName;
    }

    public void UpdateDetectionDistance(float obstaclesDetectionDistance, float cornerDetectionDistance) {
      this.obstaclesDetectionDistance = obstaclesDetectionDistance;
      this.cornerDetectionDistance = cornerDetectionDistance;
    }

    private void Awake() {
      GenerateMoveDirections();
    }

    private void OnDrawGizmos() {
      if (!displayAdjustedToCornerDirection) {
        return;
      }

      if (currentDirection == null) {
        return;
      }

      var endPosition = transform.TransformPoint(currentDirectionBeforeCornerAdjustment * cornerDetectionDistance);
      var radiusDetection = Vector2.Distance(transform.position, endPosition);
      var hit = Physics2D.BoxCast(transform.TransformPoint(currentDirectionBeforeCornerAdjustment), size * sizeMarginRatio, 0.0f, currentDirectionBeforeCornerAdjustment, radiusDetection, layerMask);

      if (hit && hit.collider.gameObject.name != targetName) {
        Gizmos.color = Color.red;
        Vector2 colliderPosition = hit.collider.transform.position;
        var directionFromColliderToHitPoint = (hit.point - colliderPosition).normalized;
        Gizmos.DrawWireCube(hit.point + (directionFromColliderToHitPoint * size * 0.5f), size * sizeMarginRatio);
      }
    }

    public Vector2 GetDirectionToMove() {
      if (!initialized) {
        Debug.LogError("Error while using method GetDirectionToMove() - ContextSteeringBehavior should be initialized first with Init() method");
      }

      var chaseBehaviorDirections = CalculateWeightsForChaseBehaviorDirections();
      var hit = DetectCornerAhead();

      if (IsObstacleAhead(hit)) {
        currentDirection = GetMeanDirectionFromOutcomeBehaviorDirections(CalculateOutcomeBehaviorDirections(chaseBehaviorDirections));
        currentDirectionBeforeCornerAdjustment = new Vector2(currentDirection.x, currentDirection.y);
        currentDirection = AdjustDirectionToCorner(hit);
        return currentDirection;
      }

      var avoidBehaviorDirections = CalculateWeightsForAvoidBehaviorDirections();
      currentDirection = GetMeanDirectionFromOutcomeBehaviorDirections(CalculateOutcomeBehaviorDirections(chaseBehaviorDirections, avoidBehaviorDirections));

      return currentDirection;
    }

    private Vector2 AdjustDirectionToCorner(RaycastHit2D hit) {
      Vector2 colliderPosition = hit.collider.transform.position;
      var directionFromColliderToHitPoint = (hit.point - colliderPosition).normalized;

      var adjustedDirection = new Vector2((currentDirection.x + directionFromColliderToHitPoint.x) / 2, (currentDirection.y + directionFromColliderToHitPoint.y) / 2).normalized;

      if (displayAdjustedToCornerDirection) {
        Debug.DrawRay(hit.point, directionFromColliderToHitPoint, Color.cyan, 0.1f);
        Debug.DrawLine(transform.position, transform.TransformPoint(adjustedDirection * obstaclesDetectionDistance), Color.blue, 0.05f);
      }

      return adjustedDirection;
    }

    private RaycastHit2D DetectCornerAhead() {
      var endPosition = transform.TransformPoint(currentDirection * cornerDetectionDistance);
      var radiusDetection = Vector2.Distance(transform.position, endPosition);

      return Physics2D.BoxCast(transform.TransformPoint(currentDirection), size * sizeMarginRatio, 0.0f, currentDirection, radiusDetection, layerMask);
    }

    private Vector2 GetMeanDirectionFromOutcomeBehaviorDirections(List<Vector2> outcomeBehaviorDirections) {
      float meanX = 0;
      float meanY = 0;

      foreach (var outcomeBehaviorDirection in outcomeBehaviorDirections) {
        meanX += outcomeBehaviorDirection.x;
        meanY += outcomeBehaviorDirection.y;
      }

      meanX = meanX / outcomeBehaviorDirections.Count;
      meanY = meanY / outcomeBehaviorDirections.Count;

      var meanDirection = new Vector2(meanX, meanY).normalized;

      if (displayResultDirection) {
        Debug.DrawLine(transform.position, transform.TransformPoint(meanDirection * obstaclesDetectionDistance), Color.gray, 0.05f);
      }

      return meanDirection;
    }

    private List<Vector2> CalculateOutcomeBehaviorDirections(List<BehaviorDirection> chaseBehaviorDirections, List<BehaviorDirection> avoidBehaviorDirections = null) {
      List<Vector2> outcomeBehaviorDirections = new List<Vector2>();

      for (var i = 0; i < chaseBehaviorDirections.Count; i++) {
        var chaseBehaviorDirection = chaseBehaviorDirections[i];

        if (chaseBehaviorDirection.distance == 0) {
          continue;
        }

        Vector2 rawDirection;

        if (avoidBehaviorDirections != null) {
          var avoidBehaviorDirection = avoidBehaviorDirections[i];
          rawDirection = chaseBehaviorDirection.direction * Mathf.Clamp(chaseBehaviorDirection.weight - avoidBehaviorDirection.weight, -chaseBehaviorDirection.weight, chaseBehaviorDirection.weight);
        } else {
          rawDirection = chaseBehaviorDirection.direction * Mathf.Clamp(chaseBehaviorDirection.weight, -chaseBehaviorDirection.weight, chaseBehaviorDirection.weight);
        }

        outcomeBehaviorDirections.Add(rawDirection);

        if (displayOutcomeDirections) {
          Debug.DrawLine(transform.position, transform.TransformPoint(rawDirection * 10.0f), Color.yellow, 0.05f);
        }
      }

      return outcomeBehaviorDirections;
    }

    private List<BehaviorDirection> CalculateWeightsForChaseBehaviorDirections() {
      var chaseBehaviorDirections = CreateEmptyBehaviorDirectionList();

      foreach (var chaseBehaviorDirection in chaseBehaviorDirections) {
        var distance = Vector2.Distance(transform.TransformPoint(chaseBehaviorDirection.direction), target);

        if (Vector2.Distance(transform.position, target) < distance) {
          continue;
        }

        chaseBehaviorDirection.distance = distance;

        var endPosition = transform.TransformPoint(chaseBehaviorDirection.direction * obstaclesDetectionDistance);
        var radiusDetection = Vector2.Distance(transform.position, endPosition);

        var weight = Mathf.Clamp(radiusDetection / chaseBehaviorDirection.distance, 0, radiusDetection);

        chaseBehaviorDirection.weight = weight;

        if (displayChaseDirections) {
          Debug.DrawLine(transform.position, transform.TransformPoint(chaseBehaviorDirection.direction * weight), Color.green, 0.05f);
        }
      }

      return chaseBehaviorDirections;
    }

    private List<BehaviorDirection> CalculateWeightsForAvoidBehaviorDirections() {
      var avoidBehaviorDirections = CreateEmptyBehaviorDirectionList();

      foreach (var avoidBehaviorDirection in avoidBehaviorDirections) {
        var endPosition = transform.TransformPoint(avoidBehaviorDirection.direction * obstaclesDetectionDistance);
        var radiusDetection = Vector2.Distance(transform.position, endPosition);

        var hit = Physics2D.Raycast(transform.position, avoidBehaviorDirection.direction, radiusDetection, layerMask);

        if (IsObstacleAhead(hit)) {
          avoidBehaviorDirection.distance = Vector2.Distance(transform.TransformPoint(avoidBehaviorDirection.direction), hit.collider.transform.position);
          var weight = Mathf.Clamp(radiusDetection / avoidBehaviorDirection.distance, 0, radiusDetection);
          avoidBehaviorDirection.weight = weight;

          if (displayAvoidDirections) {
            Debug.DrawLine(transform.position, transform.TransformPoint(avoidBehaviorDirection.direction * weight), Color.red, 0.05f);
          }
        }
      }

      return avoidBehaviorDirections;
    }

    private List<BehaviorDirection> CreateEmptyBehaviorDirectionList() {
      List<BehaviorDirection> behaviorDirections = new List<BehaviorDirection>();

      foreach (var direction in moveDirections) {
        var behaviorDirection = new BehaviorDirection();
        behaviorDirection.direction = new Vector2(direction.x, direction.y);
        behaviorDirections.Add(behaviorDirection);
      }

      return behaviorDirections;
    }

    private bool IsObstacleAhead(RaycastHit2D hit) {
      return hit && hit.collider.gameObject.name != targetName;
    }

    private void GenerateMoveDirections() {
      moveDirections.Add(Vector2.up);
      moveDirections.Add(Vector2.down);
      moveDirections.Add(Vector2.left);
      moveDirections.Add(Vector2.right);
      moveDirections.Add(new Vector2(0.75f, 0.75f));
      moveDirections.Add(new Vector2(-0.75f, -0.75f));
      moveDirections.Add(new Vector2(-0.75f, 0.75f));
      moveDirections.Add(new Vector2(0.75f, -0.75f));
    }
  }
}