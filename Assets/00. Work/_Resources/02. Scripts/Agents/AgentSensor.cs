using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class AgentSensor : MonoBehaviour, IModule
    {
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private LayerMask targetLayer;

        [SerializeField] private Vector2 boxSize;
        [SerializeField] private Vector2 boxOffset;
        
        private ModuleOwner _owner;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;    
        }

        public bool IsObstaclePresent(Vector2 direction, out Collider2D hitCollider)
        {
            Vector2 position = (Vector2)transform.position + direction + boxOffset;
            hitCollider = Physics2D.OverlapBox(position, boxSize, 0, obstacleLayer);
            return hitCollider != null;
        }

        public float BoxCastObstacle(Vector2 direction, float distance, out RaycastHit2D hit)
        {
            hit = Physics2D.BoxCast((Vector2)transform.position + boxOffset, boxSize, 0, direction, distance, obstacleLayer);
            
            distance = hit ? hit.distance : distance;
            return distance;
        }

        public bool IsTargetInRange(float range, out Collider2D hitCollider)
        {
            hitCollider = Physics2D.OverlapCircle(transform.position, range, targetLayer);
            return hitCollider != null;
        }

        public bool IsTargetInSight(Vector3 startPosition, float range, Collider2D target)
        {
            Vector2 direction = target.transform.position - startPosition;
            RaycastHit2D hit = Physics2D.Raycast(startPosition, direction.normalized, direction.magnitude, obstacleLayer);
            return hit.collider == null; //타겟과 나 사이에 아무런 장애물이 없을 경우 null이 나온다.
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)boxOffset, boxSize);
        }
    }
}