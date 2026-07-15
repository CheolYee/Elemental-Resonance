using System.Collections.Generic;
using UnityEngine;

namespace Battle.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialSequence", menuName = "Tutorial/Sequence")]
    public class TutorialSequenceSO : ScriptableObject
    {
        public List<TutorialStepSO> steps = new();
    }
}
