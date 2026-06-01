using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _02._Scripts.CombatSystem
{
    public struct DamageData
    {
        public int DamageAmount;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public ModuleOwner Attacker;
        public bool IsCritical;
        
        public DamageData(int damageAmount, Vector3 hitPoint, Vector3 hitNormal, ModuleOwner attacker, bool isCritical)
        {
            DamageAmount = damageAmount;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Attacker = attacker;
            IsCritical = isCritical;
        }
    }
}