using UnityEngine;
using Helloop.StateMachines;
using Helloop.Audio;

namespace Helloop.Enemies.States
{
    public class EnemyChaseState : IState<Enemy>
    {
        private float lastPlayerSightTime;
        private Vector3 lastKnownPlayerPosition;

        public void OnEnter(Enemy enemy)
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = false;
            }

            lastPlayerSightTime = Time.time;
            enemy.HasSeenPlayer = true;

            PlayChaseAudio(enemy);
        }

        public void Update(Enemy enemy)
        {
            if (enemy.IsDead) return;

            UpdatePlayerTracking(enemy);
            HandleChaseMovement(enemy);
            UpdateAnimation(enemy);
        }

        public void OnExit(Enemy enemy)
        {
            enemy.LastSeenPlayerTime = lastPlayerSightTime;
        }

        private void UpdatePlayerTracking(Enemy enemy)
        {
            enemy.UpdateCachedCanSeePlayer();

            if (enemy.CanSeePlayer())
            {
                lastPlayerSightTime = Time.time;
                enemy.LastSeenPlayerTime = lastPlayerSightTime;
                lastKnownPlayerPosition = enemy.Player.position;
            }
        }

        private void HandleChaseMovement(Enemy enemy)
        {
            if (enemy.Agent == null) return;

            Vector3 targetPosition = enemy.CanSeePlayer() ?
                enemy.Player.position : lastKnownPlayerPosition;

            enemy.Agent.SetDestination(targetPosition);
        }

        private void UpdateAnimation(Enemy enemy)
        {
            if (enemy.Animator != null && enemy.Agent != null)
            {
                float speed = enemy.Agent.velocity.magnitude;
                enemy.Animator.SetFloat("Speed", speed);
            }
        }

        private void PlayChaseAudio(Enemy enemy)
        {
            if (enemy.EnemyData?.idleSound != null)
            {
                enemy.PlayAudio(enemy.EnemyData.idleSound, 1f);
            }
        }
    }
}