using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntComponentManager
{
    const float physGravity = -9.81f;

    float targetSpeed = 0f;

    public void TickComponents()
    {
        if (SysLevelEntities.Instance.stateManager == null) return;
        if (SysLevelEntities.Instance.stateManager.GetEntities() == null) return;

        EntState[] entities = SysLevelEntities.Instance.stateManager.GetEntities();

        foreach (EntState ent in entities)
        {
            if (ent == null) continue;
            if (ent.RefEntity == null) continue;



            // Physics Component
            if (ent.RefEntity.UsesCompPhysics)
            {
                TickCompPhysics(ent);
            }

            // Animation Component
            if (ent.RefEntity.UsesCompAnimation)
            {
                TickCompAnimation(ent);
            }

            // Door Component
            if (ent.RefEntity.UsesCompDoor)
            {
                TickCompDoor(ent);
            }

            // Player Control Component
            if (ent.RefEntity.UsesCompPlayerController)
            {
                TickCompPlayerControl(ent);
            }

            // AI Component
            if (ent.RefEntity.UsesCompAI)
            {
                TickCompAI(ent);
            }

            // Damage Receiver Component
            if (ent.RefEntity.UsesCompDamageReceiver)
            {
                if (ent.InvulnerabilityTimer > 0f)
                {
                    ent.InvulnerabilityTimer -= Time.deltaTime;
                    if (ent.InvulnerabilityTimer < 0f) ent.InvulnerabilityTimer = 0f;
                }
            }

            // Damage Emitter Component
            if (ent.RefEntity.UsesCompDamageEmitter)
            {
                TickCompDamageEmitter(ent);
            }

        }
    }

    // ---------- Player Control ---------- //

    public void TickCompPlayerControl(EntState ent)
    {
        if (ent == null || ent.RefEntity == null) return;
        if (!ent.RefEntity.UsesCompPlayerController) return;
        if (ent.RefEntity.CompPlayerController == null) return;

        var ctrl = ent.RefEntity.CompPlayerController;

        if (ent.InvulnerabilityTimer > 0f)
        {
            // Disable control while invulnerable (e.g. after taking damage)
            ent.HasControl = false;
        }
        else
        {
            ent.HasControl = true;
        }

        // -------- Death Check -------- //
        if (ent.EntHealth <= 0f)
        {
            ent.HasControl = false;
        }

        // -------- Input -------- //
        float inputX = Input.GetAxisRaw("Horizontal");
        float moveDir = Mathf.Sign(inputX);
        bool hasInput = Mathf.Abs(inputX) > 0.01f;
        bool isGrounded = ent.IsGrounded;

        if (ent.HasControl == false)
        {
            inputX = 0f;
            moveDir = 0f;
            hasInput = false;
        }

        // -------- Move Params -------- //
        float maxSpeed = Input.GetKey(KeyCode.LeftShift) ? ctrl.RunSpeed : ctrl.MoveSpeed;
        float acceleration = ctrl.Acceleration;
        float deceleration = ctrl.Deceleration;
        float airDecel = ctrl.AirDeceleration;

        // -------- Variable Jump Params -------- //
        float baseJumpHeight = Mathf.Max(0.01f, ctrl.JumpHeight);               // tap height
        float maxJumpHeight = Mathf.Max(baseJumpHeight, ctrl.JumpHeightMax);   // held height (>= base)
        float minJumpVel = Mathf.Sqrt(2f * Mathf.Abs(physGravity) * baseJumpHeight);
        float heldGravityScale = Mathf.Clamp01(baseJumpHeight / maxJumpHeight);
        float antiGravityPerSec = Mathf.Abs(physGravity) * (1f - heldGravityScale);

        // -------- Horizontal Accel / Decel -------- //
        targetSpeed = hasInput ? moveDir * maxSpeed : 0f;
        if (ent.HasControl == false) targetSpeed = 0f;

        float speedDiff = targetSpeed - ent.Velocity.x;
        float dir = 0f;
        if (Mathf.Abs(speedDiff) >= 0.01f) dir = Mathf.Sign(speedDiff);

        if (hasInput && Mathf.Sign(dir) == Mathf.Sign(moveDir))
        {
            ent.Velocity = new Vector2(
                ent.Velocity.x + (acceleration * dir) * Time.deltaTime,
                ent.Velocity.y
            );
        }
        else
        {
            float decelRate = isGrounded ? deceleration : airDecel;
            ent.Velocity = new Vector2(
                ent.Velocity.x + (decelRate * dir) * Time.deltaTime,
                ent.Velocity.y
            );
        }

        // -------- Sprite Facing -------- //
        if (hasInput)
            ent.EntSpriteFlipped = moveDir < 0f;

        // -------- Jumping -------- //
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        bool jumpHeld = Input.GetKey(KeyCode.Space);

        if (jumpPressed && isGrounded)
        {
            ent.Velocity = new Vector2(ent.Velocity.x, minJumpVel);
            ent.IsGrounded = false;
        }

        if (jumpHeld && ent.Velocity.y > 0f)
        {
            ent.Velocity = new Vector2(
                ent.Velocity.x,
                ent.Velocity.y + antiGravityPerSec * Time.deltaTime
            );
        }

        if (!jumpHeld && ent.Velocity.y > minJumpVel)
        {
            ent.Velocity = new Vector2(ent.Velocity.x, minJumpVel);
        }

        // -------- Bounce Off Damaged Enemy -------- //
        EntState[] entities = SysLevelEntities.Instance.stateManager.GetEntities();
        foreach (var e in entities)
        {
            if (e == null || e == ent) continue;
            if (!e.WasDamagedRecently) continue;

            if (Time.time - e.LastDamagedTime < 0.2f) // short time window
            {
                if (ent.EntPosition.y > e.EntPosition.y) // player above enemy
                {
                    // Bounce even if we just killed it (prevHealth > 0 ensured in emitter)
                    float bounceVel = Mathf.Sqrt(2f * Mathf.Abs(physGravity) * ctrl.JumpHeight);
                    ent.Velocity = new Vector2(ent.Velocity.x, bounceVel);

                    e.WasDamagedRecently = false; // consume bounce
                }
            }
        }


        // -------- Animation Scene Switching -------- //
        if (ent.RefEntity.UsesCompAnimation)
        {
            string desiredScene;
            if (!ent.IsGrounded) desiredScene = "Jump";
            else if (ent.EntHealth <= 0f) desiredScene = "Dead";
            else if (ent.InvulnerabilityTimer > 0f) desiredScene = "Hurt";
            else if (hasInput) desiredScene = "Walk";
            else desiredScene = "Idle";

            if (ent.CurrentAnimationScene != desiredScene)
            {
                ent.CurrentAnimationScene = desiredScene;
                ent.CurrentFrameIndex = 0;
                ent.AnimationTimer = 0f;

                var anim = SysResource.Instance.GetResourceConfig<ResCfgAnimationCollection>(ent.RefEntity.CompAnimation.AnimationID);
                if (anim != null)
                {
                    var scene = Array.Find(anim.AnimationScenes, s => s.SceneID == desiredScene);
                    if (scene != null && scene.FrameSpriteIDs.Length > 0)
                    {
                        string frameID = scene.FrameSpriteIDs[0];
                        Sprite frameSprite = SysResource.Instance.GetResource<Sprite>(frameID);
                        if (frameSprite != null)
                            ent.EntTargetSprite = frameSprite;
                    }
                }
            }
        }
    }

    // ---------- Physics ---------- //

    public void TickCompPhysics(EntState ent)
    {
        if (ent == null || ent.RefEntity == null || !ent.RefEntity.UsesCompPhysics) return;

        // ---- Config ----
        float mass = (ent.RefEntity.CompPhysics.Mass > 0f) ? ent.RefEntity.CompPhysics.Mass : 1f;
        float drag = Mathf.Max(0f, ent.RefEntity.CompPhysics.Drag);          // generic linear drag (unitless per second)
        float bounciness = Mathf.Clamp01(ent.RefEntity.CompPhysics.Bounciness);    // 0..1

        const float minBounceImpulse = 0.5f;   // minimum downward speed to trigger a bounce-like rebound
        const float tinyVelCutoff = 0.01f;  // snap very small velocities to zero
        float dt = Time.deltaTime;

        // Physics should be additive: NEVER reset velocity here.
        // Also, we should NOT impose extra horizontal decay here for player-controlled entities.
        bool isPlayerControlled = ent.RefEntity.UsesCompPlayerController;

        // ---- Reset per-frame state ----
        ent.IsGrounded = false;

        // ---- Forces: Gravity (additive) ----
        Vector2 gravity = new Vector2(0f, physGravity);   // physGravity is negative, e.g. -9.81
        ent.Velocity += (gravity / mass) * dt;

        // ---- Passive damping (do not fight player input on X) ----
        // Apply config "drag" to both axes, but make X very lightly damped (or not at all) when player-controlled.
        if (drag > 0f)
        {
            float xDamp = isPlayerControlled ? Mathf.Max(0f, 1f - (drag * 0.25f * dt)) // lighter on X for player
                                             : Mathf.Max(0f, 1f - (drag * dt));
            float yDamp = Mathf.Max(0f, 1f - (drag * dt));

            ent.Velocity = new Vector2(ent.Velocity.x * xDamp, ent.Velocity.y * yDamp);
        }

        // ---- Integrate ----
        Vector2 oldPos = ent.EntPosition;
        Vector2 newPos = oldPos + ent.Velocity * dt;
        ent.EntPosition = newPos;

        // ---- Collision resolution (iterate a few times for robustness) ----
        for (int i = 0; i < 3; i++)
        {
            CollisionInfo hit = SysLevelPhysics.Instance.GetEntCollision(ent);
            if (hit == null || hit.CollisionRect == null) break;

            SysLevelPhysics.DrawCollisionInfo(hit);

            // Move out of penetration
            ent.EntPosition = hit.NearestValidPos;

            // Determine collision normal from the push-out force
            Vector2 n = Vector2.zero;
            if (hit.Force.x != 0f) n = new Vector2(Mathf.Sign(hit.Force.x), 0f);
            else if (hit.Force.y != 0f) n = new Vector2(0f, Mathf.Sign(hit.Force.y));

            if (n == Vector2.zero) continue;

            // Reflect or clamp velocity along the collision normal
            // NOTE: Do not stomp the tangential (along-surface) velocity for player feel.
            if (n.y > 0f) // floor
            {
                // Record downward speed BEFORE zeroing for optional bounce
                float downSpeed = -Mathf.Min(0f, ent.Velocity.y);

                // Ground contact: kill penetration axis (Y) and mark grounded
                ent.Velocity = new Vector2(ent.Velocity.x, 0f);
                ent.IsGrounded = true;

                // Optional rebound if impact was strong enough
                if (downSpeed > minBounceImpulse && bounciness > 0f)
                {
                    ent.Velocity = new Vector2(ent.Velocity.x, downSpeed * bounciness);
                }

                // Do NOT apply any extra horizontal decay here. Let the controller handle ground friction/decay.
            }
            else if (n.y < 0f) // ceiling
            {
                // Stop upward movement
                ent.Velocity = new Vector2(ent.Velocity.x, Mathf.Min(0f, ent.Velocity.y));
            }
            else if (n.x != 0f) // wall
            {
                // Stop into-wall movement; keep vertical velocity intact
                ent.Velocity = new Vector2(0f, ent.Velocity.y);
            }

            // Clean up tiny velocities to avoid micro jitter
            if (Mathf.Abs(ent.Velocity.x) < tinyVelCutoff) ent.Velocity = new Vector2(0f, ent.Velocity.y);
            if (Mathf.Abs(ent.Velocity.y) < tinyVelCutoff) ent.Velocity = new Vector2(ent.Velocity.x, 0f);
        }
    }


    // ---------- Animation ---------- //

    public void TickCompAnimation(EntState ent)
    {
        if (ent == null || ent.RefEntity == null || !ent.RefEntity.UsesCompAnimation) return;

        var animID = ent.RefEntity.CompAnimation.AnimationID;
        var anim = SysResource.Instance.GetResourceConfig<ResCfgAnimationCollection>(animID);
        if (anim == null) return;

        var scene = Array.Find(anim.AnimationScenes, s => s.SceneID == ent.CurrentAnimationScene);
        if (scene == null || scene.FrameSpriteIDs.Length == 0) return;

        float frameDuration = 1f / Mathf.Max(anim.FrameRate, 1);
        ent.AnimationTimer += Time.deltaTime;

        if (ent.AnimationTimer >= frameDuration)
        {
            ent.AnimationTimer -= frameDuration;
            ent.CurrentFrameIndex++;

            if (ent.CurrentFrameIndex >= scene.FrameSpriteIDs.Length)
            {
                if (ent.RefEntity.CompAnimation.Loop)
                {
                    ent.CurrentFrameIndex = 0;
                }
                else
                {
                    ent.CurrentFrameIndex = scene.FrameSpriteIDs.Length - 1;
                }
            }

            string frameID = scene.FrameSpriteIDs[ent.CurrentFrameIndex];
            Sprite frameSprite = SysResource.Instance.GetResource<Sprite>(frameID);
            if (frameSprite != null)
            {
                ent.EntTargetSprite = frameSprite;
            }
        }
    }

    // ---------- Door ---------- //

    public void TickCompDoor(EntState ent)
    {
        if (ent == null || ent.RefEntity == null || !ent.RefEntity.UsesCompDoor) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Check if the player is within the door entity

            EntState playerEnt = SysLevelEntities.Instance.stateManager.GetEntityWithName("Player");
            if (playerEnt != null && ent.IsEntOverlapping(playerEnt))
            {
                Debug.Log("Player entered door: " + ent.RefEntity.EntityName);

                ResCfgLevel targetLevel = SysResource.Instance.GetResourceConfig<ResCfgLevel>(ent.RefEntity.CompDoor.TargetLevel);
                SysLevel.Instance.LoadLevel(targetLevel);
            }
        }
    }

    // ---------- AI ---------- //

    public void TickCompAI(EntState ent)
    {
        if (ent == null || ent.RefEntity == null || !ent.RefEntity.UsesCompAI) return;

        ResCfgEntCompAI aiConfig = ent.RefEntity.CompAI;
        if (aiConfig == null) return;

        // -------- Control & Status Handling -------- //
        if (ent.EntHealth <= 0f)
        {
            ent.HasControl = false;
            ent.Velocity = Vector2.zero; // stop completely when dead
        }
        else if (ent.InvulnerabilityTimer > 0f)
        {
            ent.HasControl = false;
            ent.Velocity = Vector2.zero; // freeze movement when hurt/invulnerable
        }
        else
        {
            ent.HasControl = true;
        }

        // -------- Animation Scene Switching -------- //
        if (ent.RefEntity.UsesCompAnimation)
        {
            string desiredScene;
            if (ent.EntHealth <= 0f) desiredScene = "Dead";
            else if (ent.InvulnerabilityTimer > 0f) desiredScene = "Hurt";
            else if (!ent.IsGrounded) desiredScene = "Jump";
            else if (ent.Velocity.magnitude > 0.1f) desiredScene = "Walk";
            else desiredScene = "Idle";

            if (ent.CurrentAnimationScene != desiredScene)
            {
                ent.CurrentAnimationScene = desiredScene;
                ent.CurrentFrameIndex = 0;
                ent.AnimationTimer = 0f;

                var anim = SysResource.Instance.GetResourceConfig<ResCfgAnimationCollection>(ent.RefEntity.CompAnimation.AnimationID);
                if (anim != null)
                {
                    var scene = Array.Find(anim.AnimationScenes, s => s.SceneID == desiredScene);
                    if (scene != null && scene.FrameSpriteIDs.Length > 0)
                    {
                        string frameID = scene.FrameSpriteIDs[0];
                        Sprite frameSprite = SysResource.Instance.GetResource<Sprite>(frameID);
                        if (frameSprite != null)
                            ent.EntTargetSprite = frameSprite;
                    }
                }
            }
        }

        // -------- AI Logic -------- //
        if (ent.HasControl)
        {
            switch (aiConfig.Type)
            {
                case 0:
                    TickAIPatrol(ent, aiConfig);
                    break;
                // More AI types can be added here
                default:
                    break;
            }
        }
    }

    private void TickAIPatrol(EntState ent, ResCfgEntCompAI aiConfig)
    {
        if (ent == null || aiConfig == null) return;

        float patrolSpeed = aiConfig.MoveSpeed;

        float direction = ent.LastAIDirection;
        if (Mathf.Approximately(direction, 0f)) direction = 1f;

        // ---- Collision with walls ----
        CollisionInfo hit = SysLevelPhysics.Instance.GetEntCollisionAtPos(ent, ent.EntPosition + new Vector2(direction * 0.1f, 0f));
        if (hit != null && hit.CollisionRect != null)
        {
            direction = -direction;
        }

        // ---- Collision with other living AIs ----
        EntState[] allEntities = SysLevelEntities.Instance.stateManager.GetEntities();
        foreach (var other in allEntities)
        {
            if (other == null || other == ent) continue;
            if (!other.RefEntity.UsesCompAI) continue;
            if (other.EntHealth <= 0f) continue; // skip dead AIs

            if (ent.IsEntOverlapping(other))
            {
                // decide direction based on relative X positions
                if (other.EntPosition.x > ent.EntPosition.x)
                    direction = -1f; // other is to the right, move left
                else
                    direction = 1f;  // other is to the left, move right

                break;
            }
        }

        // Apply velocity directly from resolved direction
        ent.LastAIDirection = direction;
        ent.Velocity = new Vector2(patrolSpeed * direction, ent.Velocity.y);
        ent.EntSpriteFlipped = direction > 0f;
    }


    // ---------- Damage Emitter ---------- //

    private void TickCompDamageEmitter(EntState emitterEnt)
    {
        if (emitterEnt == null || emitterEnt.RefEntity == null) return;
        var emitter = emitterEnt.RefEntity.CompDamageEmitter;
        if (emitter == null) return;

        // Prevent emitter if entity is invulnerable or frozen
        if (emitterEnt.InvulnerabilityTimer > 0f) return;
        if (emitterEnt.HasControl == false) return;

        EntState[] allEntities = SysLevelEntities.Instance.stateManager.GetEntities();

        foreach (EntState targetEnt in allEntities)
        {
            if (targetEnt == null || targetEnt == emitterEnt || targetEnt.RefEntity == null) continue;
            if (!targetEnt.RefEntity.UsesCompDamageReceiver) continue;

            var receiver = targetEnt.RefEntity.CompDamageReceiver;
            if (targetEnt.InvulnerabilityTimer > 0f) continue;

            // Tag filtering
            if (emitter.TargetTags != null && emitter.TargetTags.Length > 0)
            {
                bool tagMatch = targetEnt.RefEntity.EntityTags?.Any(tag => emitter.TargetTags.Contains(tag)) ?? false;
                if (!tagMatch) continue;
            }

            // Overlap check
            if (!emitterEnt.IsEntOverlapping(targetEnt)) continue;

            // Direction check
            Vector2 delta = targetEnt.EntPosition - emitterEnt.EntPosition;
            bool directionValid = false;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                if (delta.x > 0 && emitter.damageDirRight) directionValid = true;
                else if (delta.x < 0 && emitter.damageDirLeft) directionValid = true;
            }
            else
            {
                if (delta.y > 0 && emitter.damageDirUp) directionValid = true;
                else if (delta.y < 0 && emitter.damageDirDown) directionValid = true;
            }
            if (!directionValid) continue;

            // ---- Apply damage ----
            float prevHealth = targetEnt.EntHealth;
            targetEnt.EntHealth -= emitter.Damage;
            targetEnt.InvulnerabilityTimer = receiver.InvulnerabilityTime;

            // NEW: record hit info (only if entity was alive before hit)
            if (prevHealth > 0f)
            {
                targetEnt.WasDamagedRecently = true;
                targetEnt.LastDamagedTime = Time.time;
                targetEnt.LastDamageSourceDir = (emitterEnt.EntPosition - targetEnt.EntPosition).normalized;
            }

            Debug.Log($"Entity '{targetEnt.RefEntity.EntityName}' took {emitter.Damage} damage from '{emitterEnt.RefEntity.EntityName}'");
        }
    }



}
