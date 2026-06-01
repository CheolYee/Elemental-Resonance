using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class AgentSensor : MonoBehaviour, IModule
    {
        [SerializeField] private LayerMask whatIsTarget;
        [SerializeField] private LayerMask whatIsObstacle;
        [SerializeField] private int maxColliderCount = 5;
        
        private ModuleOwner _owner;
        private Collider[] _colliderResults;
        public Collider[] ColliderResults => _colliderResults;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            Debug.Assert(maxColliderCount > 0, "[AgentSensor] cannot have more than 0 colliders");
            _colliderResults = new Collider[maxColliderCount];
        }

        public bool IsTargetInViewAngle(Transform targetTrm, float viewAngle)
        {
            Vector3 direction = targetTrm.position - transform.position;
            direction.y = 0;
            float angle = Vector3.Angle(transform.forward, direction);
            return angle <= viewAngle * 0.5f;
        }
        
        public bool IsTargetIsInSight(Transform targetTrm)
        {
            Vector3 targetPos = targetTrm.position;
            targetPos.y = transform.position.y;
            
            Vector3 direction = targetPos - transform.position;
            float distance = direction.magnitude;
            
            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, distance, whatIsObstacle))
            {
                Debug.Log(hit.collider.gameObject.name);
                return false; // 장애물에 가려져 있음
            }
            
            return true;
        }
        
        // 타겟이 시야 반경 안에 있는지 (거리 비교, 제곱근 계산 없이)
        public bool IsTargetInViewRadius(Transform targetTrm, float viewRadius)
            => (targetTrm.position - transform.position).sqrMagnitude <= viewRadius * viewRadius;
        
        // 범위 안에 있는 타겟의 개수를 반환, 최대 maxColliderCount개까지 결과를 저장
        public int FindTargetsInRadius(float viewRadius)
            => Physics.OverlapSphereNonAlloc(transform.position, viewRadius, _colliderResults, whatIsTarget);
    }
}