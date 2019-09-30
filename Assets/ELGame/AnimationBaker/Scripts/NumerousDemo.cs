using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.Tools.AnimationBaker
{
    public class SoldierRandomItem
    {
        public SoldierDataBlock soldierDataBlock;
        public int idx;
        public float moveSpeed;

        private bool isAttack;
        private bool isDeath;

        private float attackTimer;
        private float deathTimer;

        private float attackLen;
        private float deathLen;

        private float randomTimer = 0f;

        public SoldierRandomItem(SoldierDataBlock soldierDataBlock, int idx, float atkLen, float deathLen)
        {
            this.soldierDataBlock = soldierDataBlock;
            this.idx = idx;
            this.moveSpeed = Random.Range(0f, 1f);
            this.randomTimer = Random.Range(-3f, 0f);
            this.attackLen = atkLen;
            this.deathLen = deathLen;
            this.isAttack = false;
            this.isDeath = false;
        }

        public void UpdateRandom(float timeElapsed)
        {
            randomTimer += timeElapsed;
            if (randomTimer >= 0f)
            {
                if (isAttack)
                {
                    attackTimer += timeElapsed;
                    if (attackTimer >= attackLen)
                        RandomState();
                }
                else if (isDeath)
                {
                    deathTimer += timeElapsed;
                    if (deathTimer >= deathLen)
                        RandomState();
                }
                else if(randomTimer > 2f)
                    RandomState();
            }

            soldierDataBlock.moveSpeedRate[idx] = moveSpeed;
            soldierDataBlock.isAttack[idx] = isAttack ? 1f : 0f;
            soldierDataBlock.atkTimer[idx] = attackTimer;
            soldierDataBlock.isDeath[idx] = isDeath ? 1f : 0f;
            soldierDataBlock.deathTimer[idx] = deathTimer;
        }

        private void RandomState()
        {
            int state = Random.Range(0, 2);
            //移动
            if (state == 0)
            {
                isAttack = false;
                attackTimer = 0f;
                isDeath = false;
                deathTimer = 0f;

                randomTimer = 0f;
            }
            //攻击
            else if (state == 1)
            {
                isAttack = true;
                attackTimer = 0f;

                randomTimer = 0f;
            }
            //死亡（太乱了先不用了）
            else
            {
                isDeath = true;
                deathTimer = 0f;

                randomTimer = 0f;
            }
        }
    }

    public class NumerousDemo 
        : MonoBehaviour
    {
        [Header("行、列")]
        [Header("注意：一次DrawInstance只能包含1023个单位噢~")]
        public int rows;
        public int columns;

        [Header("间隔")]
        public float gap;

        [SerializeField] private GPUAnimationAsset_ICAD_UV1 animationAsset;
        private SoldierDataBlock soldierDataBlock;

        private SoldierRandomItem[] randomSoldiers;

        private void Start()
        {
            if (animationAsset == null)
            {
                Debug.LogError("Asset is null!");
                return;
            }

            soldierDataBlock = animationAsset.CreateDataBlock(rows * columns);
            randomSoldiers = new SoldierRandomItem[rows * columns];

            GenerateSoldiers();
        }

        private void GenerateSoldiers()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 pos = new Vector3(
                        c * gap,
                        0f,
                        r * gap);

                    soldierDataBlock.matrixs[r * columns + c] = Matrix4x4.Translate(pos);

                    randomSoldiers[r * columns + c] = new SoldierRandomItem(
                        soldierDataBlock,
                        r * columns + c,
                        animationAsset.attackAnimLength, 
                        animationAsset.deathAnimLength);
                }
            }
        }

        private void Update()
        {
            if (soldierDataBlock == null)
                return;

            foreach (var soldier in randomSoldiers)
                soldier.UpdateRandom(Time.deltaTime);
            
            soldierDataBlock.UpdateRenderer();
        }
    }
}