using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [Serializable]
    public sealed class StoryCameraStateData
    {
        [Tooltip("Stage local world position. (0,0)=StageRoot center. (2,0)=2 world units right. Unclamped.")]
        public Vector2 stageLocalPosition = Vector2.zero;

        [Tooltip("1 = 기본 orthographic size. 1.2 = 20% 확대 (finalOrtho = baseOrtho / zoom). 0 이하 불가.")]
        [Min(0.01f)]
        public float zoom = 1f;

        // Written by the sampler from CameraTarget keyframe data; read by Inspector/Timeline authoring UI.
        [HideInInspector] public string targetActorInstanceKey = "";
        [HideInInspector] public StoryCameraFollowMode followMode = StoryCameraFollowMode.FollowActor;
        [HideInInspector] public Vector2 snapshotNormalizedPosition = new Vector2(0.5f, 0.5f);

        public StoryCameraStateData ShallowClone() => (StoryCameraStateData)MemberwiseClone();
    }
}
