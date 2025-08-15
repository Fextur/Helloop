using UnityEngine;
using System.Collections;
using Helloop.StateMachines;
using Helloop.Audio;
using Helloop.Systems;

namespace Helloop.Enemies.States
{
    public class EnemyAttackState : IState<Enemy>
    {
        private bool isAttacking;
        private float lastAttackTime;
        private Coroutine attackCoroutine;

        private Vector3 committedForwardXZ;


        public void OnEnter(Enemy enemy)
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
            {
                enemy.Agent.isStopped = true;
                enemy.Agent.ResetPath();
                enemy.Agent.updateRotation = false;
            }

            StartAttack(enemy);
        }

        public void Update(Enemy enemy)
        {
            if (enemy.IsDead) return;

            if (!isAttacking)
            {
                FacePlayer(enemy);
            }

            UpdateAnimation(enemy);
            enemy.SetAttackingStatus(isAttacking);

            if (!isAttacking && CanAttackAgain(enemy)
        && enemy.IsInAttackRange()
        && enemy.CanSeePlayer()
        && IsWithinCurrentFacingCone(enemy, enemy.EnemyData.attackConeDegrees))
            {
                StartAttack(enemy);
            }
        }

        private bool IsWithinCurrentFacingCone(Enemy enemy, float coneDegrees)
        {
            if (enemy.Player == null) return false;

            Vector3 toPlayer = enemy.Player.position - enemy.transform.position;
            toPlayer.y = 0f;
            float sqrMag = toPlayer.sqrMagnitude;

            if (sqrMag < 0.0001f) return true;              // overlap counts as inside
            if (coneDegrees >= 359.9f) return true;         // omnidirectional

            Vector3 forward = enemy.transform.forward;      // current facing (not committed)
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return true;

            float cosHalf = Mathf.Cos(0.5f * coneDegrees * Mathf.Deg2Rad);
            float dot = Vector3.Dot(forward.normalized, toPlayer.normalized);
            return dot >= cosHalf;
        }

        public void OnExit(Enemy enemy)
        {
            if (attackCoroutine != null)
            {
                enemy.StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            isAttacking = false;
            enemy.SetAttackingStatus(false);

            if (enemy.Animator != null)
            {
                enemy.Animator.ResetTrigger("Attack");
            }

            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
            {
                enemy.Agent.updateRotation = true;
            }
        }

        private void StartAttack(Enemy enemy)
        {
            if (isAttacking) return;

            isAttacking = true;
            lastAttackTime = Time.time;

            if (enemy.Animator != null)
            {
                enemy.Animator.SetTrigger("Attack");
            }
            Vector3 fwd = enemy.transform.forward;
            fwd.y = 0f;
            committedForwardXZ = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            attackCoroutine = enemy.StartCoroutine(AttackSequence(enemy));
        }

        private IEnumerator AttackSequence(Enemy enemy)
        {
            float soundDelay = Mathf.Max(0f, enemy.EnemyData.attackDamageDelay - 0.1f);

            if (soundDelay > 0f)
            {
                yield return new WaitForSeconds(soundDelay);
            }

            PlayAttackAudio(enemy);

            float remainingDamageDelay = enemy.EnemyData.attackDamageDelay - soundDelay;
            if (remainingDamageDelay > 0f)
            {
                yield return new WaitForSeconds(remainingDamageDelay);
            }

            DealDamage(enemy);

            float remainingAnimationTime = enemy.EnemyData.attackAnimationDuration - enemy.EnemyData.attackDamageDelay;
            if (remainingAnimationTime > 0)
            {
                yield return new WaitForSeconds(remainingAnimationTime);
            }

            isAttacking = false;
            attackCoroutine = null;
        }

        private void DealDamage(Enemy enemy)
        {
            if (enemy.IsDead) return;

            bool canStillSeePlayer = enemy.CanSeePlayer();
            bool isInDamageRange = enemy.IsInDamageRange();

            if (isInDamageRange && canStillSeePlayer && IsInsideCommittedCone(enemy))
            {
                if (enemy.PlayerSystem != null)
                {
                    enemy.PlayerSystem.TakeDamage(enemy.ScaledDamage);
                }
            }
        }

        private bool IsInsideCommittedCone(Enemy enemy)
        {
            if (enemy.Player == null) return false;

            Vector3 toPlayer = enemy.Player.position - enemy.transform.position;
            toPlayer.y = 0f;
            float sqrMag = toPlayer.sqrMagnitude;

            if (sqrMag < 0.0001f) return true;              // overlap counts as inside

            float cone = Mathf.Clamp(enemy.EnemyData.attackConeDegrees, 0f, 360f);
            if (cone >= 359.9f) return true;                // omnidirectional

            Vector3 dir = toPlayer.normalized;
            float cosHalf = Mathf.Cos(0.5f * cone * Mathf.Deg2Rad);
            float dot = Vector3.Dot(committedForwardXZ, dir);
            return dot >= cosHalf;
        }

        private void FacePlayer(Enemy enemy)
        {
            if (enemy.Player != null)
            {
                Vector3 direction = (enemy.Player.position - enemy.transform.position).normalized;
                direction.y = 0f;

                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRotation, Time.deltaTime * 5f);
                }
            }
        }

        private void UpdateAnimation(Enemy enemy)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.SetFloat("Speed", 0f);
            }
        }

        private bool CanAttackAgain(Enemy enemy)
        {
            return Time.time >= lastAttackTime + enemy.EnemyData.attackCooldown;
        }

        private void PlayAttackAudio(Enemy enemy)
        {
            if (enemy.EnemyData?.hittingSound != null)
            {
                enemy.PlayAudio(enemy.EnemyData.hittingSound);
            }
        }
    }
}