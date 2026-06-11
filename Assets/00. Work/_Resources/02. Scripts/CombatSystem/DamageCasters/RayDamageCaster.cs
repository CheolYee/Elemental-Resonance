using System;
using _02._Scripts.CombatSystem;
using _02._Scripts.CombatSystem.Skills;
using UnityEngine;

namespace CombatSystem
{
    public class RayDamageCaster : AbstractDamageCaster
    {
        public enum CastType {Ray, Sphere, Box}
        
        [SerializeField] private CastType castType = CastType.Sphere;
        [SerializeField] private Vector3 boxSize = Vector3.one;
        [SerializeField, Range(0.5f, 3f)] private float casterRadius = 1f;
        [SerializeField, Range(0f, 1f)] private float casterInterpolation = 0.5f;
        [SerializeField, Range(0f, 3f)] private float castingRange = 1f;
        [SerializeField] private bool isDebugMode = false;
        
        public override bool CastDamage(Vector3 position, Vector3 direction, SkillDataSO skillData)
        {
            Vector3 startPosition = position + (direction * -casterInterpolation * 2f);

            RaycastHit hit = default;
            bool isHit = castType switch
            {
                CastType.Ray => Physics.Raycast(startPosition, direction, out hit, castingRange, whatIsEnemy),
                CastType.Sphere => Physics.SphereCast(startPosition, casterRadius, direction,
                    out hit, castingRange, whatIsEnemy),
                CastType.Box => Physics.BoxCast(startPosition, boxSize * 0.5f, direction,
                    out hit, Quaternion.identity, castingRange, whatIsEnemy),
                _ => false
            };

            if (isHit && hit.collider != null && hit.collider.TryGetComponent(out IDamageable damageable))
            {
                Debug.Log($"<color=red>Hit. </color> : {hit.collider.name}");
                int sampleDamage = 5;
                bool isCritical = false;
                
                LastHitPosition = hit.point;
                LastHitNormal = hit.normal;
                LastHitIsCritical = isCritical;
                
                damageable.TakeDamage(new DamageData
                {
                    DamageAmount = sampleDamage,
                    Attacker = CasterOwner,
                    HitPoint = LastHitPosition,
                    HitNormal = LastHitNormal,
                    IsCritical = LastHitIsCritical
                });

            }
            else
            {
                Debug.Log("Not hit");
            }
            
            return isHit;
        }

        private void OnDrawGizmos()
        {
            if (!isDebugMode) return;
            Vector3 startPosition = transform.position - transform.forward * (casterInterpolation * 2f);

            switch (castType)
            {
                case CastType.Ray:
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(startPosition, transform.forward * castingRange);
                    break;
                case CastType.Sphere:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(startPosition, casterRadius);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(startPosition + transform.forward * castingRange, casterRadius);
                    break;
                case CastType.Box:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(startPosition, boxSize);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(startPosition + transform.forward * casterRadius, boxSize);
                    break;
            }
        }
    }
}