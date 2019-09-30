using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ELGame.Tools.AnimationBaker
{
    public class AnimationBakerWindow
         : EditorWindow
    {
        public class SmartMesh
        {
            public enum MeshType
            {
                SkinnedMesh,
                MeshFilter,
            }

            public MeshType meshType;   //网格类型
            public int vertexCount;     //顶点数量
            //骨骼
            public SkinnedMeshRenderer skinnedMesh = null;

            //普通
            public MeshFilter meshFilter = null;
            public Mesh meshFilterMesh = null;
            public MeshRenderer meshRenderer;

            public Texture mainTexture;   //主纹理
            public int uvIdx;             //使用的uv

            //合并网格输出
            public Mesh combineOutput = null;

            //动画网格输出
            public Mesh animationOutput = null;
            public Mesh tempMesh = null;


            //创建一个mesh节点
            public SmartMesh(SkinnedMeshRenderer skinnedMeshRenderer, MeshFilter filter)
            {
                if (skinnedMeshRenderer == null && filter != null)
                {
                    meshFilter = filter;
                    meshFilterMesh = meshFilter.sharedMesh;
                    vertexCount = meshFilterMesh.vertexCount;
                    meshRenderer = meshFilter.transform.GetComponent<MeshRenderer>();
                    meshType = MeshType.MeshFilter;
                }
                else if (skinnedMeshRenderer != null && filter == null)
                {
                    skinnedMesh = skinnedMeshRenderer;
                    vertexCount = skinnedMesh.sharedMesh.vertexCount;
                    meshType = MeshType.SkinnedMesh;
                }
                else
                    Debug.LogErrorFormat("创建SmartMesh失败啦！！");

                GetMainTexture();
            }

            private void GetMainTexture()
            {
                switch (meshType)
                {
                    case MeshType.SkinnedMesh:
                        mainTexture = skinnedMesh.sharedMaterial.mainTexture;
                        break;
                    case MeshType.MeshFilter:
                        mainTexture = meshRenderer.sharedMaterial.mainTexture;
                        break;
                    default:
                        break;
                }
                if (mainTexture == null)
                    Debug.LogError("获取主纹理失败！");
                else
                    Debug.Log("获取主纹理成功=>" + mainTexture.name);
            }

            //烘焙并的得到mesh
            public Mesh GetAnimationMesh(Matrix4x4 tranCenterMatrix)
            {
                tempMesh = tempMesh ?? new Mesh();
                animationOutput = animationOutput ?? new Mesh();

                CombineInstance combineInstance = new CombineInstance();

                switch (meshType)
                {
                    case MeshType.SkinnedMesh:
                        //蒙皮网格需要进行烘焙，这样保证出来的mesh在对应的位置上，否则直接获取shareMesh的话，会没有动作
                        skinnedMesh.BakeMesh(tempMesh);
                        combineInstance.mesh = tempMesh;
                        combineInstance.transform = tranCenterMatrix * skinnedMesh.localToWorldMatrix;
                        break;

                    case MeshType.MeshFilter:
                        combineInstance.mesh = meshFilterMesh;
                        combineInstance.transform = tranCenterMatrix * meshFilter.transform.localToWorldMatrix;
                        break;

                    default:
                        break;
                }

                animationOutput.Clear();
                animationOutput.CombineMeshes(new CombineInstance[1] { combineInstance });
                return animationOutput;
            }

            //获取合并实例
            //合并需要处理uv信息
            public CombineInstance GetCombineInstance(Matrix4x4 tranCenterMatrix, bool multipleTextureMode)
            {
                CombineInstance combineInstance = new CombineInstance();

                switch (meshType)
                {
                    case MeshType.SkinnedMesh:
                        //蒙皮网格需要进行烘焙，这样保证出来的mesh在对应的位置上，否则直接获取shareMesh的话，会没有动作
                        combineOutput = combineOutput ?? new Mesh();
                        skinnedMesh.BakeMesh(combineOutput);
                        combineInstance.transform = tranCenterMatrix * skinnedMesh.localToWorldMatrix;
                        break;

                    case MeshType.MeshFilter:
                        combineOutput = meshFilterMesh;
                        combineInstance.transform = tranCenterMatrix * meshFilter.transform.localToWorldMatrix;
                        break;

                    default:
                        break;
                }

                //处理uv
                //这里只考虑附带了两张贴图的情况
                //如果网格涉及两张贴图，则使用uv2的x作为图片lerp值
                //如果涉及多张贴图，还可开启更多的uv
                //可是说好只是用在杂兵身上的，怎么会带那么多贴图呢？？
                Vector2[] uv2 = new Vector2[combineOutput.vertexCount];
                for (int i = 0; i < combineOutput.vertexCount; i++)
                {
                    uv2[i].Set(uvIdx == 0 ? 0f : 1f, 0f);
                }
                combineOutput.SetUVs(1, new List<Vector2>(uv2));
                combineInstance.mesh = combineOutput;

                return combineInstance;
            }

            public string Desc()
            {
                switch (meshType)
                {
                    case MeshType.SkinnedMesh:
                        return string.Format("骨骼 网格{0}使用了名为{1}的纹理蒙皮，uv idx为{2}", skinnedMesh.name, mainTexture.name, uvIdx);
                    case MeshType.MeshFilter:
                        return string.Format("普通 网格{0}使用了名为{1}的纹理蒙皮，uv idx为{2}", meshFilter.name, mainTexture.name, uvIdx);
                    default:
                        return string.Format("信息错误");
                }
            }
        }
        
        [System.Serializable]
        public class AnimationClipNode
        {
            public string newName;
            public AnimationClip clip;
        }
        
        private static AnimationBakerWindow windowInstance;

        public GameObject modelGameObject;
        public Transform modelCenter;
        public List<AnimationClipNode> animationClips;
        
        private string MeshPath
        {
            get
            {
                return Path.Combine(OutputPath, string.Format("{0}.asset", modelGameObject.name));
            }
        }

        private string OutputPath
        {
            get
            {
                string outputParnentPath = "Assets/AnimationBakerOutput";
                string outputFolder = modelGameObject.name;
                string outputPath = Path.Combine(outputParnentPath, outputFolder);

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
                
                return outputPath;
            }
        }

        private string GetAnimationTextureAssetPath(string assetName)
        {
            return string.Format("{0}/{1}.asset", OutputPath, assetName);
        }

        [MenuItem("Tools/AnimationBaker", false)]
        static void CreateWindow()
        {
            windowInstance = GetWindow(typeof(AnimationBakerWindow)) as AnimationBakerWindow;
            windowInstance.title = "Animation Baker";
        }

        SerializedObject serializedObject;
        SerializedProperty spAnimationClips;

        private Mesh combinedMesh;
        private List<SkinnedMeshRenderer> skinnedMeshRenderers;
        private List<MeshFilter> meshFilters;
        private List<SmartMesh> smartMeshList;
        private bool multipleTexturesMode = false;

        private GameObject instanceGameObject;
        private Animation instanceAnimation;
        private List<Texture> usedTextures;
        private GPUAnimationAsset_ICAD_UV1 animationAsset;
        private Material mainMaterial;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            spAnimationClips = serializedObject.FindProperty("animationClips");
        }

        private void OnGUI()
        {
            serializedObject.Update();

            //开始检查是否有修改
            EditorGUI.BeginChangeCheck();
            
            modelGameObject = EditorGUILayout.ObjectField("Model:", modelGameObject, typeof(GameObject), true) as GameObject;
            modelCenter = EditorGUILayout.ObjectField("Center:", modelCenter, typeof(Transform), true) as Transform;

            //显示属性
            EditorGUILayout.PropertyField(spAnimationClips, true);

            //结束检查是否有修改
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                AnimationClip errorClip = null;
                int checkResult = CheckAnimationClip(out errorClip);
                if (checkResult == 1)
                    Debug.LogError("发现空AnimationClip!");
                else if(checkResult == 2 && errorClip != null)
                    Debug.LogError("存在不为legacy的动画片段！", errorClip);
            }

            if (GUILayout.Button("0.检查动画片段"))
                CheckAnimationClips();

            if (GUILayout.Button("1.生成动画资源"))
                BakeMesh();
        }

        //检查动画片段，必须是legacy
        private int CheckAnimationClip(out AnimationClip errorClip)
        {
            errorClip = null;

            if (animationClips != null && animationClips.Count > 0)
            {
                foreach (var item in animationClips)
                {
                    if (item.clip == null)
                        return 1;

                    else if (!item.clip.legacy)
                    {
                        errorClip = item.clip;
                        return 2;
                    }
                }
                return 0;
            }
            return 0;
        }

        //检查动画片段
        private void CheckAnimationClips()
        {
            if (animationClips == null || animationClips.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "并没有配置任何动画!", "呃...");
                return;
            }

            AnimationClip errorClip = null;
            int checkResult = CheckAnimationClip(out errorClip);
            if (checkResult == 1)
            {
                EditorUtility.DisplayDialog("警告", "发现空AnimationClip!", "呃...");
            }
            else if (checkResult == 2 && errorClip != null)
            {
                EditorUtility.DisplayDialog("警告", "存在不为legacy的动画片段!", "呃...");
                Debug.LogError("存在不为legacy的动画片段！", errorClip);
            }
            else
                EditorUtility.DisplayDialog("提示", "恭喜，动画片段检查通过", "同喜");
        }

        //开始烘焙网格
        private void BakeMesh()
        {

            if(modelCenter == null)
            {
                bool rst = EditorUtility.DisplayDialog("警告", "并没有选择中心节点，将会以自身根节点作为中心！", "好的", "别啊！");
                if (rst)
                    modelCenter = modelGameObject.transform;
                else
                    return;
            }

            //判断是否有有效的网格
            //蒙皮
            skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            //普通网格
            meshFilters = new List<MeshFilter>();
            
            smartMeshList = new List<SmartMesh>();

            if (modelGameObject == null)
            {
                EditorUtility.DisplayDialog("提示", "并没有选择任何单位呢，呵呵。", "呃...");
                return;
            }

            modelGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true, skinnedMeshRenderers);
            modelGameObject.GetComponentsInChildren<MeshFilter>(true, meshFilters);
            if (skinnedMeshRenderers.Count == 0 && meshFilters.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "并没有找到任何网格呢，呵呵。", "呃...");
                return;
            }

            //生成资源
            GenerateAnimationAsset();

            //生成材质球
            CreateMaterial();

            //这时就要生成一个临时对象了
            GenerateInstanceGameObject();

            //先把节点上的网格一通合并
            CombineMesh();

            if (combinedMesh == null)
                return;

            //生成动画纹理
            GenerateAnimationTextures();
            
            //清除临时内容
            Clean();

            EditorUtility.DisplayDialog("恭喜", "生成成功！", "感谢国家");
        }

        //0.生成动画资源
        private void GenerateAnimationAsset()
        {
            //生成动画资源
            string assetPath = string.Format("{0}/{1}_Animation.asset", OutputPath, modelGameObject.name).ToLower();
            animationAsset = AssetDatabase.LoadAssetAtPath<GPUAnimationAsset_ICAD_UV1>(assetPath);
            if (animationAsset == null)
            {
                animationAsset = ScriptableObject.CreateInstance<GPUAnimationAsset_ICAD_UV1>();
                AssetDatabase.CreateAsset(animationAsset, assetPath);
            }
            Debug.Log("正在创建动画资源到" + assetPath, animationAsset);
        }

        //1.创建动画所用材质球
        private void CreateMaterial()
        {
            //生成材质球
            string materialPath = string.Format("{0}/{1}_Animation.mat", OutputPath, modelGameObject.name).ToLower();
            mainMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mainMaterial == null)
            {
                mainMaterial = new Material(Shader.Find("EL_Shader/Shader_GPUAnimation_ICAD_UV1"));
                AssetDatabase.CreateAsset(mainMaterial, materialPath);
            }
            animationAsset.material = mainMaterial;

            EditorUtility.SetDirty(animationAsset);
        }

        //2.在世界中生成实例对象（用于播放动画）
        private void GenerateInstanceGameObject()
        {
            //准备一个动画播放器
            if (animationClips == null || animationClips.Count == 0)
            {
                EditorUtility.DisplayDialog("警告", "并没有配置任何动画信息！", "呃...");
                return;
            }
            
            //在场景中创建一个带动画控制器的实体
            PrepareGameObjectInScene();
        }

        //3.合并网格
        private void CombineMesh()
        {
            //开始合并...

            //获取顶点数量
            int totalVertexCount = 0;
            foreach (var smr in skinnedMeshRenderers)
                totalVertexCount += smr.sharedMesh.vertexCount;
            foreach (var mf in meshFilters)
                totalVertexCount += mf.sharedMesh.vertexCount;

            //最终网格
            Debug.LogFormat("开始合并:{0}个网格，其中{1}个蒙皮网格，{2}个普通网格，顶点数量{3}",
                skinnedMeshRenderers.Count + meshFilters.Count,
                skinnedMeshRenderers.Count,
                meshFilters.Count,
                totalVertexCount);

            combinedMesh = new Mesh();

            List<CombineInstance> combineInstanceList = new List<CombineInstance>();
            foreach (var smartMesh in smartMeshList)
            {
                combineInstanceList.Add(smartMesh.GetCombineInstance(modelCenter.worldToLocalMatrix, multipleTexturesMode));
            }

            combinedMesh.CombineMeshes(combineInstanceList.ToArray());

            //保存到本地
            AssetDatabase.CreateAsset(combinedMesh, MeshPath);
            
            Debug.Log("获得了网格=>" + modelGameObject.name, combinedMesh);

            //组装动画资源(网格）
            animationAsset.mesh = combinedMesh;
        }

        //4.生成动画纹理
        private void GenerateAnimationTextures()
        {
            Debug.LogFormat("开始生成动画纹理...");
            //开始生成动画纹理...
            List<AnimationState> animationStates = new List<AnimationState>(instanceAnimation.Cast<AnimationState>());
            foreach (var state in animationStates)
                GenerateAnimationTexture(instanceAnimation, state);

            mainMaterial.enableInstancing = true;
            mainMaterial.SetTexture("_MainTex", usedTextures[0]);
            if(multipleTexturesMode)
                mainMaterial.SetTexture("_SecondaryTex", usedTextures[1]);

            EditorUtility.SetDirty(mainMaterial);
        }

        //针对每一个动作单独生成纹理
        private void GenerateAnimationTexture(Animation animation, AnimationState state)
        {
            //生成动画纹理的名称
            string assetName = string.Format("{0}_{1}", modelGameObject.name, state.name);
            Debug.LogFormat("正在处理 {0}...", assetName);

            //播放并记录
            Debug.Log("播放动画" + state.name);
            animation.Play(state.name);

            AnimationClip clip = state.clip;

            //连接到材质球
            string keyLen = string.Format("_{0}Len", state.name);
            mainMaterial.SetFloat(keyLen, state.length);

            //原始需要帧数
            int originFrames = (int)(state.clip.frameRate * state.length);
            int generateFrames = Mathf.ClosestPowerOfTwo(originFrames);
            float frameGap = state.length / generateFrames;

            Debug.LogFormat("{0}的原始帧数为{1},处理后为{2},动画长度为{3:0.00},帧间隔为{4:0.00}", 
                state.name, 
                originFrames, 
                generateFrames,
                state.length,
                frameGap);

            //纹理宽度为顶点数量，高度为动作帧数
            int textureWidth = Mathf.NextPowerOfTwo(combinedMesh.vertexCount);
            int textureHeight = generateFrames;
            Texture2D animationTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false);

            for (int v = 0; v < textureHeight; v++)
            {
                //摆pose
                state.time = v * frameGap;
                //采样！！！
                animation.Sample();

                for (int u = 0; u < combinedMesh.vertexCount; u++)
                {
                    //这里必须按照之前合并网格的顺序来搞...
                    int relativeU = 0;
                    SmartMesh sm = null;
                    GetSmartMeshByU(u, ref relativeU, out sm);

                    if (sm == null)
                    {
                        EditorUtility.DisplayDialog("错误", "根据UV坐标获取网格信息失败！", "呃...");
                        return;
                    }
                    Mesh mesh = sm.GetAnimationMesh(modelCenter.worldToLocalMatrix);

                    Vector3 vertexPos = mesh.vertices[relativeU];
                    Color color = new Color(vertexPos.x, vertexPos.y, vertexPos.z);
                    //将位置保存为颜色
                    animationTexture.SetPixel(u, v, color);
                }
            }

            animationTexture.Apply();
            
            //保存
            AssetDatabase.CreateAsset(animationTexture, GetAnimationTextureAssetPath(assetName));
            Debug.Log("生成了动画纹理:" + assetName, animationTexture);

            //连接到材质球
            string keyTex = string.Format("_{0}Tex", state.name);
            mainMaterial.SetTexture(keyTex, animationTexture);

            if (state.name.ToLower().Contains("attack"))
                animationAsset.attackAnimLength = clip.length;
            else if (state.name.ToLower().Contains("death"))
            {
                //别问为什么，经验！
                animationAsset.deathAnimLength = clip.length * 0.95f;
            }
        }
        
        //通过采样坐标u获取对应的网格
        private void GetSmartMeshByU(int u, ref int relativeU, out SmartMesh mesh)
        {
            mesh = null;
            int uCursor = 0;
            relativeU = u;
            for (int i = 0; i < smartMeshList.Count; i++)
            {
                int vertexCount = smartMeshList[i].vertexCount;
                uCursor += vertexCount;
                //找到了u停留的Mesh，计算相对u
                if (u < uCursor)
                {
                    mesh = smartMeshList[i];
                    return;
                }
                else
                    relativeU -= vertexCount;
            }

            Debug.LogErrorFormat("根据坐标{0}获取模型数据失败！！！", u);
        }

        //在场景中准备一个节点
        private void PrepareGameObjectInScene()
        {
            instanceGameObject = GameObject.Instantiate<GameObject>(modelGameObject);
            instanceGameObject.name = "TEMP";

            Animation ac = instanceGameObject.GetComponent<Animation>();

            //移除旧的
            if (ac != null)
                DestroyImmediate(ac);

            //添加新的
            instanceAnimation = instanceGameObject.AddComponent<Animation>();

            //添加..
            foreach (var item in animationClips)
            {
                string animName = string.IsNullOrEmpty(item.newName) ? item.clip.name : item.newName;
                instanceAnimation.AddClip(item.clip, animName);
            }

            skinnedMeshRenderers.Clear();
            meshFilters.Clear();
            smartMeshList.Clear();
            instanceGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true, skinnedMeshRenderers);
            instanceGameObject.GetComponentsInChildren<MeshFilter>(true, meshFilters);

            usedTextures = new List<Texture>();
            
            foreach (var smr in skinnedMeshRenderers)
            {
                SmartMesh smartMesh = new SmartMesh(smr, null);
                var meshUseTexture = smartMesh.mainTexture;
                int idx = usedTextures.IndexOf(meshUseTexture);
                if (idx < 0)
                {
                    //用的是第几套uv呢
                    smartMesh.uvIdx = usedTextures.Count;
                    usedTextures.Add(meshUseTexture);
                }
                else
                    smartMesh.uvIdx = idx;

                smartMeshList.Add(smartMesh);
                Debug.Log(smartMesh.Desc());
            }
            foreach (var mf in meshFilters)
            {
                SmartMesh smartMesh = new SmartMesh(null, mf);
                var meshUseTexture = smartMesh.mainTexture;
                int idx = usedTextures.IndexOf(meshUseTexture);
                if (idx < 0)
                {
                    //用的是第几套uv呢
                    smartMesh.uvIdx = usedTextures.Count;
                    usedTextures.Add(meshUseTexture);
                }
                else
                    smartMesh.uvIdx = idx;

                smartMeshList.Add(smartMesh);
                Debug.Log(smartMesh.Desc());
            }
            multipleTexturesMode = usedTextures.Count > 1;
            Debug.LogFormat("使用的蒙皮纹理数为:{0}", usedTextures.Count);
            if (multipleTexturesMode)
                EditorUtility.DisplayDialog("注意", "注意，当前存在多套蒙皮纹理！", "好的");
        }

        //清除所有临时变量
        private void Clean()
        {
            DestroyImmediate(instanceGameObject);
            instanceAnimation = null;
            modelCenter = null;
            combinedMesh = null;
            skinnedMeshRenderers.Clear();
            meshFilters.Clear();
            smartMeshList.Clear();
            multipleTexturesMode = false;
            animationAsset = null;
            usedTextures.Clear();
            usedTextures = null;
            mainMaterial = null;

        }
    }
}