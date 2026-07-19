using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Presentation-only behavior shared by stakeholder Humanoids. Public scenario
    /// identity and conversation state stay owned by the existing hotspot and
    /// interview systems.
    /// </summary>
    public sealed class StakeholderCharacterController : MonoBehaviour
    {
        private static readonly int IdleState =
            Animator.StringToHash("Mai_Idle");
        private static readonly int TalkState =
            Animator.StringToHash("Mai_Talk");
        private static readonly int WaveState =
            Animator.StringToHash("Mai_Wave");
        private static readonly int ThinkingState =
            Animator.StringToHash("Mai_HatAdjust");

        [SerializeField] private Animator animator;
        [SerializeField] private Transform lookTarget;
        [SerializeField, Range(0f, 1f)] private float lookWeight = .68f;

        private int requestedState = IdleState;
        private bool focused;

        public Animator Animator => animator;
        public Transform LookTarget => lookTarget;
        public bool IsFocused => focused;
        public Vector3 FocusPoint
        {
            get
            {
                Transform head =
                    animator != null && animator.isHuman
                        ? animator.GetBoneTransform(
                            HumanBodyBones.Head)
                        : null;
                return head != null
                    ? head.position
                    : transform.position + Vector3.up * 1.55f;
            }
        }

        public void Configure(
            Animator sourceAnimator,
            Transform sourceLookTarget)
        {
            animator = sourceAnimator;
            lookTarget = sourceLookTarget;
            Play(IdleState, 0f);
        }

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            Play(value ? WaveState : IdleState, .18f);
        }

        public void SetConversationState(
            bool waitingForReply,
            bool presentingReply)
        {
            Play(
                waitingForReply
                    ? ThinkingState
                    : presentingReply
                        ? TalkState
                        : IdleState,
                .2f);
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void Update()
        {
            if (animator == null ||
                requestedState == IdleState ||
                animator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == requestedState &&
                state.normalizedTime >= .96f)
            {
                Play(IdleState, .2f);
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
                .12f,
                .7f,
                0f,
                .58f);
            animator.SetLookAtPosition(
                lookTarget.position + Vector3.down * .12f);
        }

        private void Play(int stateHash, float transition)
        {
            if (animator == null ||
                !animator.isActiveAndEnabled ||
                !animator.HasState(0, stateHash) ||
                requestedState == stateHash)
            {
                return;
            }

            requestedState = stateHash;
            if (transition <= 0f)
            {
                animator.Play(stateHash, 0, 0f);
            }
            else
            {
                animator.CrossFadeInFixedTime(
                    stateHash,
                    transition,
                    0,
                    0f);
            }
        }
    }
}
