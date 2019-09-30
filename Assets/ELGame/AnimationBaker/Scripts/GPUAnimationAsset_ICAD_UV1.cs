using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.Tools.AnimationBaker
{

    public class SoldierDataBlock
    {
        public GPUAnimationAsset_ICAD_UV1 animationAsset;

        public int soldiersCount;

        public Matrix4x4[] matrixs;             //位移矩阵
        public float[] moveSpeedRate;           //移动速率(0~1)

        public float[] atkTimer;                //攻击计时器
        public float atkAnimLength;             //攻击时长

        public float[] deathTimer;              //死亡计时器
        public float deathAnimLength;           //死亡时长

        public float[] isAttack;                //是否攻击
        public float[] isDeath;                 //是否死亡

        public void Reset(int count)
        {
            atkAnimLength = animationAsset.attackAnimLength;
            deathAnimLength = animationAsset.deathAnimLength;

            soldiersCount = count;

            matrixs = new Matrix4x4[count];
            moveSpeedRate = new float[count];
            atkTimer = new float[count];
            deathTimer = new float[count];
            isAttack = new float[count];
            isDeath = new float[count];
        }

        //渲染更新
        public void UpdateRenderer()
        {
            animationAsset?.Prepare();
            animationAsset?.SetFloatArray("_MoveSpeed", moveSpeedRate);
            animationAsset?.SetFloatArray("_AttackTimer", atkTimer);
            animationAsset?.SetFloatArray("_DeathTimer", deathTimer);
            animationAsset?.SetFloatArray("_IsAttack", isAttack);
            animationAsset?.SetFloatArray("_IsDeath", isDeath);
            animationAsset?.Draw(matrixs);
        }
    }

    public class GPUAnimationAsset_ICAD_UV1
        : ScriptableObject
    {
        public Material material;
        public Mesh mesh;
        public MaterialPropertyBlock MaterialBlock;

        public float attackAnimLength;  //攻击动画时长
        public float deathAnimLength;   //死亡动画时长

        public void Prepare()
        {
            MaterialBlock = MaterialBlock ?? new MaterialPropertyBlock();
            MaterialBlock.Clear();
        }

        public void SetFloatArray(string key, float[] values)
        {
            MaterialBlock = MaterialBlock ?? new MaterialPropertyBlock();
            MaterialBlock?.SetFloatArray(key, values);
        }

        //渲染（需要在Update里一直调用）
        public void Draw(Matrix4x4[] matrixs)
        {
            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                matrixs,
                matrixs.Length,
                MaterialBlock);
        }

        //创建数据块
        public SoldierDataBlock CreateDataBlock(int dataCount)
        {
            SoldierDataBlock soldierDataBlock = new SoldierDataBlock();
            soldierDataBlock.animationAsset = this;
            soldierDataBlock.Reset(dataCount);
            return soldierDataBlock;
        }
    }
}