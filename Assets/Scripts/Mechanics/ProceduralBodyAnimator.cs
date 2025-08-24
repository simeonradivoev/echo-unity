using UnityEcho.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

[RequireComponent(typeof(Animator))]
public class ProceduralBodyAnimator : MonoBehaviour
{
    [Header("Animator & Tracking")]
    public Animator animator; // your avatar's Animator

    public Vector3 HeadOffset;

    public Transform Head;

    public Rigidbody HipJointBody; // the HMD's rigidbody

    public float _testAngle;

    private IKController _ikController;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        _ikController = GetComponent<IKController>();
    }

    private void Update()
    {
        animator.transform.position = HipJointBody.transform.position;
        animator.transform.rotation = _ikController.Body.rotation;
    }

    private void LateUpdate()
    {
        var leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        var rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);

        var leftShoulderPos = leftShoulder.position;
        var leftShoulderRot = leftShoulder.rotation;
        var rightShoulderPos = rightShoulder.position;
        var rightShoulderRot = rightShoulder.rotation;

        //var chestPos = chest.position;
        //var chestRotation = chest.rotation;

        //chest.position = chestPos;
        //chest.rotation = chestRotation;

        var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        var neckBone = animator.GetBoneTransform(HumanBodyBones.Neck);
    }

    private void OnAnimatorIK(int layer)
    {
        var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        var spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);

        var localHipRotation = Quaternion.Inverse(hips.parent.rotation) * HipJointBody.transform.rotation * hips.localRotation;
        animator.SetBoneLocalRotation(HumanBodyBones.Hips, localHipRotation);

        var localHipSpace = Matrix4x4.TRS(hips.localPosition, localHipRotation, Vector3.one);
        var localSpineSpace = Matrix4x4.TRS(spine.localPosition, spine.localRotation, Vector3.one);
        var chestLocalRotation = chest.localRotation;
        var localChestSpace = Matrix4x4.TRS(chest.localPosition, chestLocalRotation, Vector3.one);

        var worldChestSpace = animator.transform.localToWorldMatrix * localHipSpace * localSpineSpace * localChestSpace;
        var inverseWorldChestSpace = Matrix4x4.Inverse(worldChestSpace);

        var directionToHeadLocal = inverseWorldChestSpace.MultiplyPoint(Head.TransformPoint(HeadOffset)).normalized;
        var upLocalDir = new Vector3(-1, 0, 0);

        var localTorsoDirection = inverseWorldChestSpace.MultiplyVector(_ikController.TorsoDirection);
        var projectedLocalTorsoDir = Vector3.ProjectOnPlane(localTorsoDirection, upLocalDir);

        var rotationDelta = Quaternion.FromToRotation(upLocalDir, directionToHeadLocal) *
                            Quaternion.FromToRotation(Vector3.forward, projectedLocalTorsoDir);
        animator.SetBoneLocalRotation(HumanBodyBones.Chest, rotationDelta);
    }
}