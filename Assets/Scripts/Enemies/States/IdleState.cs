using UnityEngine;
using Helloop.StateMachines;
using Helloop.Audio;

namespace Helloop.Enemies.States
{
    public class EnemyIdleState : IState<Enemy>
    {
        private float idleStartTime;

        public void OnEnter(Enemy enemy)
        {
            idleStartTime = Time.time;

            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = true;
            }

            if (enemy.Animator != null)
            {
                enemy.Animator.SetFloat("Speed", 0f);
            }

            PlayIdleAudio(enemy);
        }

        public void Update(Enemy enemy)
        {
            if (enemy.IsDead) return;

            UpdateAnimation(enemy);
            CheckEnvironment(enemy);
        }

        public void OnExit(Enemy enemy)
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = false;
            }
        }

        private void PlayIdleAudio(Enemy enemy)
        {
            if (enemy.EnemyData?.idleSound != null)
            {
                enemy.PlayAudio(enemy.EnemyData.idleSound, 0.7f);
            }
        }

        private void UpdateAnimation(Enemy enemy)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.SetFloat("Speed", 0f);
            }
        }

        private void CheckEnvironment(Enemy enemy)
        {
            enemy.UpdateCachedCanSeePlayer();
        }
    }
}