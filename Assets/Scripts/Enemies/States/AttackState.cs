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

        public void OnEnter(Enemy enemy)
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = true;
            }

            StartAttack(enemy);
        }

        public void Update(Enemy enemy)
        {
            if (enemy.IsDead) return;

            FacePlayer(enemy);
            UpdateAnimation(enemy);

            enemy.SetAttackingStatus(isAttacking);

            if (!isAttacking && CanAttackAgain(enemy))
            {
                StartAttack(enemy);
            }
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
            bool canStillSeePlayer = enemy.CanSeePlayer();
            bool isInDamageRange = enemy.IsInDamageRange();

            if (isInDamageRange && canStillSeePlayer && !enemy.IsDead)
            {
                if (enemy.PlayerSystem != null)
                {
                    enemy.PlayerSystem.TakeDamage(enemy.ScaledDamage);
                }
            }
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