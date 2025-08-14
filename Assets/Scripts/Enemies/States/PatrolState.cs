using UnityEngine;
using Helloop.StateMachines;
using Helloop.Audio;

namespace Helloop.Enemies.States
{
    public class EnemyPatrolState : IState<Enemy>
    {
        private int currentPatrolIndex;
        private float lastPatrolTime;
        private bool waitingAtPoint;

        public void OnEnter(Enemy enemy)
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
            {
                enemy.Agent.isStopped = false;
            }

            InitializePatrol(enemy);
            PlayPatrolAudio(enemy);
        }

        public void Update(Enemy enemy)
        {
            if (enemy.IsDead) return;

            HandlePatrolMovement(enemy);
            UpdateAnimation(enemy);
            CheckForPlayer(enemy);
        }

        public void OnExit(Enemy enemy)
        {
            waitingAtPoint = false;
        }

        private void InitializePatrol(Enemy enemy)
        {
            if (enemy.PatrolPoints.Count > 0)
            {
                currentPatrolIndex = 0;
                MoveToCurrentPatrolPoint(enemy);
            }
            else
            {
                MoveToReturnPoint(enemy);
            }
        }

        private void HandlePatrolMovement(Enemy enemy)
        {
            if (enemy.PatrolPoints.Count == 0)
            {
                MoveToReturnPoint(enemy);
                return;
            }

            if (enemy.Agent.isActiveAndEnabled && !enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
            {
                if (!waitingAtPoint)
                {
                    waitingAtPoint = true;
                    lastPatrolTime = Time.time;
                }
                else if (Time.time - lastPatrolTime >= enemy.PatrolDelay)
                {
                    MoveToNextPatrolPoint(enemy);
                }
            }
        }

        private void MoveToCurrentPatrolPoint(Enemy enemy)
        {
            if (enemy.PatrolPoints.Count > 0 && currentPatrolIndex < enemy.PatrolPoints.Count)
            {
                enemy.Agent.SetDestination(enemy.PatrolPoints[currentPatrolIndex].position);
                waitingAtPoint = false;
            }
        }

        private void MoveToNextPatrolPoint(Enemy enemy)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % enemy.PatrolPoints.Count;
            MoveToCurrentPatrolPoint(enemy);
        }

        private void MoveToReturnPoint(Enemy enemy)
        {
            if (enemy.ReturnPoint != null && enemy.Agent.isActiveAndEnabled)
            {
                enemy.Agent.SetDestination(enemy.ReturnPoint.position);
            }
        }

        private void UpdateAnimation(Enemy enemy)
        {
            if (enemy.Animator != null && enemy.Agent != null && enemy.Agent.isActiveAndEnabled)
            {
                float speed = enemy.Agent.velocity.magnitude;
                enemy.Animator.SetFloat("Speed", speed);
            }
        }

        private void CheckForPlayer(Enemy enemy)
        {
            enemy.UpdateCachedCanSeePlayer();
        }

        private void PlayPatrolAudio(Enemy enemy)
        {
            if (enemy.EnemyData?.idleSound != null)
            {
                enemy.PlayAudio(enemy.EnemyData.idleSound, 0.5f);
            }
        }
    }
}