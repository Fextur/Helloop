using UnityEngine;
using Helloop.StateMachines;
using Helloop.Audio;

namespace Helloop.Enemies.States
{
    public class EnemyDeathState : IState<Enemy>
    {
        private bool deathSequenceStarted;

        public void OnEnter(Enemy enemy)
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
            {
                enemy.Agent.isStopped = true;
            }

            PlayDeathAudio(enemy);
            PlayDeathAnimation(enemy);
            NotifyRoomController(enemy);

            deathSequenceStarted = true;
        }

        public void Update(Enemy enemy)
        {
            if (!deathSequenceStarted) return;
        }

        public void OnExit(Enemy enemy)
        {
        }

        private void PlayDeathAudio(Enemy enemy)
        {
            if (enemy.EnemyData?.deathSound != null)
            {
                enemy.PlayAudio(enemy.EnemyData.deathSound);
            }
        }

        private void PlayDeathAnimation(Enemy enemy)
        {
            if (enemy.Animator != null)
            {
                enemy.Animator.ResetTrigger("Attack");
                enemy.Animator.ResetTrigger("Hit");
                enemy.Animator.SetTrigger("Death");
            }
        }

        private void NotifyRoomController(Enemy enemy)
        {
            if (enemy.AssignedRoomController != null)
            {
                enemy.AssignedRoomController.NotifyEnemyDeath(enemy.gameObject);
            }
        }
    }
}