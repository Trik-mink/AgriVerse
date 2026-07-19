using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Presentation-only Humanoid look-at behavior. The authored animation remains
    /// authoritative; IK adds a restrained head-and-body response toward the player.
    /// </summary>
    public sealed class MaiGuideController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform lookTarget;
        [SerializeField, Range(0f, 1f)] private float lookWeight = .72f;

        public Animator Animator => animator;
        public Transform LookTarget => lookTarget;

        public void Configure(Animator sourceAnimator, Transform target)
        {
            animator = sourceAnimator;
            lookTarget = target;
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null ||
                lookTarget == null ||
                !animator.isHuman)
            {
                return;
            }

            animator.SetLookAtWeight(
                lookWeight,
                .14f,
                .72f,
                0f,
                .62f);
            animator.SetLookAtPosition(
                lookTarget.position + Vector3.down * .15f);
        }
    }
}
