using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TMProEffect
{
    /// <summary>
    /// tmp effect material
    /// </summary>
    public class TMPEffectMaterial
    {
        #region material related
        /// <summary>
        /// material instance
        /// </summary>
        public Material EffectMaterial { get; private set; }
        /// <summary>
        /// material id
        /// </summary>
        public int ID { get; private set; }
        /// <summary>
        /// shader id
        /// </summary>
        public int ShaderID { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        TMPEffectMaterial(Material material, int id, int shader_id)
        {
            EffectMaterial = material;
            ID = id;
            ShaderID = shader_id;
        }
        #endregion

        #region material pool
        /// <summary>
        /// material reference dictionary
        /// </summary>
        static Dictionary<int, Material> m_refMatDic = new Dictionary<int, Material>();
        /// <summary>
        /// material pool
        /// </summary>
        static Dictionary<int, Dictionary<int, TMPEffectMaterial>> pool = new Dictionary<int, Dictionary<int, TMPEffectMaterial>>();
        /// <summary>
        /// material reference count
        /// </summary>
        int refCount = 1;

        /// <summary>
        /// load effect material
        /// </summary>
        static public TMPEffectMaterial LoadEffectMaterial(Material material_ref, System.Action<Material> setMaterialCallback)
        {
            Profiler.BeginSample("CREATE TMP MAT");

            int shader_id = material_ref.shader.GetHashCode();
            Material m_refMat;
            if (!m_refMatDic.ContainsKey(shader_id))
            {
                m_refMat = new Material(material_ref);
                m_refMatDic.Add(shader_id, m_refMat);
            }
            else
            {
                m_refMat = m_refMatDic[shader_id];
                if (m_refMat == null)
                {
                    //if material is null, create a new one
                    m_refMat = new Material(material_ref);
                    m_refMatDic[shader_id] = m_refMat;
                }
            }
            m_refMat.CopyPropertiesFromMaterial(material_ref);

            setMaterialCallback?.Invoke(m_refMat);

            int id = m_refMat.ComputeCRC();

            Profiler.EndSample();

            if (!pool.ContainsKey(shader_id))
            {
                pool.Add(shader_id, new Dictionary<int, TMPEffectMaterial>());
            }

            //if has same material, return it
            var one_pool = pool[shader_id];
            if (one_pool.ContainsKey(id))
            {
                if (one_pool[id].EffectMaterial == null)
                {
                    //如果材质球已经被释放
                    var n_mat = new Material(m_refMat)
                    {
                        name = material_ref.name
                    };
                    one_pool[id].EffectMaterial = n_mat;
                }
                one_pool[id].refCount++;
                return one_pool[id];
            }
            var new_mat = new Material(m_refMat)
            {
                name = material_ref.name
            };
            var tmpMat = new TMPEffectMaterial(new_mat, id, shader_id);
            one_pool.Add(id, tmpMat);
            return tmpMat;
        }

        /// <summary>
        /// release effect material
        /// </summary>
        /// <param name="material"></param>
        static public void ReleaseEffectMaterial(TMPEffectMaterial tmpEffectMat)
        {
            var poolMat = pool[tmpEffectMat.ShaderID][tmpEffectMat.ID];
            poolMat.refCount--;
            if (poolMat.refCount < 1)
            {
                DestroyMaterial(pool[tmpEffectMat.ShaderID][tmpEffectMat.ID].EffectMaterial);
                pool[tmpEffectMat.ShaderID].Remove(tmpEffectMat.ID);
                if(pool[tmpEffectMat.ShaderID].Count == 0)
                {
                    DestroyMaterial(m_refMatDic[tmpEffectMat.ShaderID]);
                    m_refMatDic.Remove(tmpEffectMat.ShaderID);
                    pool.Remove(tmpEffectMat.ShaderID);
                }
            }
        }

        static void DestroyMaterial(Material mat)
        {
#if UNITY_EDITOR
            //destroy material asynchronously in editor mode
            //this is to avoid the inspector refresh issue
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(mat);
                }
                else
                {
                    Object.DestroyImmediate(mat);
                }
            };
#else
            if (Application.isPlaying)
            {
                Object.Destroy(mat);
            }
            else
            {
                Object.DestroyImmediate(mat);
            }
#endif
        }
        #endregion
    }
}