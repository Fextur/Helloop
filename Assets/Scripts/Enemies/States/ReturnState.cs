using UnityEngine;
using Helloop.StateMachines;
using Helloop.Audio;

namespace Helloop.Enemies.States
{
    public class EnemyReturnState : IState<Enemy>
    {
        public void OnEnter(Enemy enemy)
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = false;
            }

            enemy.IsReturning = true;
            StartReturnMovement(enemy);
            PlayReturnAudio(enemy);
        }

        public void Update(Enemy enemy)
        {
            if (enemy.IsDead) return;

            UpdateReturnMovement(enemy);
            UpdateAnimation(enemy);
            CheckForPlayer(enemy);
        }

        public void OnExit(Enemy enemy)
        {
            enemy.IsReturning = false;
        }

        private void StartReturnMovement(Enemy enemy)
        {
            if (enemy.ReturnPoint != null && enemy.Agent != null)
            {
                enemy.Agent.SetDestination(enemy.ReturnPoint.position);
            }
        }

        private void UpdateReturnMovement(Enemy enemy)
        {
            if (enemy.ReturnPoint != null && enemy.Agent != null)
            {
                if (Vector3.Distance(enemy.transform.position, enemy.ReturnPoint.position) < 1f)
                {
                    enemy.HasReturnedToBase = true;
                }
            }
        }

        private void UpdateAnimation(Enemy enemy)
        {
            if (enemy.Animator != null && enemy.Agent != null)
            {
                float speed = enemy.Agent.velocity.magnitude;
                enemy.Animator.SetFloat("Speed", speed);
            }
        }

        private void CheckForPlayer(Enemy enemy)
        {
            enemy.UpdateCachedCanSeePlayer();
        }

        private void PlayReturnAudio(Enemy enemy)
        {
            if (enemy.EnemyData?.idleSound != null)
            {
                enemy.PlayAudio(enemy.EnemyData.idleSound, 0.6f);
            }
        }
    }
}