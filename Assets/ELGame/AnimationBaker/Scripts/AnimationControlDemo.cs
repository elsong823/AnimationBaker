using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.Tools.AnimationBaker
{
#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(AnimationControlDemo))]
    public class AnimationControlDemoEditor
        :Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            AnimationControlDemo ctrl = target as AnimationControlDemo;

            if (GUILayout.Button("Attack"))
                ctrl.PlayAttack();

            else if (GUILayout.Button("Death"))
                ctrl.PlayDeath();

            else if (GUILayout.Button("Reset"))
                ctrl.ResetAnimationState();
        }
    }


#endif

    public class AnimationControlDemo
        : MonoBehaviour
    {
        [SerializeField] private GPUAnimationAsset_ICAD_UV1 animationAsset;

        private SoldierDataBlock soldierDataBlock;
        
        [Header("MoveSpeed"), Range(0f, 1f)]
        public float moveSpeed;

        private bool isAttack;
        private bool isDeath;

        private float attackTimer;
        private float deathTimer;
        
        private void Awake()
        {
            soldierDataBlock = animationAsset?.CreateDataBlock(1);

            if(soldierDataBlock == null)
            {
                Debug.LogErrorFormat("Awake animation control demo faile. Asset is null");
            }
            soldierDataBlock.Reset(1);

            soldierDataBlock.atkAnimLength = animationAsset.attackAnimLength;
            soldierDataBlock.deathAnimLength = animationAsset.deathAnimLength;

            soldierDataBlock.matrixs[0] = transform.localToWorldMatrix;
        }

        public void PlayAttack()
        {
            if (isDeath)
                return;

            isAttack = true;
            attackTimer = 0f;
        }

        public void PlayDeath()
        {
            isAttack = false;
            isDeath = true;
            deathTimer = 0f;
        }

        public void ResetAnimationState()
        {
            isAttack = false;
            isDeath = false;
        }

        private void Update()
        {
            if(isAttack)
            {
                attackTimer += Time.deltaTime;
                if(attackTimer >= animationAsset.attackAnimLength)
                {
                    isAttack = false;
                    attackTimer = 0f;
                }
            }
            else if(isDeath)
            {
                deathTimer += Time.deltaTime;
                if (deathTimer >= animationAsset.deathAnimLength)
                {
                    deathTimer = animationAsset.deathAnimLength;
                }
            }

            //更新数据
            soldierDataBlock.moveSpeedRate[0] = moveSpeed;
            soldierDataBlock.atkTimer[0] = attackTimer;
            soldierDataBlock.deathTimer[0] = deathTimer;
            soldierDataBlock.isAttack[0] = isAttack ? 1f : 0f;
            soldierDataBlock.isDeath[0] = isDeath ? 1f : 0f;

            //更新绘制
            soldierDataBlock?.UpdateRenderer();
        }
    }
}