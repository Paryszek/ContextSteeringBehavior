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
    [SerializeField] private bool displayOutcomeDirections = true;
    [SerializeField] private bool displayResultDirection = true;

    private List<Vector2> moveDirections = new List<Vector2>();
    private GameObject targetGameObject;
    private Vector2 target;
    private float targetRadius;
    private float colliderSize;
    private float detectObstaclesRatio;
    private bool initialized = false;

    public void Init(GameObject targetGameObject, float targetRadius, float colliderSize, float detectObstaclesRatio = 1.0f) {
      this.targetGameObject = targetGameObject;
      this.targetRadius = targetRadius;
      this.colliderSize = colliderSize;
      this.detectObstaclesRatio = detectObstaclesRatio;
      this.initialized = true;
    }

    private void Awake() {
      GenerateMoveDirections();
    }

    public Vector2 GetDirectionToMove() {
      if (!initialized) {
        Debug.LogError("Error while using method GetDirectionToMove() - ContextSteeringBehavior should be initialized first with Init() method");
      }

      this.target = targetGameObject.transform.position;
      var chaseBehaviorDirections = CreateEmptyBehaviorDirectionList();
      var avoidBehaviorDirections = CreateEmptyBehaviorDirectionList();
      var outcomeBehaviorDirections = new List<Vector2>();

      CalculateWeightsForAvoidBehaviorDirections(avoidBehaviorDirections);
      CalculateWeightsForChaseBehaviorDirections(chaseBehaviorDirections);
      CalculateOutcomeBehaviorDirections(chaseBehaviorDirections, avoidBehaviorDirections, outcomeBehaviorDirections);

      return GetMeanDirectionFromOutcomeBehaviorDirections(outcomeBehaviorDirections);
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
        Debug.DrawLine(transform.position, transform.TransformPoint(meanDirection), Color.blue * 10.0f, 0.05f);
      }

      return meanDirection;
    }

    private void CalculateOutcomeBehaviorDirections(List<BehaviorDirection> chaseBehaviorDirections, List<BehaviorDirection> avoidBehaviorDirections, List<Vector2> outcomeBehaviorDirections) {
      for (var i = 0; i < chaseBehaviorDirections.Count; i++) {
        var chaseBehaviorDirection = chaseBehaviorDirections[i];
        var avoidBehaviorDirection = avoidBehaviorDirections[i];

        if (chaseBehaviorDirection.distance == 0) {
          continue;
        }

        var rawDirection = chaseBehaviorDirection.direction * Mathf.Clamp(chaseBehaviorDirection.weight - avoidBehaviorDirection.weight, -chaseBehaviorDirection.weight, chaseBehaviorDirection.weight);
        outcomeBehaviorDirections.Add(rawDirection);

        if (displayOutcomeDirections) {
          Debug.DrawLine(transform.position, transform.TransformPoint(rawDirection), Color.yellow, 0.05f);
        }
      }
    }

    private void CalculateWeightsForChaseBehaviorDirections(List<BehaviorDirection> chaseBehaviorDirections) {
      foreach (var chaseBehaviorDirection in chaseBehaviorDirections) {
        var startPosition = transform.TransformPoint(chaseBehaviorDirection.direction * colliderSize);
        var distance = Vector2.Distance(startPosition, target);

        if (Vector2.Distance(transform.position, target) < distance) {
          continue;
        }

        chaseBehaviorDirection.distance = distance;

        var endPosition = transform.TransformPoint(chaseBehaviorDirection.direction * colliderSize * targetRadius);
        var radiusDetection = Vector2.Distance(startPosition, endPosition);

        var weight = Mathf.Clamp(radiusDetection / chaseBehaviorDirection.distance, 0, radiusDetection);

        chaseBehaviorDirection.weight = weight;

        if (displayChaseDirections) {
          Debug.DrawLine(transform.position, transform.TransformPoint(chaseBehaviorDirection.direction * weight), Color.green, 0.05f);
        }
      }
    }

    private void CalculateWeightsForAvoidBehaviorDirections(List<BehaviorDirection> avoidBehaviorDirections) {
      foreach (var avoidBehaviorDirection in avoidBehaviorDirections) {
        var startPosition = transform.TransformPoint(avoidBehaviorDirection.direction * colliderSize);
        var endPosition = transform.TransformPoint(avoidBehaviorDirection.direction * colliderSize * targetRadius);
        var hitEndPosition = transform.TransformPoint(avoidBehaviorDirection.direction * colliderSize * targetRadius * detectObstaclesRatio);
        var radiusDetection = Vector2.Distance(startPosition, endPosition);

        var hit = Physics2D.Linecast(startPosition, hitEndPosition);

        if (hit && hit.collider.gameObject.name != this.targetGameObject.name) {
          avoidBehaviorDirection.distance = Vector2.Distance(startPosition, hit.collider.transform.position);
          var weight = Mathf.Clamp(radiusDetection / avoidBehaviorDirection.distance, 0, radiusDetection);
          avoidBehaviorDirection.weight = weight;

          if (displayAvoidDirections) {
            Debug.DrawLine(startPosition, transform.TransformPoint(avoidBehaviorDirection.direction * weight), Color.red, 0.05f);
          }
        }
      }
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