using RPGSilent.Domain;
using UnityEngine;

public partial class PlayerController
{
    Vector3 tutorialPosition;
    bool tutorialPositionSet;
    float tutorialWalked;
    void TrackTutorialMovement()
    {
        Vector3 position = transform.position;
        Vector3 delta = position - tutorialPosition; delta.y = 0;
        tutorialPosition = position;
        if (!tutorialPositionSet) { tutorialPositionSet = true; return; }
        if (progress == null || !progress.ShouldTrackTutorialMovement) { tutorialWalked = 0; return; }
        if (!GameplayInputEnabled || IsDead || !IsGrounded || InputDir.sqrMagnitude < .01f || StateMachine.CurrentState is not MoveState) return;
        // Teleports, loading snaps and knockback cannot complete the walking step.
        float distance = delta.magnitude;
        if (distance > CurrentMoveSpeed * Time.deltaTime * 2f + .2f) return;
        tutorialWalked += distance;
        if (tutorialWalked >= 3f) progress?.RecordTutorialAction(TutorialStep.Move);
    }
}
