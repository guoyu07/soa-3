﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace NewLife.Serialization
{
    /// <summary>对象信息</summary>
    public class ObjectInfo
    {
        #region 属性
        /// <summary>默认上下文</summary>
        public static StreamingContext DefaultStreamingContext = new StreamingContext();
        #endregion

        #region 创建成员信息
        /// <summary>创建反射成员信息</summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static IObjectMemberInfo CreateObjectMemberInfo(MemberInfo member)
        {
            return new ReflectMemberInfo(member);
        }

        /// <summary>创建简单成员信息</summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IObjectMemberInfo CreateObjectMemberInfo(String name, Type type, Object value)
        {
            return new SimpleMemberInfo(name, type, value);
        }
        #endregion

        #region 获取成员信息
        /// <summary>
        /// 获取指定对象的成员信息。优先考虑ISerializable接口。
        /// 对于Write，该方法没有任何问题；对于Read，如果是ISerializable接口，并且value是空，则可能无法取得成员信息。
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="isField">是否字段</param>
        /// <param name="isBaseFirst">是否基类成员排在前面</param>
        /// <returns></returns>
        public static IObjectMemberInfo[] GetMembers(Type type, Object value = null, Boolean isField = true, Boolean isBaseFirst = true)
        {
            if (type == null && value != null) type = value.GetType();

            if (typeof(ISerializable).IsAssignableFrom(type))
            {
                // 如果异常，改用后面的方法获取成员信息
                try
                {
                    return GetMembers(value as ISerializable, type);
                }
                catch { }
            }

            return GetMembers(type, isField, isBaseFirst);
        }

        static DictionaryCache<Type, IObjectMemberInfo[]> fieldCache = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static DictionaryCache<Type, IObjectMemberInfo[]> fieldCache2 = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static DictionaryCache<Type, IObjectMemberInfo[]> propertyCache = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static DictionaryCache<Type, IObjectMemberInfo[]> propertyCache2 = new DictionaryCache<Type, IObjectMemberInfo[]>();
        static IObjectMemberInfo[] GetMembers(Type type, Boolean isField, Boolean isBaseFirst)
        {
            var cache = isField ? (isBaseFirst ? fieldCache : fieldCache2) : (isBaseFirst ? propertyCache : propertyCache2);
            return cache.GetItem<Boolean, Boolean>(type, isField, isBaseFirst, (t, isf, isb) => (isf ? FindFields(t, isb) : FindProperties(t, isb)).Select(CreateObjectMemberInfo).ToArray());
        }

        static DictionaryCache<Type, MemberInfo[]> cache1 = new DictionaryCache<Type, MemberInfo[]>();
        /// <summary>取得所有字段</summary>
        /// <param name="type"></param>
        /// <param name="isBaseFirst"></param>
        /// <returns></returns>
        static MemberInfo[] FindFields(Type type, Boolean isBaseFirst)
        {
            if (type == null) return new MemberInfo[0];

            var list = new List<MemberInfo>();

            // GetFields只能取得本类的字段，没办法取得基类的字段
            var fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fis != null && fis.Length > 0)
            {
                foreach (var item in fis)
                {
                    if (!Attribute.IsDefined(item, typeof(NonSerializedAttribute))) list.Add(item);
                }
            }

            // 递归取父级的字段
            if (type.BaseType != null && type.BaseType != typeof(Object))
            {
                var mis = FindFields(type.BaseType, isBaseFirst);
                if (mis != null)
                {
                    if (isBaseFirst)
                    {
                        // 基类的字段排在子类字段前面
                        var list2 = new List<MemberInfo>(mis);
                        if (list.Count > 0) list2.AddRange(list);
                        list = list2;
                    }
                    else
                        list.AddRange(mis);
                }
            }

            return list.ToArray();
        }

        /// <summary>取得所有属性</summary>
        /// <param name="type"></param>
        /// <param name="isBaseFirst"></param>
        /// <returns></returns>
        static MemberInfo[] FindProperties(Type type, Boolean isBaseFirst)
        {
            if (type == null) return new MemberInfo[0];

            var list = new List<MemberInfo>();

            // 只返回本级的属性，递归返回，保证排序
            var pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (pis != null && pis.Length > 0)
            {
                foreach (var item in pis)
                {
                    // 必须可读写
                    if (!item.CanRead || !item.CanWrite) continue;

                    var ps = item.GetIndexParameters();
                    if (ps != null && ps.Length > 0) continue;

                    if (!Attribute.IsDefined(item, typeof(XmlIgnoreAttribute))) list.Add(item);
                }
            }

            // 递归取父级的属性
            if (type.BaseType != null && type.BaseType != typeof(Object))
            {
                var mis = FindProperties(type.BaseType, isBaseFirst);
                if (mis != null)
                {
                    if (isBaseFirst)
                    {
                        // 基类的属性排在子类属性前面
                        var list2 = new List<MemberInfo>(mis);
                        if (list.Count > 0) list2.AddRange(list);
                        list = list2;
                    }
                    else
                        list.AddRange(mis);
                }
            }

            return list.ToArray();
        }

        static Dictionary<Type, IObjectMemberInfo[]> serialCache = new Dictionary<Type, IObjectMemberInfo[]>();

        static IObjectMemberInfo[] GetMembers(ISerializable value, Type type)
        {
            IObjectMemberInfo[] mis = null;
            if (value == null)
            {
                if (serialCache.TryGetValue(type, out mis)) return mis;

                // 尝试创建type的实例
                value = GetDefaultObject(type) as ISerializable;
            }

            var info = new SerializationInfo(type, new FormatterConverter());

            value.GetObjectData(info, DefaultStreamingContext);

            var list = new List<IObjectMemberInfo>();
            foreach (var item in info)
            {
                list.Add(CreateObjectMemberInfo(item.Name, item.ObjectType, item.Value));
            }
            mis = list.ToArray();

            if (!serialCache.ContainsKey(type))
            {
                lock (serialCache)
                {
                    if (!serialCache.ContainsKey(type)) serialCache.Add(type, mis);
                }
            }

            return mis;
        }
        #endregion

        #region 默认对象
        static DictionaryCache<Type, Object> defCache = new DictionaryCache<Type, object>();
        /// <summary>获取某个类型的默认对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Object GetDefaultObject(Type type)
        {
            // 使用FormatterServices.GetSafeUninitializedObject创建对象，该方法创建的对象不执行构造函数
            return defCache.GetItem(type, delegate(Type t)
            {
                if (t == typeof(String)) return null;
                //return FormatterServices.GetSafeUninitializedObject(t);

                // 如果类型没有无参数构造函数，则可能异常
                try
                {
                    return TypeX.CreateInstance(t);
                }
                catch { return FormatterServices.GetSafeUninitializedObject(t); }
            });
        }
        #endregion
    }
}